using Domain.Entities;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Infrastructure.Ocpp
{
    public interface IOcppService
    {
        Task ProcessWebSocketAsync(HttpContext context, string stationId);
        Task SendTriggerMessageAsync(string stationId, string requestedMessage);
        Task SendChangeConfigurationAsync(string stationId, string key, string value);
    }

    public class OcppService : IOcppService
    {
        private readonly IChargingStationService _chargingStationService;
        private readonly ILogger<OcppService> _logger;

        public OcppService(IChargingStationService chargingStationService, ILogger<OcppService> logger)
        {
            _chargingStationService = chargingStationService;
            _logger = logger;
        }

        public async Task ProcessWebSocketAsync(HttpContext context, string stationId)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext { SubProtocol = "ocpp1.6" });
            _logger.LogInformation("WebSocket accepted for stationId: {StationId}", stationId);
            OcppConnectionManager.RegisterStationSocket(stationId, ws);

            var buffer = new byte[8192];
            while (ws.State == WebSocketState.Open)
            {
                var data = new List<byte>();
                WebSocketReceiveResult result;
                do
                {
                    _logger.LogDebug("Awaiting data... Buffer length: {BufferLength}", buffer.Length);
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Station closed connection gracefully.");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }
                    data.AddRange(buffer.Take(result.Count));
                } while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(data.ToArray()).Trim();
                _logger.LogDebug("Complete message received: {Json}", json);
                if (string.IsNullOrEmpty(json)) continue;

                JsonArray arr;
                try
                {
                    arr = JsonNode.Parse(json) as JsonArray ?? new JsonArray();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse OCPP message: {Json}", json);
                    continue;
                }
                if (arr.Count < 2) continue;

                int messageType = arr[0]!.GetValue<int>();
                string uid = stationId;
                if (messageType == 2)
                {
                    var action = arr[2]!.GetValue<string>();
                    var payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                    _logger.LogInformation("[OCPP] Call: {Action} from {StationId}", action, stationId);
                    if (action.Equals("MeterValues", StringComparison.OrdinalIgnoreCase))
                        await HandleMeterValues(ws, uid, payload);
                    else
                        await ProcessCallAsync(ws, uid, action, payload);
                }
                else
                {
                    _logger.LogInformation("[OCPP] Non-call message type {Type} from {StationId}", messageType, stationId);
                }
            }
        }

        public async Task SendChangeConfigurationAsync(string stationId, string key, string value)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No open socket for {StationId}", stationId);
                return;
            }
            var uid = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["key"] = key, ["value"] = value };
            var msg = new JsonArray { 2, uid, "ChangeConfiguration", payload };
            _logger.LogInformation("Sending ChangeConfiguration to {StationId}: {Key}={Value}", stationId, key, value);
            await SendResponse(ws, msg);
        }

        public async Task SendTriggerMessageAsync(string stationId, string requestedMessage)
        {
            // grab the already-open OCPP socket
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null)
            {
                _logger.LogWarning("No WebSocket found for stationId={StationId}", stationId);
                return;
            }

            // build the OCPP CALL message [2,<uniqueId>,"TriggerMessage",{"requestedMessage":…}]
            var uniqueId = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["requestedMessage"] = requestedMessage };
            var message = new JsonArray { 2, uniqueId, "TriggerMessage", payload };
            var json = message.ToJsonString();

            _logger.LogInformation("Sending TriggerMessage to {StationId}: {RequestedMessage}", stationId, requestedMessage);
            await ws.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None
            );
        }


        private async Task ProcessCallAsync(WebSocket ws, string uid, string action, JsonObject payload)
        {
            switch (action)
            {
                case "BootNotification": await HandleBootNotification(ws, uid, payload); break;
                case "Heartbeat": await HandleHeartbeat(ws, uid, payload); break;
                case "StatusNotification": await HandleStatusNotification(ws, uid, payload); break;
                case "Authorize": await HandleAuthorize(ws, uid, payload); break;
                case "StartTransaction": await HandleStartTransaction(ws, uid, payload); break;
                case "StopTransaction": await HandleStopTransaction(ws, uid, payload); break;
                case "FirmwareStatusNotification": await HandleFirmwareStatusNotification(ws, uid, payload); break;
                case "DataTransfer": await HandleDataTransfer(ws, uid, payload); break;
                case "TriggerMessage": await HandleTriggerMessage(ws, uid, payload); break;
                default:
                    var err = new JsonArray { 4, uid, "NotImplemented", $"'{action}' not implemented", new JsonObject() };
                    await SendResponse(ws, err);
                    break;
            }
        }

        private async Task HandleBootNotification(WebSocket ws, string uid, JsonObject payload)
        {
            string vendor = payload["chargePointVendor"]?.GetValue<string>() ?? "Unknown";
            string model = payload["chargePointModel"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("BootNotification from {Vendor}/{Model}", vendor, model);
            var station = await _chargingStationService.GetStationByOcppIdAsync(uid)
                          ?? new ChargingStation { OcppStationId = uid, Model = model };
            station.LastHeartbeat = DateTime.UtcNow;
            station.ChargerStatus = "Booted";
            station.Model = model;
            await _chargingStationService.UpdateStationStatusAsync(station);

            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o"), ["interval"] = 300, ["status"] = "Accepted" };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleHeartbeat(WebSocket ws, string uid, JsonObject payload)
        {
            _logger.LogInformation("Heartbeat from {Station}", uid);
            var station = await _chargingStationService.GetStationByOcppIdAsync(uid);
            if (station != null)
            {
                station.LastHeartbeat = DateTime.UtcNow;
                station.ChargerStatus = "Active";
                await _chargingStationService.UpdateStationStatusAsync(station);
            }
            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleMeterValues(WebSocket ws, string uid, JsonObject payload)
        {
            _logger.LogDebug("MeterValues payload: {Payload}", payload.ToJsonString());
            var meterArray = payload["meterValue"] as JsonArray;
            double? power = null, current = null;
            if (meterArray != null)
            {
                foreach (JsonObject mv in meterArray)
                {
                    var sv = mv["sampledValue"] as JsonArray;
                    if (sv != null)
                        foreach (JsonObject val in sv)
                        {
                            var meas = val["measurand"]?.GetValue<string>();
                            var vstr = val["value"]?.GetValue<string>();
                            if (meas == "Power.Active.Import" && double.TryParse(vstr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) power = p;
                            if (meas == "Current.Import" && double.TryParse(vstr, NumberStyles.Any, CultureInfo.InvariantCulture, out var c)) current = c;
                        }
                }
            }
            _logger.LogInformation("Extracted MeterValues: Power={Power}, Current={Current}", power, current);
            var station = await _chargingStationService.GetStationByOcppIdAsync(uid);
            if (station != null)
            {
                station.MeterLine1Power = power;
                station.MeterLine1Current = current;
                await _chargingStationService.UpdateStationStatusAsync(station);
            }
            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleStatusNotification(WebSocket ws, string uid, JsonObject payload)
        {
            var status = payload["status"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("StatusNotification from {Station}: {Status}", uid, status);
            var station = await _chargingStationService.GetStationByOcppIdAsync(uid);
            if (station != null)
            {
                station.ChargerStatus = status;
                await _chargingStationService.UpdateStationStatusAsync(station);
            }
            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleAuthorize(WebSocket ws, string uid, JsonObject payload)
        {
            var idTag = payload["idTag"]?.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Authorize request for {IdTag} at {Station}", idTag, uid);
            var idTagInfo = new JsonObject { ["status"] = "Accepted" };
            var resp = new JsonObject { ["idTagInfo"] = idTagInfo };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleStartTransaction(WebSocket ws, string uid, JsonObject payload)
        {
            var transId = 1;
            _logger.LogInformation("StartTransaction at {Station}", uid);
            var resp = new JsonObject
            {
                ["transactionId"] = transId,
                ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" }
            };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleStopTransaction(WebSocket ws, string uid, JsonObject payload)
        {
            _logger.LogInformation("StopTransaction at {Station}", uid);
            var resp = new JsonObject { ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" } };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleFirmwareStatusNotification(WebSocket ws, string uid, JsonObject payload)
        {
            var status = payload["status"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("FirmwareStatusNotification for {Station}: {Status}", uid, status);
            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleDataTransfer(WebSocket ws, string uid, JsonObject payload)
        {
            var vendor = payload["vendorId"]?.GetValue<string>();
            var msgId = payload["messageId"]?.GetValue<string>();
            _logger.LogInformation("DataTransfer from {Station}: vendor={Vendor}, messageId={MsgId}", uid, vendor, msgId);
            var resp = new JsonObject { ["status"] = "Accepted" };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task HandleTriggerMessage(WebSocket ws, string uid, JsonObject payload)
        {
            var reqMsg = payload["requestedMessage"]?.GetValue<string>();
            _logger.LogInformation("TriggerMessage request for {Msg} at {Station}", reqMsg, uid);
            var resp = new JsonObject { ["status"] = "Accepted" };
            await SendResponse(ws, new JsonArray { 3, uid, resp });
        }

        private async Task SendResponse(WebSocket ws, JsonArray response)
        {
            var json = response.ToJsonString();
            _logger.LogInformation("Sending response: {Json}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static class OcppConnectionManager
    {
        private static readonly Dictionary<string, WebSocket> _stationSockets = new();
        public static void RegisterStationSocket(string stationId, WebSocket socket) => _stationSockets[stationId] = socket;
        public static WebSocket? GetStationSocket(string stationId) => _stationSockets.TryGetValue(stationId, out var ws) ? ws : null;
    }
}

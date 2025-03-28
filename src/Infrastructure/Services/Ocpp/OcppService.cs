using Domain.Entities;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Ocpp
{
    public interface IOcppService
    {
        Task ProcessWebSocketAsync(HttpContext context, string stationId);
        Task SendTriggerMessageAsync(HttpContext context, string stationId, string requestedMessage);
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

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("New OCPP station connected with stationId from URL: {StationId}", stationId);

            var buffer = new byte[8192];

            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var receivedData = new List<byte>();
                    WebSocketReceiveResult result = null;
                    try
                    {
                        // Accumulate full message from multiple fragments if needed.
                        do
                        {
                            _logger.LogDebug("Awaiting data... Buffer length: {BufferLength}", buffer.Length);
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                            _logger.LogDebug("Received {Count} bytes, MessageType: {MessageType}, EndOfMessage: {EndOfMessage}, State: {State}",
                                result.Count, result.MessageType, result.EndOfMessage, ws.State);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                _logger.LogInformation("Station closed connection gracefully.");
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                                return;
                            }
                            receivedData.AddRange(buffer.Take(result.Count));
                        } while (!result.EndOfMessage);
                    }
                    catch (OperationCanceledException oce)
                    {
                        _logger.LogWarning(oce, "Operation canceled during WebSocket receive. Aborted.");
                        break;
                    }
                    catch (WebSocketException wse)
                    {
                        _logger.LogWarning(wse, "WebSocket exception encountered. WebSocket State: {State}. Remote party may have closed the connection abruptly.", ws.State);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error during WebSocket receive.");
                        break;
                    }

                    string jsonText = Encoding.UTF8.GetString(receivedData.ToArray()).Trim();
                    _logger.LogDebug("Complete message received: {JsonText}", jsonText);
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        _logger.LogWarning("Received an empty message after trimming.");
                        continue;
                    }

                    JsonNode? root;
                    try
                    {
                        root = JsonNode.Parse(jsonText);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse station message. Received text: {JsonText}", jsonText);
                        continue;
                    }

                    if (root is not JsonArray arr || arr.Count < 2)
                    {
                        _logger.LogWarning("Malformed OCPP message (not a valid array). Data: {JsonText}", jsonText);
                        continue;
                    }

                    int messageTypeId = arr[0]?.GetValue<int>() ?? -1;
                    _logger.LogDebug("Parsed messageTypeId: {MessageTypeId}", messageTypeId);

                    // Use the stationId from the URL as the unique identifier.
                    string uniqueId = stationId;

                    if (messageTypeId == 2)
                    {
                        string action = arr.Count > 2 ? arr[2]?.GetValue<string>() ?? "" : "";
                        JsonObject payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                        _logger.LogInformation("[OCPP] Call received: Action={Action}, StationId={StationId}", action, uniqueId);
                        await ProcessCallAsync(ws, uniqueId, action, payload);
                    }
                    else
                    {
                        _logger.LogInformation("[OCPP] Received message type {MessageTypeId} for StationId={StationId}", messageTypeId, uniqueId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred in ProcessWebSocketAsync.");
            }
        }

        private async Task ProcessCallAsync(WebSocket ws, string uniqueId, string action, JsonObject payload)
        {
            switch (action)
            {
                case "BootNotification":
                    await HandleBootNotification(ws, uniqueId, payload);
                    break;
                case "Heartbeat":
                    await HandleHeartbeat(ws, uniqueId, payload);
                    break;
                case "TriggerMessage":
                    await HandleTriggerMessage(ws, uniqueId, payload);
                    break;
                default:
                    var callError = new JsonArray
                    {
                        4,
                        uniqueId,
                        "NotImplemented",
                        $"Action '{action}' is not implemented.",
                        new JsonObject()
                    };
                    await SendResponse(ws, callError);
                    break;
            }
        }

        private async Task HandleBootNotification(WebSocket ws, string uniqueId, JsonObject payload)
        {
            string vendor = payload["chargePointVendor"]?.GetValue<string>() ?? "UnknownVendor";
            string model = payload["chargePointModel"]?.GetValue<string>() ?? "UnknownModel";
            // NEW custom fields
            string? vehicle = payload["vehicle"]?.GetValue<string>();
            string? access = payload["access"]?.GetValue<string>();
            string? selfConsumption = payload["selfConsumption"]?.GetValue<string>();
            string? internet = payload["internet"]?.GetValue<string>();
            string? scheduling = payload["scheduling"]?.GetValue<string>();
            string? meterNominalPower = payload["meterNominalPower"]?.GetValue<string>();

            _logger.LogInformation("BootNotification from {Vendor}/{Model},{vehicle},{access},{selfConsumption},{internet},{scheduling},{meterNominalPower}", vendor, model,vehicle,access,selfConsumption,internet,scheduling,meterNominalPower);

            // Retrieve existing station by its OCPP identifier.
            var station = await _chargingStationService.GetStationByOcppIdAsync(uniqueId);
            if (station == null)
            {
                station = new ChargingStation
                {
                    OcppStationId = uniqueId,
                    ChargerName = "Charger-" + uniqueId,
                    Model = model,
                    BootTime = DateTime.UtcNow,
                    LastHeartbeat = DateTime.UtcNow,
                    ChargerStatus = "Booted",
                    ConnectionStatus = "Disconnected",
                    SerialNumber = "N/A",
                    Puk = "N/A",
                    PowerValue = 0,
                    NetworkId = 1,  // default network, if needed

                    // Assign custom fields
                    Vehicle = vehicle,
                    Access = access,
                    SelfConsumption = selfConsumption,
                    Internet = internet,
                    Scheduling = scheduling,
                    MeterNominalPower = meterNominalPower
                };
            }
            else
            {
                station.Model = model;
                station.BootTime = DateTime.UtcNow;
                station.LastHeartbeat = DateTime.UtcNow;
                station.ChargerStatus = "Booted";
                if (string.IsNullOrEmpty(station.ConnectionStatus))
                    station.ConnectionStatus = "Disconnected";
                if (string.IsNullOrEmpty(station.ChargerName))
                    station.ChargerName = "Charger-" + uniqueId;
                if (string.IsNullOrEmpty(station.SerialNumber))
                    station.SerialNumber = "N/A";
                if (string.IsNullOrEmpty(station.Puk))
                    station.Puk = "N/A";
                if (station.PowerValue == 0)
                    station.PowerValue = 0;
                if (string.IsNullOrEmpty(station.PhotoUrl))
                    station.PhotoUrl = "https://example.com/default-charger.png";
                if (station.NetworkId == 0)
                    station.NetworkId = 1;
                // Update custom fields if not null
                if (!string.IsNullOrEmpty(vehicle)) station.Vehicle = vehicle;
                if (!string.IsNullOrEmpty(access)) station.Access = access;
                if (!string.IsNullOrEmpty(selfConsumption)) station.SelfConsumption = selfConsumption;
                if (!string.IsNullOrEmpty(internet)) station.Internet = internet;
                if (!string.IsNullOrEmpty(scheduling)) station.Scheduling = scheduling;
                if (!string.IsNullOrEmpty(meterNominalPower)) station.MeterNominalPower = meterNominalPower;
            }

            await _chargingStationService.UpdateStationStatusAsync(station);

            var responsePayload = new JsonObject
            {
                ["currentTime"] = DateTime.UtcNow.ToString("o"),
                ["interval"] = 300,
                ["status"] = "Accepted"
            };

            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }






        private async Task HandleHeartbeat(WebSocket ws, string uniqueId, JsonObject payload)
        {
            _logger.LogInformation("Heartbeat received for StationId={StationId}", uniqueId);
            var station = await _chargingStationService.GetStationByOcppIdAsync(uniqueId);
            if (station != null)
            {
                station.LastHeartbeat = DateTime.UtcNow;
                station.ChargerStatus = "Active";
                await _chargingStationService.UpdateStationStatusAsync(station);
            }

            var responsePayload = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        // New method to handle TriggerMessage requests.
        private async Task HandleTriggerMessage(WebSocket ws, string uniqueId, JsonObject payload)
        {
            string requestedMessage = payload["requestedMessage"]?.GetValue<string>() ?? "";
            _logger.LogInformation("TriggerMessage received for StationId={StationId}. Requested message: {RequestedMessage}", uniqueId, requestedMessage);

            // Here, we respond with a simple TriggerMessageResponse.
            // You can expand this logic to actually trigger sending a message to the charging station if needed.
            var responsePayload = new JsonObject
            {
                ["status"] = "Accepted"
            };

            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        public async Task SendTriggerMessageAsync(HttpContext context, string stationId, string requestedMessage)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                _logger.LogWarning("No WebSocket request found when attempting to send TriggerMessage.");
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation("Sending TriggerMessage request for StationId: {StationId}", stationId);

            var triggerPayload = new JsonObject
            {
                ["requestedMessage"] = requestedMessage
            };
            var triggerMessage = new JsonArray { 2, stationId, "TriggerMessage", triggerPayload };
            await SendResponse(ws, triggerMessage);
        }

        private async Task SendResponse(WebSocket ws, JsonArray response)
        {
            string json = response.ToJsonString();
            _logger.LogInformation("Sending OCPP response: {ResponseJson}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

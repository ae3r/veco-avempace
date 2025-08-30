using Domain.Entities;
using System;
using System.Linq;                           // FirstOrDefault, Take
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Generic;            // List<T>
using System.Threading;                      // Interlocked

namespace Infrastructure.Ocpp
{
    public interface IOcppService
    {
        Task ProcessWebSocketAsync(HttpContext context, string stationId);
        Task SendTriggerMessageAsync(string stationId, string requestedMessage);
        Task SendChangeConfigurationAsync(string stationId, string key, string value);

        // NEW: remote start/stop
        Task SendRemoteStartTransactionAsync(string stationId, int connectorId, string idTag);
        Task SendRemoteStopTransactionAsync(string stationId, int transactionId);
    }

    public class OcppService : IOcppService
    {
        private readonly IChargingStationService _chargingStationService;
        private readonly ILogger<OcppService> _logger;

        // NEW: track active transactions per station
        private static readonly ConcurrentDictionary<string, int> _activeTransactions = new();
        private static int _txCounter = 1000;

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

            // Negotiate OCPP subprotocol with the charger
            var offered = context.Request.Headers["Sec-WebSocket-Protocol"].ToString();
            string? subprotocol = offered
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(p =>
                    p.Equals("ocpp1.6", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("ocpp1.6J", StringComparison.OrdinalIgnoreCase) ||
                    p.Equals("ocpp1.6-json", StringComparison.OrdinalIgnoreCase));

            _logger.LogInformation("OCPP subprotocol offered: {Offered}; chosen: {Chosen}", offered, subprotocol ?? "(none)");

            using var ws = await context.WebSockets.AcceptWebSocketAsync(
                new WebSocketAcceptContext { SubProtocol = subprotocol });

            _logger.LogInformation("WebSocket accepted for stationId: {StationId}", stationId);
            OcppConnectionManager.RegisterStationSocket(stationId, ws);

            // Mark connected
            var connected = await _chargingStationService.GetStationByOcppIdAsync(stationId)
                           ?? new ChargingStation { OcppStationId = stationId };
            connected.ConnectionStatus = "Connected";
            connected.LastHeartbeat = DateTime.UtcNow;
            await _chargingStationService.UpdateStationStatusAsync(connected);

            // Warm-up the link so proxies don't idle-close and the CP starts sending soon
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    // Ask for frequent heartbeats
                    await SendChangeConfigurationAsync(stationId, "HeartbeatInterval", "30");
                    // Nudge the charger to talk
                    await SendTriggerMessageAsync(stationId, "Heartbeat");
                    await SendTriggerMessageAsync(stationId, "BootNotification"); // optional; many CPs support it
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Warm-up heartbeat/boot trigger failed for {StationId}", stationId);
                }
            });

            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var data = new List<byte>();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogInformation("Station {StationId} closed connection.", stationId);
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

                            var st = await _chargingStationService.GetStationByOcppIdAsync(stationId);
                            if (st != null)
                            {
                                st.ConnectionStatus = "Disconnected";
                                await _chargingStationService.UpdateStationStatusAsync(st);
                            }

                            OcppConnectionManager.UnregisterStationSocket(stationId);
                            return;
                        }
                        data.AddRange(buffer.Take(result.Count));
                    } while (!result.EndOfMessage);

                    var json = Encoding.UTF8.GetString(data.ToArray()).Trim();
                    if (string.IsNullOrEmpty(json)) continue;

                    JsonArray? arr;
                    try
                    {
                        arr = JsonNode.Parse(json) as JsonArray;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse OCPP message: {Json}", json);
                        continue;
                    }
                    if (arr == null || arr.Count < 2) continue;

                    int messageType = arr[0]!.GetValue<int>();
                    string messageId = arr[1]!.GetValue<string>();

                    if (messageType == 2)
                    {
                        // CALL: [2, "<MessageId>", "Action", {payload}]
                        var action = arr[2]!.GetValue<string>();
                        var payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                        _logger.LogInformation("[OCPP] Call: {Action} from {StationId}", action, stationId);

                        if (action.Equals("MeterValues", StringComparison.OrdinalIgnoreCase))
                            await HandleMeterValues(ws, stationId, messageId, payload);
                        else
                            await ProcessCallAsync(ws, stationId, messageId, action, payload);
                    }
                    else if (messageType == 3)
                    {
                        // CALLRESULT: [3, "<MessageId>", {payload}]
                        _logger.LogInformation("[OCPP] CallResult {MessageId} from {StationId}: {Json}", messageId, stationId, json);
                    }
                    else if (messageType == 4)
                    {
                        // CALLERROR: [4, "<MessageId>", "errorCode", "errorDesc", {details}]
                        _logger.LogWarning("[OCPP] CallError {MessageId} from {StationId}: {Json}", messageId, stationId, json);
                    }
                }
            }
            catch (OperationCanceledException) { /* app shutting down or request aborted */ }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OCPP loop for {StationId}", stationId);
            }
            finally
            {
                // ensure disconnected state is persisted
                var st = await _chargingStationService.GetStationByOcppIdAsync(stationId);
                if (st != null)
                {
                    st.ConnectionStatus = "Disconnected";
                    await _chargingStationService.UpdateStationStatusAsync(st);
                }

                OcppConnectionManager.UnregisterStationSocket(stationId);
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
            await SendArray(ws, msg);
        }

        public async Task SendTriggerMessageAsync(string stationId, string requestedMessage)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No WebSocket found or not open for stationId={StationId}", stationId);
                return;
            }
            var uniqueId = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["requestedMessage"] = requestedMessage };
            var message = new JsonArray { 2, uniqueId, "TriggerMessage", payload };

            _logger.LogInformation("Sending TriggerMessage to {StationId}: {RequestedMessage}", stationId, requestedMessage);
            await SendArray(ws, message);
        }

        // ========= NEW: remote start/stop =========

        public async Task SendRemoteStartTransactionAsync(string stationId, int connectorId, string idTag)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No WebSocket found or not open for stationId={StationId}", stationId);
                return;
            }
            var uid = Guid.NewGuid().ToString("N");
            var payload = new JsonObject
            {
                ["connectorId"] = connectorId,
                ["idTag"] = idTag
            };
            var message = new JsonArray { 2, uid, "RemoteStartTransaction", payload };
            _logger.LogInformation("Sending RemoteStartTransaction to {StationId}: connectorId={ConnectorId}, idTag={IdTag}", stationId, connectorId, idTag);
            await SendArray(ws, message);
        }

        public async Task SendRemoteStopTransactionAsync(string stationId, int transactionId)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No WebSocket found or not open for stationId={StationId}", stationId);
                return;
            }
            var uid = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["transactionId"] = transactionId };
            var message = new JsonArray { 2, uid, "RemoteStopTransaction", payload };
            _logger.LogInformation("Sending RemoteStopTransaction to {StationId}: transactionId={TransactionId}", stationId, transactionId);
            await SendArray(ws, message);
        }

        // ==========================================

        private async Task ProcessCallAsync(WebSocket ws, string stationId, string messageId, string action, JsonObject payload)
        {
            switch (action)
            {
                case "BootNotification": await HandleBootNotification(ws, stationId, messageId, payload); break;
                case "Heartbeat": await HandleHeartbeat(ws, stationId, messageId, payload); break;
                case "StatusNotification": await HandleStatusNotification(ws, stationId, messageId, payload); break;
                case "Authorize": await HandleAuthorize(ws, stationId, messageId, payload); break;
                case "StartTransaction": await HandleStartTransaction(ws, stationId, messageId, payload); break;
                case "StopTransaction": await HandleStopTransaction(ws, stationId, messageId, payload); break;
                case "FirmwareStatusNotification": await HandleFirmwareStatusNotification(ws, stationId, messageId, payload); break;
                case "DataTransfer": await HandleDataTransfer(ws, stationId, messageId, payload); break;
                case "TriggerMessage": await HandleTriggerMessage(ws, stationId, messageId, payload); break;
                default:
                    var err = new JsonArray { 4, messageId, "NotImplemented", $"'{action}' not implemented", new JsonObject() };
                    await SendArray(ws, err);
                    break;
            }
        }

        private async Task HandleBootNotification(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            string vendor = payload["chargePointVendor"]?.GetValue<string>() ?? "Unknown";
            string model = payload["chargePointModel"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("BootNotification from {Vendor}/{Model} @ {StationId}", vendor, model, stationId);

            var station = await _chargingStationService.GetStationByOcppIdAsync(stationId)
                          ?? new ChargingStation { OcppStationId = stationId, Model = model };

            station.BootTime = DateTime.UtcNow;
            station.LastHeartbeat = DateTime.UtcNow;
            station.ChargerStatus = "Booted";
            station.Model = model;
            await _chargingStationService.UpdateStationStatusAsync(station);

            // IMPORTANT: send desired heartbeat interval here so CP schedules heartbeats accordingly
            var resp = new JsonObject
            {
                ["currentTime"] = DateTime.UtcNow.ToString("o"),
                ["interval"] = 30, // was 300; set to 30 so heartbeats become ~30s
                ["status"] = "Accepted"
            };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleHeartbeat(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            _logger.LogInformation("Heartbeat from {Station}", stationId);
            var station = await _chargingStationService.GetStationByOcppIdAsync(stationId);
            if (station != null)
            {
                station.LastHeartbeat = DateTime.UtcNow;
                station.ChargerStatus = "Active";
                await _chargingStationService.UpdateStationStatusAsync(station);
            }
            var resp = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleMeterValues(WebSocket ws, string stationId, string messageId, JsonObject payload)
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
                    {
                        foreach (JsonObject val in sv)
                        {
                            var meas = val["measurand"]?.GetValue<string>();
                            var vstr = val["value"]?.GetValue<string>();
                            if (meas == "Power.Active.Import" &&
                                double.TryParse(vstr, NumberStyles.Any, CultureInfo.InvariantCulture, out var p)) power = p;

                            if (meas == "Current.Import" &&
                                double.TryParse(vstr, NumberStyles.Any, CultureInfo.InvariantCulture, out var c)) current = c;
                        }
                    }
                }
            }

            _logger.LogInformation("Extracted MeterValues for {Station}: Power={Power}, Current={Current}", stationId, power, current);

            var station = await _chargingStationService.GetStationByOcppIdAsync(stationId);
            if (station != null)
            {
                station.MeterLine1Power = power;
                station.MeterLine1Current = current;
                await _chargingStationService.UpdateStationStatusAsync(station);
            }

            // OCPP 1.6 expects an empty payload object for MeterValues CallResult
            await SendArray(ws, new JsonArray { 3, messageId, new JsonObject() });
        }

        private async Task HandleStatusNotification(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var status = payload["status"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("StatusNotification from {Station}: {Status}", stationId, status);

            var station = await _chargingStationService.GetStationByOcppIdAsync(stationId);
            if (station != null)
            {
                station.ChargerStatus = status;
                await _chargingStationService.UpdateStationStatusAsync(station);
            }

            await SendArray(ws, new JsonArray { 3, messageId, new JsonObject() });
        }

        private async Task HandleAuthorize(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var idTag = payload["idTag"]?.GetValue<string>() ?? string.Empty;
            _logger.LogInformation("Authorize request for {IdTag} at {Station}", idTag, stationId);

            var idTagInfo = new JsonObject { ["status"] = "Accepted" };
            var resp = new JsonObject { ["idTagInfo"] = idTagInfo };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleStartTransaction(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var idTag = payload["idTag"]?.GetValue<string>() ?? "";
            var connectorId = payload["connectorId"]?.GetValue<int>() ?? 0;
            var meterStart = payload["meterStart"]?.GetValue<int?>();

            // Assign a unique transaction id per StartTransaction
            var newTxId = Interlocked.Increment(ref _txCounter);
            _activeTransactions[stationId] = newTxId;

            _logger.LogInformation("StartTransaction @ {Station}: txId={TxId}, connector={Connector}, idTag={IdTag}, meterStart={Meter}",
                stationId, newTxId, connectorId, idTag, meterStart);

            var resp = new JsonObject
            {
                ["transactionId"] = newTxId,
                ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" }
            };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleStopTransaction(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var meterStop = payload["meterStop"]?.GetValue<int?>();

            _activeTransactions.TryRemove(stationId, out _);

            _logger.LogInformation("StopTransaction @ {Station}, meterStop={MeterStop}", stationId, meterStop);
            var resp = new JsonObject { ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" } };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleFirmwareStatusNotification(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var status = payload["status"]?.GetValue<string>() ?? "Unknown";
            _logger.LogInformation("FirmwareStatusNotification for {Station}: {Status}", stationId, status);
            await SendArray(ws, new JsonArray { 3, messageId, new JsonObject() });
        }

        private async Task HandleDataTransfer(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var vendor = payload["vendorId"]?.GetValue<string>();
            var msgId = payload["messageId"]?.GetValue<string>();
            _logger.LogInformation("DataTransfer from {Station}: vendor={Vendor}, messageId={MsgId}", stationId, vendor, msgId);
            var resp = new JsonObject { ["status"] = "Accepted" };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private async Task HandleTriggerMessage(WebSocket ws, string stationId, string messageId, JsonObject payload)
        {
            var reqMsg = payload["requestedMessage"]?.GetValue<string>();
            _logger.LogInformation("TriggerMessage request for {Msg} at {Station}", reqMsg, stationId);
            var resp = new JsonObject { ["status"] = "Accepted" };
            await SendArray(ws, new JsonArray { 3, messageId, resp });
        }

        private static async Task SendArray(WebSocket ws, JsonArray response)
        {
            var json = response.ToJsonString();
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static class OcppConnectionManager
    {
        private static readonly ConcurrentDictionary<string, WebSocket> _stationSockets = new();

        public static void RegisterStationSocket(string stationId, WebSocket socket)
            => _stationSockets[stationId] = socket;

        public static WebSocket? GetStationSocket(string stationId)
            => _stationSockets.TryGetValue(stationId, out var ws) ? ws : null;

        public static void UnregisterStationSocket(string stationId)
            => _stationSockets.TryRemove(stationId, out _);
    }
}

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
                var received = new List<byte>();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Station closed connection.");
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }
                    received.AddRange(buffer.Take(result.Count));
                } while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(received.ToArray()).Trim();
                if (string.IsNullOrEmpty(json)) continue;

                JsonArray? arr;
                try { arr = JsonNode.Parse(json) as JsonArray; }
                catch { _logger.LogWarning("Invalid JSON: {Json}", json); continue; }
                if (arr == null || arr.Count < 2) continue;

                var messageTypeId = arr[0]!.GetValue<int>();
                var uniqueId = stationId;

                if (messageTypeId == 2)
                {
                    var action = arr[2]!.GetValue<string>();
                    var payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                    _logger.LogInformation("[OCPP] Call received: Action={Action}, StationId={StationId}", action, uniqueId);

                    if (action.Equals("MeterValues", StringComparison.OrdinalIgnoreCase))
                        await HandleMeterValues(ws, uniqueId, payload);
                    else
                        await ProcessCallAsync(ws, uniqueId, action, payload);
                }
                else
                {
                    _logger.LogInformation("[OCPP] Received non-call messageType {Type} for {StationId}", messageTypeId, stationId);
                }
            }
        }

        public async Task SendChangeConfigurationAsync(string stationId, string key, string value)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No socket for {StationId}", stationId);
                return;
            }

            var uid = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["key"] = key, ["value"] = value };
            var msg = new JsonArray { 2, uid, "ChangeConfiguration", payload };

            _logger.LogInformation("Sending ChangeConfiguration to {StationId}", stationId);
            await SendResponse(ws, msg);
        }

        public async Task SendTriggerMessageAsync(string stationId, string requestedMessage)
        {
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null || ws.State != WebSocketState.Open)
            {
                _logger.LogWarning("No open WebSocket for station {StationId}", stationId);
                return;
            }

            var uid = Guid.NewGuid().ToString("N");
            var payload = new JsonObject { ["requestedMessage"] = requestedMessage };
            var msg = new JsonArray { 2, uid, "TriggerMessage", payload };

            _logger.LogInformation("Triggering {RequestedMessage} on {StationId}", requestedMessage, stationId);
            await SendResponse(ws, msg);
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
                    var err = new JsonArray { 4, uid, "NotImplemented", $"Action '{action}' not implemented.", new JsonObject() };
                    await SendResponse(ws, err);
                    break;
            }
        }

        // Handlers for each OCPP action:
        private async Task HandleBootNotification(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleHeartbeat(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleMeterValues(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleStatusNotification(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleAuthorize(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleStartTransaction(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleStopTransaction(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleFirmwareStatusNotification(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleDataTransfer(WebSocket ws, string uid, JsonObject p) { /* ... */ }
        private async Task HandleTriggerMessage(WebSocket ws, string uid, JsonObject p) { /* ... */ }

        private async Task SendResponse(WebSocket ws, JsonArray response)
        {
            var json = response.ToJsonString();
            _logger.LogInformation("Sending OCPP response: {Json}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static class OcppConnectionManager
    {
        private static readonly Dictionary<string, WebSocket> _stationSockets = new();
        public static void RegisterStationSocket(string stationId, WebSocket socket) => _stationSockets[stationId] = socket;
        public static WebSocket? GetStationSocket(string stationId) => _stationSockets.TryGetValue(stationId, out var s) ? s : null;
    }
}
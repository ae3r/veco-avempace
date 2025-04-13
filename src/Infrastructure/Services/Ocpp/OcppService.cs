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
        Task SendTriggerMessageAsync(HttpContext context, string stationId, string requestedMessage);
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

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            try
            {
                _logger.LogInformation("WebSocket accepted for stationId: {StationId}", stationId);
            }
            catch (Exception ex)
            {
                // Log if logger itself is failing
                Console.WriteLine("Logger error before registration: " + ex);
            }

            // Register the station’s WebSocket
            OcppConnectionManager.RegisterStationSocket(stationId, ws);

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
                            try
                            {
                                _logger.LogDebug("Awaiting data... Buffer length: {BufferLength}", buffer.Length);
                            }
                            catch (Exception logEx)
                            {
                                Console.WriteLine("Logging error before receive: " + logEx);
                            }

                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);

                            try
                            {
                                _logger.LogDebug("Received {Count} bytes, MessageType: {MessageType}, EndOfMessage: {EndOfMessage}, State: {State}",
                                    result.Count, result.MessageType, result.EndOfMessage, ws.State);
                            }
                            catch (Exception logEx)
                            {
                                Console.WriteLine("Logging error after receive: " + logEx);
                            }

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                try
                                {
                                    _logger.LogInformation("Station closed connection gracefully.");
                                }
                                catch (Exception logEx)
                                {
                                    Console.WriteLine("Logging error on close: " + logEx);
                                }
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
                        _logger.LogWarning(wse, "WebSocket exception encountered. State: {State}. Remote party may have closed the connection abruptly.", ws.State);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error during WebSocket receive.");
                        break;
                    }

                    string jsonText = Encoding.UTF8.GetString(receivedData.ToArray()).Trim();
                    try
                    {
                        _logger.LogDebug("Complete message received: {JsonText}", jsonText);
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine("Logging error after complete message: " + logEx);
                    }
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
                    try
                    {
                        _logger.LogDebug("Parsed messageTypeId: {MessageTypeId}", messageTypeId);
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine("Logging error after parsing messageTypeId: " + logEx);
                    }

                    // Use the stationId from the URL as the unique identifier.
                    string uniqueId = stationId;

                    if (messageTypeId == 2)
                    {
                        string action = arr.Count > 2 ? arr[2]?.GetValue<string>() ?? "" : "";
                        JsonObject payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                        try
                        {
                            _logger.LogInformation("[OCPP] Call received: Action={Action}, StationId={StationId}", action, uniqueId);
                        }
                        catch (Exception logEx)
                        {
                            Console.WriteLine("Logging error before processing call: " + logEx);
                        }

                        if (action.Equals("MeterValues", StringComparison.OrdinalIgnoreCase))
                        {
                            await HandleMeterValues(ws, uniqueId, payload);
                        }
                        else
                        {
                            await HandleMeterValues(ws, uniqueId, payload);
                            await ProcessCallAsync(ws, uniqueId, action, payload);
                        }
                    }
                    else
                    {
                        try
                        {
                            _logger.LogInformation("[OCPP] Received message type {MessageTypeId} for StationId={StationId}", messageTypeId, uniqueId);
                        }
                        catch (Exception logEx)
                        {
                            Console.WriteLine("Logging error for non-call message: " + logEx);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.LogError(ex, "An unexpected error occurred in ProcessWebSocketAsync.");
                }
                catch (Exception logEx)
                {
                    Console.WriteLine("Logging error in outer catch: " + logEx);
                }
            }
        }


        public async Task SendChangeConfigurationAsync(string stationId, string key, string value)
        {
            // Retrieve the stored WebSocket
            var ws = OcppConnectionManager.GetStationSocket(stationId);
            if (ws == null)
            {
                _logger.LogWarning("No WebSocket found for stationId={StationId}", stationId);
                return;
            }

            // Build the OCPP "Call" message
            // Format: [2, "uniqueId", "ChangeConfiguration", { "key": "...", "value": "..." }]
            var uniqueId = Guid.NewGuid().ToString("N"); // generate a unique ID
            var payload = new JsonObject
            {
                ["key"] = key,
                ["value"] = value
            };
            var messageArray = new JsonArray
            {
                2,
                uniqueId,
                "ChangeConfiguration",
                payload
            };

            string messageJson = messageArray.ToJsonString();
            _logger.LogInformation("Sending ChangeConfiguration to {StationId} for key={Key} value={Value}", stationId, key, value);

            var bytes = Encoding.UTF8.GetBytes(messageJson);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
            // Custom fields
            string? vehicle = payload["vehicle"]?.GetValue<string>();
            string? access = payload["access"]?.GetValue<string>();
            string? selfConsumption = payload["selfConsumption"]?.GetValue<string>();
            string? internet = payload["internet"]?.GetValue<string>();
            string? scheduling = payload["scheduling"]?.GetValue<string>();
            string? meterNominalPower = payload["meterNominalPower"]?.GetValue<string>();

            // Meter values if provided (if the payload sends them here, though typically MeterValues is separate)
            double? line1Power = payload["meterLine1Power"]?.GetValue<double?>();
            double? line1Current = payload["meterLine1Current"]?.GetValue<double?>();
            double? line2Power = payload["meterLine2Power"]?.GetValue<double?>();
            double? line2Current = payload["meterLine2Current"]?.GetValue<double?>();

            _logger.LogInformation("BootNotification from {Vendor}/{Model} Vehicle={Vehicle}, Access={Access}, SelfConsumption={SelfConsumption}, Internet={Internet}, Scheduling={Scheduling}, MeterNominalPower={MeterNominalPower}",
                vendor, model, vehicle, access, selfConsumption, internet, scheduling, meterNominalPower);

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
                    NetworkId = 1,
                    Vehicle = vehicle,
                    Access = access,
                    SelfConsumption = selfConsumption,
                    Internet = internet,
                    Scheduling = scheduling,
                    MeterNominalPower = meterNominalPower,
                    MeterLine1Power = line1Power,
                    MeterLine1Current = line1Current,
                    MeterLine2Power = line2Power,
                    MeterLine2Current = line2Current,
                    PhotoUrl = "https://yourdomain.com/img/autel.jpg"
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
                    station.PhotoUrl = "https://yourdomain.com/img/autel.jpg";
                if (station.NetworkId == 0)
                    station.NetworkId = 1;
                if (!string.IsNullOrEmpty(vehicle)) station.Vehicle = vehicle;
                if (!string.IsNullOrEmpty(access)) station.Access = access;
                if (!string.IsNullOrEmpty(selfConsumption)) station.SelfConsumption = selfConsumption;
                if (!string.IsNullOrEmpty(internet)) station.Internet = internet;
                if (!string.IsNullOrEmpty(scheduling)) station.Scheduling = scheduling;
                if (!string.IsNullOrEmpty(meterNominalPower)) station.MeterNominalPower = meterNominalPower;
                station.MeterLine1Power = line1Power;
                station.MeterLine1Current = line1Current;
                station.MeterLine2Power = line2Power;
                station.MeterLine2Current = line2Current;
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

        private async Task HandleMeterValues(WebSocket ws, string uniqueId, JsonObject payload)
        {
            // Log the entire payload for debugging.
            _logger.LogDebug("Received MeterValues payload: {Payload}", payload.ToJsonString());

            // Retrieve the meter values array from the payload (using "meterValue").
            JsonArray? meterValueArray = payload["meterValue"] as JsonArray;

            double? powerActiveImport = null;
            double? currentImport = null;

            if (meterValueArray != null && meterValueArray.Count > 0)
            {
                foreach (var meterValueNode in meterValueArray)
                {
                    if (meterValueNode is JsonObject meterValueObj)
                    {
                        // Log the timestamp if present.
                        if (meterValueObj["timestamp"] is JsonValue tsValue)
                        {
                            string timestamp = tsValue.GetValue<string>() ?? "";
                            _logger.LogDebug("Meter sample timestamp: {Timestamp}", timestamp);
                        }

                        // Retrieve the sampledValue array.
                        if (meterValueObj["sampledValue"] is JsonArray sampledValues)
                        {
                            _logger.LogDebug("Found {Count} sampled values.", sampledValues.Count);
                            foreach (var sampledValueNode in sampledValues)
                            {
                                if (sampledValueNode is JsonObject sampledValueObj)
                                {
                                    string measurand = "";
                                    string valueStr = "";

                                    if (sampledValueObj["measurand"] is JsonValue measVal)
                                    {
                                        measurand = measVal.GetValue<string>() ?? "";
                                    }
                                    if (sampledValueObj["value"] is JsonValue valueVal)
                                    {
                                        valueStr = valueVal.GetValue<string>() ?? "";
                                    }

                                    _logger.LogDebug("Parsed sampled value: measurand='{Measurand}', value='{Value}'", measurand, valueStr);

                                    if (!string.IsNullOrEmpty(measurand) && !string.IsNullOrEmpty(valueStr))
                                    {
                                        if (measurand.Equals("Power.Active.Import", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                                            {
                                                powerActiveImport = parsedValue;
                                                _logger.LogDebug("Power.Active.Import parsed as: {ParsedValue}", parsedValue);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Failed to parse Power.Active.Import value: {ValueStr}", valueStr);
                                            }
                                        }
                                        else if (measurand.Equals("Current.Import", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
                                            {
                                                currentImport = parsedValue;
                                                _logger.LogDebug("Current.Import parsed as: {ParsedValue}", parsedValue);
                                            }
                                            else
                                            {
                                                _logger.LogWarning("Failed to parse Current.Import value: {ValueStr}", valueStr);
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("Sampled value node is not a JsonObject.");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No 'sampledValue' array found in meterValue sample.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("MeterValue node is not a JsonObject.");
                    }
                }
            }
            else
            {
                _logger.LogWarning("MeterValues payload does not contain a valid 'meterValue' array.");
            }

            _logger.LogInformation("Extracted MeterValues: Power.Active.Import={Power}, Current.Import={Current}",
                                     powerActiveImport, currentImport);

            // Retrieve the charging station record by its OCPP identifier.
            var station = await _chargingStationService.GetStationByOcppIdAsync(uniqueId);
            if (station != null)
            {
                station.MeterLine1Power = powerActiveImport;
                station.MeterLine1Current = currentImport;
                await _chargingStationService.UpdateStationStatusAsync(station);
            }
            else
            {
                _logger.LogWarning("No charging station found with OCPP identifier: {UniqueId}", uniqueId);
            }

            // Prepare and send a response payload.
            var responsePayload = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
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

        private async Task HandleTriggerMessage(WebSocket ws, string uniqueId, JsonObject payload)
        {
            string requestedMessage = payload["requestedMessage"]?.GetValue<string>() ?? "";
            _logger.LogInformation("TriggerMessage received for StationId={StationId}. Requested message: {RequestedMessage}", uniqueId, requestedMessage);

            var responsePayload = new JsonObject
            {
                ["status"] = "Accepted"
            };

            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task SendResponse(WebSocket ws, JsonArray response)
        {
            string json = response.ToJsonString();
            _logger.LogInformation("Sending OCPP response: {ResponseJson}", json);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public static class OcppConnectionManager
    {
        private static readonly Dictionary<string, WebSocket> _stationSockets = new();

        public static void RegisterStationSocket(string stationId, WebSocket socket)
        {
            _stationSockets[stationId] = socket;
        }

        public static WebSocket? GetStationSocket(string stationId)
        {
            _stationSockets.TryGetValue(stationId, out var ws);
            return ws;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Infrastructure.Services.Ocpp
{
    public interface IOcppService
    {
        Task ProcessWebSocketAsync(HttpContext context);
    }

    public class OcppService : IOcppService
    {
        public async Task ProcessWebSocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("New OCPP station connected.");

            var buffer = new byte[8192];

            while (ws.State == WebSocketState.Open)
            {
                var receivedData = new List<byte>();
                WebSocketReceiveResult result;
                try
                {
                    // Accumulate the full message
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("Station closed connection.");
                            return;
                        }
                        receivedData.AddRange(buffer.Take(result.Count));
                    } while (!result.EndOfMessage);
                }
                catch (ObjectDisposedException ode)
                {
                    Console.WriteLine($"WebSocket disposed: {ode.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    continue;
                }

                string jsonText = Encoding.UTF8.GetString(receivedData.ToArray()).Trim();
                if (string.IsNullOrEmpty(jsonText))
                    continue;

                JsonNode? root;
                try
                {
                    root = JsonNode.Parse(jsonText);
                }
                catch (JsonReaderException jrex)
                {
                    Console.WriteLine($"JSON parsing error: {jrex.Message}. Received text: {jsonText}");
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error during JSON parsing: {ex.Message}");
                    continue;
                }

                if (root is not JsonArray arr || arr.Count < 2)
                {
                    Console.WriteLine("Malformed OCPP message (not a valid array).");
                    continue;
                }

                int messageTypeId = arr[0]?.GetValue<int>() ?? -1;
                string uniqueId = arr[1]?.GetValue<string>() ?? "";

                try
                {
                    switch (messageTypeId)
                    {
                        case 2: // Call from station
                            string action = arr.Count > 2 ? arr[2]?.GetValue<string>() ?? "" : "";
                            JsonObject payload = arr.Count > 3 ? arr[3] as JsonObject ?? new JsonObject() : new JsonObject();
                            Console.WriteLine($"[OCPP] Call received: Action={action}, UniqueId={uniqueId}");
                            await ProcessCallAsync(ws, uniqueId, action, payload);
                            break;
                        case 3: // CallResult from station
                            Console.WriteLine($"[OCPP] CallResult received for UniqueId={uniqueId}");
                            break;
                        case 4: // CallError from station
                            Console.WriteLine($"[OCPP] CallError received for UniqueId={uniqueId}");
                            break;
                        default:
                            Console.WriteLine($"[OCPP] Unknown MessageTypeId={messageTypeId}");
                            break;
                    }
                }
                catch (ObjectDisposedException ode)
                {
                    Console.WriteLine($"Operation failed due to disposed object: {ode.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
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
                case "Authorize":
                    await HandleAuthorize(ws, uniqueId, payload);
                    break;
                case "MeterValues":
                    await HandleMeterValues(ws, uniqueId, payload);
                    break;
                case "StatusNotification":
                    await HandleStatusNotification(ws, uniqueId, payload);
                    break;
                case "StartTransaction":
                    await HandleStartTransaction(ws, uniqueId, payload);
                    break;
                case "StopTransaction":
                    await HandleStopTransaction(ws, uniqueId, payload);
                    break;
                case "RemoteStartTransaction":
                    await HandleRemoteStartTransaction(ws, uniqueId, payload);
                    break;
                case "RemoteStopTransaction":
                    await HandleRemoteStopTransaction(ws, uniqueId, payload);
                    break;
                // Add further cases for additional features/configuration keys
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

        // Handlers for various OCPP actions:
        private async Task HandleBootNotification(WebSocket ws, string uniqueId, JsonObject payload)
        {
            // Extract charger details (vendor, model, etc.)
            string vendor = payload["chargePointVendor"]?.GetValue<string>() ?? "UnknownVendor";
            string model = payload["chargePointModel"]?.GetValue<string>() ?? "UnknownModel";
            Console.WriteLine($"BootNotification from {vendor}/{model}");

            // Respond with a basic BootNotificationResponse
            var responsePayload = new JsonObject
            {
                ["currentTime"] = DateTime.UtcNow.ToString("o"),
                ["interval"] = 300, // heartbeat interval in seconds
                ["status"] = "Accepted"
            };

            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleHeartbeat(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("Heartbeat received.");
            var responsePayload = new JsonObject { ["currentTime"] = DateTime.UtcNow.ToString("o") };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleAuthorize(WebSocket ws, string uniqueId, JsonObject payload)
        {
            string idTag = payload["idTag"]?.GetValue<string>() ?? "Unknown";
            Console.WriteLine($"Authorize received for idTag: {idTag}");
            var responsePayload = new JsonObject { ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" } };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleMeterValues(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("MeterValues received.");
            // Here you would extract meter readings and other data.
            // For example, you might check for "MeterValuesSampleInterval" and "MeterValuesSampledData".
            var responsePayload = new JsonObject(); // Typically an empty payload for MeterValues response.
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleStatusNotification(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("StatusNotification received.");
            // Process status details (e.g., connectorId, errorCode, status, etc.)
            var responsePayload = new JsonObject(); // Typically empty in OCPP 1.6
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleStartTransaction(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("StartTransaction received.");
            // Extract details (connectorId, idTag, meterStart, timestamp, etc.)
            int transactionId = new Random().Next(10000, 99999); // Generate a dummy transaction ID
            var responsePayload = new JsonObject
            {
                ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" },
                ["transactionId"] = transactionId
            };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleStopTransaction(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("StopTransaction received.");
            // Process stopping transaction details (meterStop, timestamp, transactionId, etc.)
            var responsePayload = new JsonObject { ["idTagInfo"] = new JsonObject { ["status"] = "Accepted" } };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleRemoteStartTransaction(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("RemoteStartTransaction received.");
            // Process remote start details (e.g., idTag, connectorId, etc.)
            var responsePayload = new JsonObject { ["status"] = "Accepted" };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task HandleRemoteStopTransaction(WebSocket ws, string uniqueId, JsonObject payload)
        {
            Console.WriteLine("RemoteStopTransaction received.");
            // Process remote stop details (e.g., transactionId)
            var responsePayload = new JsonObject { ["status"] = "Accepted" };
            var callResult = new JsonArray { 3, uniqueId, responsePayload };
            await SendResponse(ws, callResult);
        }

        private async Task SendResponse(WebSocket ws, JsonArray response)
        {
            string json = response.ToJsonString();
            Console.WriteLine($"Sending OCPP response: {json}");
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

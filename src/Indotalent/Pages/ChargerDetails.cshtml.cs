using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces; // IOcppService
using Domain.Entities;
using Infrastructure;
using Infrastructure.Ocpp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Indotalent.Pages
{
    public class ChargerDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOcppService _ocpp;

        public ChargerDetailsModel(ApplicationDbContext dbContext, IOcppService ocpp)
        {
            _dbContext = dbContext;
            _ocpp = ocpp;
        }

        [BindProperty(SupportsGet = true)]
        public int ChargerId { get; set; }

        public ChargingStation? Charger { get; set; }

        // Controls bindings
        [BindProperty] public int ConnectorId { get; set; } = 1;
        [BindProperty] public string IdTag { get; set; } = "TEST123";
        [BindProperty] public int? TransactionId { get; set; }

        // Sessions for the table
        public List<ChargingSession> Sessions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();

            // Load the most recent 100 sessions for this station
            Sessions = await _dbContext.ChargingSession
                .Where(s => s.StationId == ChargerId)
                .OrderByDescending(s => s.StartTimeUtc)
                .Take(100)
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        // ===== Remote controls (kept) =====

        public async Task<IActionResult> OnPostStartAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();

            if (Charger?.OcppStationId is null)
            {
                ModelState.AddModelError(string.Empty, "Charger OCPP ID not found.");
                return await OnGetAsync();
            }

            try
            {
                if (ConnectorId <= 0) ConnectorId = 1;
                if (string.IsNullOrWhiteSpace(IdTag)) IdTag = "TEST123";

                await _ocpp.SendRemoteStartTransactionAsync(Charger.OcppStationId, ConnectorId, IdTag);
                TempData["Flash"] = $"RemoteStartTransaction sent to {Charger.OcppStationId} (connector {ConnectorId}, idTag {IdTag}).";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to send RemoteStartTransaction: {ex.Message}");
            }

            return RedirectToPage(new { chargerId = ChargerId });
        }

        public async Task<IActionResult> OnPostStopAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();

            if (Charger?.OcppStationId is null)
            {
                ModelState.AddModelError(string.Empty, "Charger OCPP ID not found.");
                return await OnGetAsync();
            }

            if (TransactionId is null)
            {
                ModelState.AddModelError(string.Empty, "Please provide a Transaction ID to stop.");
                return await OnGetAsync();
            }

            try
            {
                await _ocpp.SendRemoteStopTransactionAsync(Charger.OcppStationId, TransactionId.Value);
                TempData["Flash"] = $"RemoteStopTransaction({TransactionId.Value}) sent to {Charger.OcppStationId}.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to send RemoteStopTransaction: {ex.Message}");
            }

            return RedirectToPage(new { chargerId = ChargerId });
        }

        public async Task<IActionResult> OnPostTriggerMeterValuesAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();
            if (Charger?.OcppStationId is null)
            {
                ModelState.AddModelError(string.Empty, "Charger OCPP ID not found.");
                return await OnGetAsync();
            }

            try
            {
                await _ocpp.SendTriggerMessageAsync(Charger.OcppStationId, "MeterValues");
                TempData["Flash"] = $"TriggerMessage(MeterValues) sent to {Charger.OcppStationId}.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to trigger MeterValues: {ex.Message}");
            }

            return RedirectToPage(new { chargerId = ChargerId });
        }

        public async Task<IActionResult> OnPostTriggerHeartbeatAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();
            if (Charger?.OcppStationId is null)
            {
                ModelState.AddModelError(string.Empty, "Charger OCPP ID not found.");
                return await OnGetAsync();
            }

            try
            {
                await _ocpp.SendTriggerMessageAsync(Charger.OcppStationId, "Heartbeat");
                TempData["Flash"] = $"TriggerMessage(Heartbeat) sent to {Charger.OcppStationId}.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to trigger Heartbeat: {ex.Message}");
            }

            return RedirectToPage(new { chargerId = ChargerId });
        }

        // ===== Export sessions to CSV (simple) =====
        public async Task<IActionResult> OnPostExportCsvAsync()
        {
            var station = await _dbContext.ChargingStations.FindAsync(ChargerId);
            if (station == null) return NotFound();

            var sessions = await _dbContext.ChargingSession
                .Where(s => s.StationId == ChargerId)
                .OrderByDescending(s => s.StartTimeUtc)
                .Take(1000)
                .AsNoTracking()
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Station,User,Rfid,Date,Start,End,Duration,Energy_kWh,Cost,Currency,TransactionId");

            foreach (var s in sessions)
            {
                var start = s.StartTimeUtc.ToLocalTime();
                var end = s.EndTimeUtc?.ToLocalTime();
                var duration = s.DurationSec.HasValue
                    ? TimeSpan.FromSeconds(s.DurationSec.Value)
                    : (end.HasValue ? end.Value - start : (TimeSpan?)null);

                var energy = s.EnergyKWh.HasValue ? s.EnergyKWh.Value.ToString("0.###") : "";
                var cost = s.Cost.HasValue ? s.Cost.Value.ToString("0.##") : "";
                var currency = s.Currency ?? "";

                sb.AppendLine(string.Join(",", new[]
                {
                    Quote(station.ChargerName ?? station.OcppStationId),
                    Quote(""),                          // User (unknown)
                    Quote(s.IdTag ?? ""),
                    Quote(start.ToString("dd/MM/yyyy")),
                    Quote(start.ToString("HH:mm")),
                    Quote(end.HasValue ? end.Value.ToString("HH:mm") : ""),
                    Quote(duration.HasValue ? duration.Value.ToString(@"hh\:mm\:ss") : ""),
                    Quote(energy),
                    Quote(cost),
                    Quote(currency),
                    Quote(s.TransactionId?.ToString() ?? "")
                }));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"sessions_{(station.ChargerName ?? station.OcppStationId)}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);

            static string Quote(string value) => $"\"{value?.Replace("\"", "\"\"")}\"";
        }

        private async Task<bool> LoadChargerAsync()
        {
            Charger = await _dbContext.ChargingStations
                        .Include(cs => cs.Network)
                        .FirstOrDefaultAsync(cs => cs.Id == ChargerId);
            return Charger != null;
        }
    }
}

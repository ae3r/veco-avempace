using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Infrastructure;                 // ApplicationDbContext
using Domain.Entities;
using System.Threading.Tasks;
using System;                         // Exception
using Infrastructure.Ocpp;            // IOcppService

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
        [BindProperty]
        public int ConnectorId { get; set; } = 1;

        [BindProperty]
        public string IdTag { get; set; } = "TEST123";

        [BindProperty]
        public int? TransactionId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostStartAsync()
        {
            if (!await LoadChargerAsync()) return NotFound();

            if (Charger?.OcppStationId is null)
            {
                ModelState.AddModelError(string.Empty, "Charger OCPP ID not found.");
                return Page();
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
                return Page();
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
                return Page();
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
                return Page();
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

        private async Task<bool> LoadChargerAsync()
        {
            Charger = await _dbContext.ChargingStations
                                      .Include(cs => cs.Network)
                                      .FirstOrDefaultAsync(cs => cs.Id == ChargerId);
            return Charger != null;
        }
    }
}

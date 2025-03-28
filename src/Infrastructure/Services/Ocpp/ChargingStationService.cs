using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Ocpp
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly ApplicationDbContext _context;

        public ChargingStationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateStationStatusAsync(ChargingStation station)
        {
            // Try to get an existing station by its OcppStationId.
            var existing = await _context.ChargingStations
                .FirstOrDefaultAsync(cs => cs.OcppStationId == station.OcppStationId);

            if (existing == null)
            {
                // If not found, add the new station.
                _context.ChargingStations.Add(station);
            }
            else
            {
                // Update the existing station.
                existing.Model = station.Model;
                existing.BootTime = station.BootTime;
                existing.LastHeartbeat = station.LastHeartbeat;
                existing.ChargerStatus = station.ChargerStatus;
                // Optionally update other fields as needed.
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ChargingStation>> GetAllStationsAsync() =>
            await _context.ChargingStations.ToListAsync();

        public async Task<ChargingStation?> GetStationByIdAsync(string stationId)
        {
            if (int.TryParse(stationId, out int id))
            {
                return await _context.ChargingStations.FindAsync(id);
            }
            return null;
        }

        // New method: get station by OcppStationId
        public async Task<ChargingStation?> GetStationByOcppIdAsync(string ocppStationId) =>
            await _context.ChargingStations.FirstOrDefaultAsync(cs => cs.OcppStationId == ocppStationId);
    }
}

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
                existing.Model = station.Model;
                existing.BootTime = station.BootTime;
                existing.LastHeartbeat = station.LastHeartbeat;
                existing.ChargerStatus = station.ChargerStatus;
                existing.ConnectionStatus = station.ConnectionStatus ?? "Disconnected";
                existing.ChargerName = station.ChargerName ?? existing.ChargerName;
                existing.SerialNumber = station.SerialNumber ?? existing.SerialNumber;
                existing.Puk = station.Puk ?? existing.Puk;
                existing.PowerValue = station.PowerValue;
                existing.PhotoUrl = station.PhotoUrl ?? existing.PhotoUrl;

                // new custom fields
                existing.Vehicle = station.Vehicle ?? existing.Vehicle;
                existing.Access = station.Access ?? existing.Access;
                existing.SelfConsumption = station.SelfConsumption ?? existing.SelfConsumption;
                existing.Internet = station.Internet ?? existing.Internet;
                existing.Scheduling = station.Scheduling ?? existing.Scheduling;
                existing.MeterNominalPower = station.MeterNominalPower ?? existing.MeterNominalPower;

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IChargingStationService
    {
        Task UpdateStationStatusAsync(ChargingStation station);
        Task<IEnumerable<ChargingStation>> GetAllStationsAsync();
        Task<ChargingStation?> GetStationByIdAsync(string stationId);

        // NEW method to get a station by its OCPP identifier:
        Task<ChargingStation?> GetStationByOcppIdAsync(string ocppStationId);
    }
}

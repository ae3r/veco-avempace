using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Entities;
using Application.Common.Interfaces;

namespace Indotalemplace.Pages
{
    public class StationStatusModel : PageModel
    {
        private readonly IChargingStationService _chargingStationService;
        public IEnumerable<ChargingStation> Stations { get; set; } = Enumerable.Empty<ChargingStation>();

        public StationStatusModel(IChargingStationService chargingStationService)
        {
            _chargingStationService = chargingStationService;
        }

        public async Task OnGetAsync()
        {
            Stations = await _chargingStationService.GetAllStationsAsync();
        }
    }
}

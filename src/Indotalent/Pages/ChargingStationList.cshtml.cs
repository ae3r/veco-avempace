using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace Indotalent.Pages
{
    public class ChargingStationListModel : PageModel
    {
        public List<ChargerStationData> Stations { get; set; }

        public void OnGet()
        {
            Stations = ChargingStationStore.Stations;

            // For each station, recalc the dynamic power
            foreach (var station in Stations)
            {
                // Always start from 50 if we are doing the hourly logic 
                var hoursPassed = (int)(DateTime.Now - station.CreatedAt).TotalHours;
                var dynamicKW = 22 - (hoursPassed * 10);

                if (dynamicKW <= 0)
                {
                    dynamicKW = 22;
                }

                station.PowerKW = dynamicKW + " kW";
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace Indotalent.Pages
{
    public class NetworkDetailsModel : PageModel
    {
        public string NetworkName { get; set; }
        public string ActiveTab { get; set; } = "chargers";

        public List<ChargerStationData> ChargingStations { get; set; }
        public List<SessionRow> Sessions { get; set; }

        // We'll store the sum of all sessions' kW in this property
        public double TotalEnergy { get; set; }

        public void OnGet(string networkName, string tab)
        {
            NetworkName = networkName ?? "My Network";
            ActiveTab = string.IsNullOrEmpty(tab) ? "chargers" : tab;

            // Load stations from the store
            ChargingStations = ChargingStationStore.Stations;

            // Only build sessions if needed (for sessions or energy tab)
            if (ActiveTab == "sessions" || ActiveTab == "energy")
            {
                Sessions = BuildSessionsForAllStations();

                if (ActiveTab == "energy")
                {
                    // Sum up the numeric portion from each session's kWString
                    TotalEnergy = 0.0;
                    foreach (var s in Sessions)
                    {
                        // s.kWString might be "44 kW"
                        var trimmed = s.kWString.Replace("kW", "").Trim(); // e.g. "44"
                        if (double.TryParse(trimmed, out double val))
                        {
                            TotalEnergy += val; // e.g. 44
                        }
                    }
                }
            }
        }

        private List<SessionRow> BuildSessionsForAllStations()
        {
            var sessions = new List<SessionRow>();

            // Example: for each station, we pretend there's a 2-hour session
            foreach (var stn in ChargingStations)
            {
                // Fake times
                var date = new DateTime(2025, 1, 30);
                var start = date.AddHours(8);
                var end = date.AddHours(10);

                var diff = end - start; // 2 hours
                double hours = diff.TotalHours;

                // Parse the station's power (e.g. "22 kW")
                double stationPower = 22; // default
                if (!string.IsNullOrEmpty(stn.PowerKW))
                {
                    var trimmed = stn.PowerKW.Replace("kW", "").Trim();
                    double.TryParse(trimmed, out stationPower);
                }
                double totalConsumed = stationPower * hours; // e.g. 44 if 22 * 2

                sessions.Add(new SessionRow
                {
                    StationName = stn.ChargerName,
                    DateString = "30/01/2025",
                    StartTimeString = "08:00",
                    EndTimeString = "10:00",
                    DurationString = "02:00",
                    kWString = $"{totalConsumed} kW"
                });
            }

            return sessions;
        }
    }

    public class SessionRow
    {
        public string StationName { get; set; }
        public string DateString { get; set; }
        public string StartTimeString { get; set; }
        public string EndTimeString { get; set; }
        public string DurationString { get; set; }
        public string kWString { get; set; }
    }
}

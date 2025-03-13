using System.Collections.Generic;

namespace Indotalent.Pages
{
    public static class ChargingStationStore
    {
        // An in-memory list of stations
        public static List<ChargerStationData> Stations { get; } = new List<ChargerStationData>();
    }

    // Renamed class from ChargingStationDto to ChargerStationData
    public class ChargerStationData
    {
        public string ChargerName { get; set; }
        public string SerialNumber { get; set; }
        public string Puk { get; set; }

        public string Status { get; set; } // "In charging", "In standby", etc.
        public string PowerKW { get; set; } // e.g. "10.953 kW"

        public DateTime CreatedAt { get; set; }
    }
}

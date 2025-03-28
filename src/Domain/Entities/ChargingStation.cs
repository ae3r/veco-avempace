using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ChargingStation
    {
        public int Id { get; set; }
        public DateTime? BootTime { get; set; }
        public DateTime? LastHeartbeat { get; set; }

        // New: Unique identifier provided by the charging station (from the OCPP URL)
        public string OcppStationId { get; set; }
        public string ChargerName { get; set; }
        public string ChargerStatus { get; set; }
        public string ConnectionStatus { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }   // New: Serial number of the station
        public string Puk { get; set; }            // New: PUK code
        public double PowerValue { get; set; }     // New: Power in kW (store numeric value)
        // New property: URL for the photo of the charging station
        public string? PhotoUrl { get; set; }
        // NEW nullable custom fields
        public string? Vehicle { get; set; }
        public string? Access { get; set; }
        public string? SelfConsumption { get; set; }
        public string? Internet { get; set; }
        public string? Scheduling { get; set; }
        public string? MeterNominalPower { get; set; }

        // Foreign key to Network
        public int NetworkId { get; set; }
        public Network Network { get; set; }
    }
}

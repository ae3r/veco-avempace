using System;

namespace Domain.Entities
{
    public class ChargingSession
    {
        public int Id { get; set; }

        // FK to ChargingStation
        public int StationId { get; set; }
        public ChargingStation Station { get; set; }

        // Transaction context
        public int? TransactionId { get; set; }    // OCPP transaction id (returned by us)
        public int ConnectorId { get; set; } = 1;
        public string? IdTag { get; set; }

        // Timing
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public int? DurationSec { get; set; }      // computed at stop
        public DateTime? LastUpdateUtc { get; set; }

        // Energy / meter
        public int? StartMeterWh { get; set; }     // from StartTransaction.meterStart if provided
        public int? StopMeterWh { get; set; }      // from StopTransaction.meterStop if provided
        public decimal? EnergyKWh { get; set; }    // computed ((Stop-Start)/1000) or from live deltas

        // Optional billing
        public decimal? Cost { get; set; }
        public string? Currency { get; set; }      // e.g., "EUR"
    }
}

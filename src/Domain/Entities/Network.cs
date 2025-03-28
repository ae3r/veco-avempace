using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Network
    {
        public int Id { get; set; }
        public string NetworkName { get; set; }
        public string? PhotoUrl { get; set; }
        public string Address { get; set; }
        public string? Plant { get; set; }
        public string? NetworkLayout { get; set; }
        public int? NumberOfChargers { get; set; }
        public int? NumberOfUsers { get; set; }

        // New fields to store detailed address information:
        public string Street { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }

        // Relationship: One Network can have many ChargingStations
        public ICollection<ChargingStation> ChargingStations { get; set; }
            = new List<ChargingStation>();
    }
}

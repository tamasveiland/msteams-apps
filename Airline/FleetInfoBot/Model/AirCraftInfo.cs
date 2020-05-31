using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.FleetInfoBot.Web.Model
{
    public class AirCraftInfo
    {
        [JsonProperty(PropertyName = "flightNumber")]
        public string FlightNumber { get; set; }
        [JsonProperty(PropertyName = "model")]

        
        public string Model { get; set; }
        [JsonProperty(PropertyName = "capacity")]
        public string Capacity { get; set; }
        [JsonProperty(PropertyName = "flightType")]
        public string FlightType { get; set; }
        [JsonProperty(PropertyName = "baseLocation")]
        public string BaseLocation { get; set; }

        [JsonProperty(PropertyName = "aircraftId")]
        public string AircraftId { get; set; }

        [JsonProperty(PropertyName = "status")]
        public Status Status { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }

    public enum Status
    {
        Available = 0,
        Assigned = 1,
        Grounded=2
    }

    public class FlightInfo
    {
        [JsonProperty(PropertyName = "flightNumber")]
        public string FlightNumber { get; set; }
        [JsonProperty(PropertyName = "flightName")]
        public string FlightName { get; set; }

        [JsonProperty(PropertyName = "journeyDate")]
        public DateTime JourneyDate { get; set; }
        [JsonProperty(PropertyName = "seatCount")]
        public int SeatCount { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "arrival")]
        public DateTime Arrival { get; set; }

        [JsonProperty(PropertyName = "departure")]
        public DateTime Departure { get; set; }

        [JsonProperty(PropertyName = "fromCity")]
        public string FromCity { get; set; }

        [JsonProperty(PropertyName = "toCity")]
        public string ToCity { get; set; }
        [JsonProperty(PropertyName = "pnr")]
        public string PNR { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }


}
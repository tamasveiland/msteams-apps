using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.FlightInfoBot.Web.Model
{
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
        public DateTime Arrival{get;set;}

        [JsonProperty(PropertyName = "departure")]
        public DateTime Departure { get; set; }

        [JsonProperty(PropertyName = "fromCity")]
        public string FromCity { get; set; }

        [JsonProperty(PropertyName = "toCity")]
        public string ToCity { get; set; }
        [JsonProperty(PropertyName = "pnr")]
        public string PNR { get; set; }
    }

    public class Cities
    {
        [JsonProperty(PropertyName = "cityName")]
        public string CityName { get; set; }
        [JsonProperty(PropertyName = "cityCode")]
        public string CityCode { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }
    }
}
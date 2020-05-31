using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.BaggageInfoBot.Web.Model
{
    public class Baggage
    {
        [JsonProperty(PropertyName = "pnr")]
        public string PNR { get; set; }

        [JsonProperty(PropertyName = "bagCount")]
        public int BagCount { get; set; }

        [JsonProperty(PropertyName = "currentStatus")]
        public string CurrentStatus { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "ticketNo")]
        public string TicketNo { get; set; }

        [JsonProperty(PropertyName = "seatNo")]
        public string SeatNo { get; set; }

        [JsonProperty(PropertyName = "gender")]
        public string Gender { get; set; }
        [JsonProperty(PropertyName = "flightNumber")]
        public string FlightNumber { get; set; }
        [JsonProperty(PropertyName = "from")]
        public string From { get; set; }

        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }

        [JsonProperty(PropertyName = "baggageIdentifer")]
        public int BaggageIdentifer { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }


    //public class Flight
    //{
    //    public string FlightNumber { get; set; }
    //    public string From { get; set; }
    //    public string To { get; set; }
    //    public string PNR { get; set; }
    //}
    //public class Passenger
    //{
    //    public string Name { get; set; }
    //    public string TicketNo { get; set; }
    //    public string SeatNo { get; set; }
    //    public string Gender { get; set; }
    //    public string Pnr { get; internal set; }
    //}


}
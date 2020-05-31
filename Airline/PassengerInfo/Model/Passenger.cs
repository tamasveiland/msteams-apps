using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.PassengerInfo.Web.Model
{
    public class Passenger
    {
        public string Name { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public string GateNumber { get; set; }

        public Gender Gender { get; set; }

        public DateTime DepartureTime { get; set; }

        public DateTime BoardingTime { get; set; }

        public string Seat { get; set; }

        public string Class { get; set; }

        public string FlightNumber { get; set; }

        public string PNR { get; set; }

        public string FrequentFlyerNumber { get; set; }

        public LoyaltyStatus LoyaltyStatus { get; set; }

        public string SpecialAssistance { get; set; }

        public string Zone { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            });
        }
    }

    public enum Gender
    {
        Male = 1,
        Female = 2
    }

    public enum LoyaltyStatus
    {
        None = 0,
        Gold = 1,
        Platinum = 2,
        Diamond = 3

    }
    
}
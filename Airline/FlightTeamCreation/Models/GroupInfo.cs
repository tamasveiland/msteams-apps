using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.FlightTeamCreation.Web.Models
{

    public class GroupInfo
    {
        public string description { get; set; }
        public string displayName { get; set; }
        public string[] groupTypes { get; set; }
        public bool mailEnabled { get; set; }
        public string mailNickname { get; set; }
        public bool securityEnabled { get; set; }
    }



}
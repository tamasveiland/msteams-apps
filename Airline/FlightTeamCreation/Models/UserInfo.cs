using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.FlightTeamCreation.Web.Models
{
    public class UserInfo
    {
        public string accessToken { get; set; }
        public string idToken { get; set; }
        public string tokenType { get; set; }
        public string expiresIn { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }

        public DateTime ExpiryTime { get; set; }

    }
}
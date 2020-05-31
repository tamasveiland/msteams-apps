using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airline.FlightTeamCreation.Web.Models
{
    public class TeamInfoData

    {
        public Membersettings memberSettings { get; set; }
        public Messagingsettings messagingSettings { get; set; }
        public Funsettings funSettings { get; set; }
    }

    public class Membersettings
    {
        public bool allowCreateUpdateChannels { get; set; }
    }

    public class Messagingsettings
    {
        public bool allowUserEditMessages { get; set; }
        public bool allowUserDeleteMessages { get; set; }
    }

    public class Funsettings
    {
        public bool allowGiphy { get; set; }
        public string giphyContentRating { get; set; }
    }

}
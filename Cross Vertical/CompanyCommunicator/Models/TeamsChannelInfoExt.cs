using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrossVertical.Announcement.Models
{
    public class TeamsChannelDataExt : TeamsChannelData
    {
        public new TeamInfoExt Team { get; set; }
    }

    public class TeamInfoExt: TeamInfo
    {
        [JsonProperty("aadGroupId")]
        public string AADGroupId { get; set; }
    }
}
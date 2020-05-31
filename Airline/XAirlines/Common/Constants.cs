using Airlines.XAirlines.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines.XAirlines.Common
{
    public class Constants
    {
        public const string NextWeekRoster = "my next week roster";
        public const string UpdateCard = "update";
        public const string NextMonthRoster = "my next month roster";
        public const string ShowDetailedRoster = "show detailed roster";
        public const string WeatherCard = "get weather card";
        public const string CurrencyCard = "get currency card";
        public static string PortalTabDeeplink { get; set; } = $"https://teams.microsoft.com/l/entity/{ApplicationSettings.AppId}/com.contoso.Airlines.portal?webUrl={HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "/portal")}&label=Portal";
    }
}
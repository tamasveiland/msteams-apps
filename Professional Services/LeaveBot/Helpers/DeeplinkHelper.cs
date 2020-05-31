using ProfessionalServices.LeaveBot.Helper;
using System.Web;

namespace ProfessionalServices.LeaveBot.Helpers
{
    public static class DeeplinkHelper
    {
        public static string GetLeaveBoardDeeplink(string emailId)
        {
            return $"https://teams.microsoft.com/l/entity/{ApplicationSettings.AppId}/com.contoso.LeaveBot.leaveboard?webUrl={HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "?EmailId=" + emailId)}&label=Leave%20Board";
        }

        public static string PublicHolidaysDeeplink { get; set; } =
            $"https://teams.microsoft.com/l/entity/{ApplicationSettings.AppId}/com.contoso.LeaveBot.holidays?webUrl={HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "/first")}&label=Public%20Holidays";

        public static string HelpDeeplink { get; set; } =
            $"https://teams.microsoft.com/l/entity/{ApplicationSettings.AppId}/com.contoso.LeaveBot.help?webUrl={HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "/second")}&label=Help";
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
using CrossVertical.Announcement.Helper;
using System.Web;

namespace CrossVertical.Announcement.Helpers
{
    public class Constants
    {
        public const string AllEmployeesGroupAndTeamName = "All Employees";

        public const string ConfigureAdminSettings = "ConfigureAdminSettings";
        public const string ShowAllDrafts = "ShowDrafts";
        public const string ShowAnnouncement = "ShowAnnouncementDraft";
        public const string ShowSentAnnouncement = "ShowSentAnnouncement";
        public const string Cancel = "Cancel";
        public const string ShowRecents = "ShowRecents";

        public const string CreateOrEditAnnouncement = "CreateOrEditAnnouncement";
        public const string EditAnnouncementFromTab = "EditAnnouncementFromTab";
        public const string ScheduleAnnouncement = "ScheduleAnnouncement";
        public const string ShowMoreDetails = "ShowMoreDetails";
        public const string SendAnnouncement = "SendAnnouncement";

        public const string CreateGroupWithAllEmployees = "CreateGroupWithAllEmployee";
        public const string CreateTeamsWithAllEmployees = "CreateTeamsWithAllEmployees";
        
        public const string ConfigureGroups = "ConfigureGroups";
        public const string SetModerators = "SetModerators";

        public const string ShowEditAnnouncementTaskModule = "ShowEditAnnouncementTaskModule";

        public const string Acknowledge = "Acknowledge";
        public const string ContactSender = "Contact Sender";
        public const string ShowWelcomeScreen = "ShowWelcomeScreen";

        public const string ProfileKey = "profile";

        public static string HistoryTabDeeplink { get; set; } = $"https://teams.microsoft.com/l/entity/{ApplicationSettings.AppId}/com.contoso.Announcement.history?webUrl={HttpUtility.UrlEncode(ApplicationSettings.BaseUrl + "/history?tid={tid}")}&label=History";

    }
}
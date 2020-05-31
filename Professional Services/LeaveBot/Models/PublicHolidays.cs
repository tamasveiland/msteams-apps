using ProfessionalServices.LeaveBot.Helper;
using System;
using System.Collections.Generic;

namespace ProfessionalServices.LeaveBot.Models
{
    // Static class
    public static class PublicHolidaysList
    {
        public static string Title = "Public Holidays" + " " + DateTime.Now.Year;

        public static List<PublicHoliday> HolidayList { get; set; } = new List<PublicHoliday>()
        {
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 01, 01), Title = "New Years Day",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 01, 15), Title = "Pongal",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 04, 19), Title = "Good Friday",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 05,01 ), Title = "May Day",ImagePath = null,OptionalHoliday = "Optional",CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 06, 05), Title = "Eid Ul Fitr",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 08, 15), Title = "Independence Day",ImagePath = ApplicationSettings.BaseUrl + "/Resources/Diwali.PNG",OptionalHoliday = null,CelebrationText = "Office Celebrations on a day before" },
             new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 10, 02), Title = "Gandhi Jayanthi",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 09, 02), Title = "Ganesh Chaviti",ImagePath = null,OptionalHoliday = null,CelebrationText = null },

            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 09, 08), Title = "Dussehra",ImagePath = null,OptionalHoliday = null,CelebrationText = null },

            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 10, 28), Title = "Diwali",ImagePath = ApplicationSettings.BaseUrl + "/Resources/Diwali.PNG",OptionalHoliday = null,CelebrationText = "Office Celebrations on a day before" },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 11, 01), Title = "Kannada Rajyotsava",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
            new PublicHoliday() {Date = new DateTime(DateTime.Now.Year, 12, 25), Title = "Christmas",ImagePath = null,OptionalHoliday = null,CelebrationText = null },
        };
    }

    public class PublicHoliday
    {
        public DateTime Date { get; set; }

        public string Title { get; set; }

        public string ImagePath { get; set; }

        public string OptionalHoliday { get; set; }

        public string CelebrationText { get; set; }
    }
}
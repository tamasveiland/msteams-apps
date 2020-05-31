using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Airlines.XAirlines.ViewModels
{
    public class PortalViewModel
    {
        public PortalViewModel(DateTime today, int numberOfDays, int numberOfMonths, List<Duty> duties)
        {
            int maxDays = numberOfDays;
            int maxMonths = numberOfMonths;
            var currentDate = today;
            int daysCount = 0;
            int monthCount = 0;
            int weekNumber = 0;
            do
            {
                var monthlySchedule = Monthly.FirstOrDefault(m => m.Month == currentDate.Month && m.Year == currentDate.Year);
                if (monthlySchedule == null)
                {
                    monthlySchedule = new MonthSchedule() { Month = currentDate.Month, MonthName = currentDate.ToString("MMMM"), Year = currentDate.Year };
                    monthCount += 1;
                    if (monthCount > maxMonths)
                        break;

                    Monthly.Add(monthlySchedule);
                    weekNumber = 1;
                }
                var weeklySchedule = monthlySchedule.Weekly.FirstOrDefault(m => m.WeekNumber == weekNumber);
                if (weeklySchedule == null)
                {
                    weeklySchedule = new WeekSchedule() { WeekNumber = weekNumber, DayOfWeek = (int)currentDate.DayOfWeek };
                    monthlySchedule.Weekly.Add(weeklySchedule);
                }
                var duty = duties.FirstOrDefault(d => d.Date.Date == currentDate.Date);
                weeklySchedule.Daily.Add(new Duty() { Date = currentDate,vacationPlan=duty.vacationPlan,isDayOff=duty.isDayOff, Details = duty?.Details });
                daysCount++;
                currentDate = currentDate.AddDays(1);
                if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                    weekNumber++;
            }
            while (daysCount < maxDays);
        }
        public List<MonthSchedule> Monthly { get; set; } = new List<MonthSchedule>();
        public string UserEmailId { get; set; }
    }
    public class MonthSchedule
    {
        public int Month { get; set; }
        public string MonthName { get; set; }
        public int Year { get; set; }
        public List<WeekSchedule> Weekly { get; set; } = new List<WeekSchedule>();
    }
    public class WeekSchedule
    {
        public int DayOfWeek { get; set; }
        public int WeekNumber { get; set; }
        public List<Duty> Daily { get; set; } = new List<Duty>();
    }
    public class Duty
    {
        public DateTime Date { get; set; }
        public Details Details { get; set; }
        public bool vacationPlan { get; set; }
        public bool isDayOff { get; set; }
    }
    public class Details
    {
        public string DisplayText { get; set; }
        public string Colour { get; set; }
        public string Event2 { get; set; }
    }
}
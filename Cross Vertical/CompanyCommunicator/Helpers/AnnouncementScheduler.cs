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
using CrossVertical.Announcement.Repository;
using System;
using System.Threading.Tasks;

namespace CrossVertical.Announcement.Helpers
{
    public static class AnnouncementScheduler
    {
        public static async Task InitializeSchedulesFromDB()
        {
            Scheduler.InitializeScheduler(new TimeSpan(0, 30, 0));

            var allScheduledTask = await Cache.Announcements.GetAllItemsAsync(announcement =>
                        announcement.Type == typeof(Models.Campaign).Name &&
                        announcement != null &&
                        announcement.Schedule != null &&
                        announcement.Status == Models.Status.Scheduled);

            foreach (var announcement in allScheduledTask)
            {
                announcement.Schedule.ScheduleId = Scheduler.AddSchedule(
                    announcement.Schedule.GetScheduleTimeUTC(),
                    new AnnouncementSender()
                    {
                        AnnouncementId = announcement.Id
                    }.Execute);

                await Cache.Announcements.AddOrUpdateItemAsync(announcement.Id, announcement);
            }
        }

        public static async Task CleanUpOldSchedules()
        {
            var allScheduledTask = await Cache.Announcements.GetAllItemsAsync(announcement =>
                        announcement.Type == typeof(Models.Campaign).Name &&
                        announcement != null &&
                        announcement.Schedule != null &&
                        announcement.Status == Models.Status.Scheduled);

            foreach (var announcement in allScheduledTask)
            {
                Scheduler.RemoveSchedule(announcement.Id);
            }
        }
    }
}
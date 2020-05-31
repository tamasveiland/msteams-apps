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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CrossVertical.Announcement.Helpers
{

    public static class Scheduler
    {
        private static Timer mainScheduleTimer;
        private static Dictionary<string, Timer> ScheduledTimers = new Dictionary<string, Timer>();
        private static Dictionary<string, ScheduleDetails> ScheduleQueue { get; set; } = new Dictionary<string, ScheduleDetails>();
        private static TimeSpan _timespan;
        private static readonly int offset = 10;// Milliseconds


        public static void InitializeScheduler(TimeSpan interval)
        {
            _timespan = interval;
            mainScheduleTimer = new Timer(interval.TotalMilliseconds);
            // Hook up the Elapsed event for the timer.
            mainScheduleTimer.Elapsed += OnTimedEvent;
            mainScheduleTimer.AutoReset = true;
        }

        private static void CheckAndEnableScheduler()
        {
            mainScheduleTimer.Enabled = ScheduleQueue.Where(s => !s.Value.IsScheduled).Count() != 0;
        }

        public static string AddSchedule(DateTime dateTime, Func<Task> action)
        {
            var schedule = new ScheduleDetails()
            {
                Id = Guid.NewGuid().ToString(),
                ExecutionTime = dateTime,
                Action = action
            };
            ScheduleQueue.Add(schedule.Id, schedule);
            RefreshSchedules();
            return schedule.Id;
        }

        public static bool UpdateSchedule(string scheduleId, DateTime updateDateTime)
        {
            if (scheduleId == null || !ScheduleQueue.ContainsKey(scheduleId) )
            {
                return false;
            }

            ScheduleQueue[scheduleId].ExecutionTime = updateDateTime;

            if (ScheduledTimers.ContainsKey(scheduleId))
            {
                var diff = ScheduleQueue[scheduleId].ExecutionTime - DateTime.UtcNow;
                if (diff.TotalMilliseconds < (_timespan.TotalMilliseconds - offset))
                {
                    ScheduledTimers[scheduleId].Interval = diff.TotalMilliseconds > 0 ? diff.TotalMilliseconds : 1;
                }
            }
            else
                RefreshSchedules();
            return true;
        }

        public static bool RemoveSchedule(string scheduleId)
        {
            if (!ScheduleQueue.ContainsKey(scheduleId))
            {
                return false;
            }
            ScheduleQueue.Remove(scheduleId);
            ClenupScheduleTimer(scheduleId);
            return true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            RefreshSchedules();
        }

        private static void RefreshSchedules()
        {
            var toSchedule = ScheduleQueue.Where(s => !s.Value.IsScheduled).OrderByDescending(s => s.Value.ExecutionTime);
            foreach (var schedule in toSchedule)
            {
                var diff = schedule.Value.ExecutionTime - DateTime.UtcNow;
                if (diff.TotalMilliseconds <= (_timespan.TotalMilliseconds - offset))// Just to be at safer side
                {
                    schedule.Value.IsScheduled = true;

                    // Create schedule
                    // scheduleTimers.Add();
                    var timer = new Timer();
                    timer.Elapsed += (sender, e) => OnTimedEvent(sender, e, schedule.Key);
                    ScheduledTimers.Add(schedule.Key, timer);
                    timer.AutoReset = false;
                    timer.Interval = diff.TotalMilliseconds > 0 ? diff.TotalMilliseconds : 1;
                    timer.Enabled = true;
                }
            }
            CheckAndEnableScheduler();
        }

        public static void Stop()
        {
            mainScheduleTimer.Stop();
            mainScheduleTimer.Elapsed -= OnTimedEvent;
            mainScheduleTimer.Dispose();
        }

        static void OnTimedEvent(object sender, ElapsedEventArgs e, string id)
        {
            ClenupScheduleTimer(id);
            if (ScheduleQueue.ContainsKey(id))
            {
                ScheduleQueue[id].Action();
                ScheduleQueue.Remove(id);
            }
        }

        private static void ClenupScheduleTimer(string id)
        {
            if (ScheduledTimers.ContainsKey(id))
            {
                ScheduledTimers[id].Stop();
                // Clean the event
                ScheduledTimers[id].Elapsed -= (s, ex) => { };
                // Clean up the handlers
                ScheduledTimers[id].Dispose();
                ScheduledTimers[id] = null;
                ScheduledTimers.Remove(id);
            }
            CheckAndEnableScheduler();
        }
    }

    internal class ScheduleDetails
    {
        public DateTime ExecutionTime { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public Func<Task> Action { get; set; }
        internal bool IsScheduled { get; set; }
    }
}

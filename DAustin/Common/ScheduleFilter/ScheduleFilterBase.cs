using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.DAustin.Common.ScheduleFilter
{
    public abstract class ScheduleFilterBase<T> where T : OptimizationParametersBase.DOWTimeblock
    {
        protected readonly TimeWindowTimeZone _timeZone;
        protected readonly string _anchorTime;
        protected readonly List<T> _schedules;
        protected DateTime _anchorDateTime;

        protected ScheduleFilterBase(
            TimeWindowTimeZone timeZone,
            string anchorTime,
            List<T> schedules)
        {
            _timeZone = timeZone;
            _anchorTime = anchorTime;
            _schedules = schedules ?? new List<T>();
            _anchorDateTime = DateTime.Today; // Default to midnight

            // Parse anchor time (format: "HH:mm")
            if (!string.IsNullOrEmpty(_anchorTime))
            {
                _anchorDateTime = DateTime.Parse(anchorTime);
            }
        }

        /// <summary>
        /// Finds the active schedule for the current bar time
        /// </summary>
        protected T GetActiveSchedule(DateTime currentBarTime)
        {
            if (_schedules == null || _schedules.Count == 0)
                return null;

            // Convert to target timezone
            DateTime targetTime = ConvertToTimeZone(currentBarTime);
            DADayOfWeek currentDayOfWeek = ConvertToDayOfWeek(targetTime.DayOfWeek);

            // Check each schedule
            foreach (var schedule in _schedules)
            {
                // Skip if day of week doesn't match (if specified)
                if (schedule.DayOfWeek != DADayOfWeek.None &&
                    schedule.DayOfWeek != currentDayOfWeek)
                    continue;

                // Check if current time is within this schedule's time block
                if (IsInTimeBlock(targetTime, schedule.Offset, schedule.Duration))
                {
                    return schedule;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the current time falls within the specified time block
        /// </summary>
        protected bool IsInTimeBlock(DateTime currentTime, int offsetMinutes, int durationMinutes)
        {
            if (durationMinutes <= 0)
                return false;

            // Get today's anchor in target timezone
            DateTime todayAnchor = currentTime.Date.Add(_anchorDateTime.TimeOfDay);

            DateTime blockStart = todayAnchor.AddMinutes(offsetMinutes);
            DateTime blockEnd = blockStart.AddMinutes(durationMinutes);

            return currentTime >= blockStart && currentTime < blockEnd;
        }

        /// <summary>
        /// Converts bar time to the configured timezone
        /// </summary>
        protected DateTime ConvertToTimeZone(DateTime barTime)
        {
            switch (_timeZone)
            {
                case TimeWindowTimeZone.None:
                    return barTime; // Assume already in exchange time

                case TimeWindowTimeZone.Eastern:
                    return TimeZoneInfo.ConvertTime(barTime,
                        TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

                case TimeWindowTimeZone.Central:
                    return TimeZoneInfo.ConvertTime(barTime,
                        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));

                default:
                    return barTime;
            }
        }

        /// <summary>
        /// Converts System.DayOfWeek to DADayOfWeek
        /// </summary>
        protected DADayOfWeek ConvertToDayOfWeek(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Sunday: return DADayOfWeek.Sunday;
                case DayOfWeek.Monday: return DADayOfWeek.Monday;
                case DayOfWeek.Tuesday: return DADayOfWeek.Tuesday;
                case DayOfWeek.Wednesday: return DADayOfWeek.Wednesday;
                case DayOfWeek.Thursday: return DADayOfWeek.Thursday;
                case DayOfWeek.Friday: return DADayOfWeek.Friday;
                case DayOfWeek.Saturday: return DADayOfWeek.Saturday;
                default: return DADayOfWeek.None;
            }
        }
    }
}
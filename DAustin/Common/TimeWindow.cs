using NinjaTrader.NinjaScript.Strategies;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TimeWindow
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string TimeZoneId 
        { 
            get { return TimeZoneInfo?.Id; }
            set
            {
                TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
            }
        }
        public TimeZoneInfo TimeZoneInfo { get; set; }
        public string LastConvertToTimeZoneId
        {
            get { return LastConvertToTimeZoneInfo?.Id; }
            set
            {
                LastConvertToTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
            }
        }
        public TimeZoneInfo LastConvertToTimeZoneInfo { get; set; }


        #region Constructors
        public TimeWindow(TimeSpan start, TimeSpan end, String timeZoneId)
        {
            TimeZoneId = timeZoneId;
            Start = start;
            End = end;
        }

        public TimeWindow(string start, string end, string timeZoneId)
        {
            TimeZoneId = timeZoneId;
            SetStart(start);
            SetEnd(end);
        }

        public TimeWindow(string start, int durationInMinutes, string timeZoneId)
        {
            TimeZoneId = timeZoneId;
            SetStart(start);
            SetEndFromDurationMinutes(durationInMinutes);
        }

        public TimeWindow()
        {

        }
        #endregion

        #region PublicMethods
        public void SetStart(string start)
        {
            Start = DateTime.Parse(start).TimeOfDay;
        }

        public void SetEnd(string end)
        {
            End = DateTime.Parse(end).TimeOfDay;
        }

        public void SetEndFromDurationMinutes(int durationMinutes)
        {
            TimeSpan minutesToAdd = TimeSpan.FromMinutes(durationMinutes);
            End = Start.Add(minutesToAdd);
        }

        public TimeSpan StartTimeIn(string timeZoneId)
        {
            return TimeIn(Start, timeZoneId);
        }

        public TimeSpan EndTimeIn(string timeZoneId)
        {
            return TimeIn(End, timeZoneId);
        }

        /// <summary>
        /// Checks if the specified DateTime falls within this TimeWindow.
        /// The DateTime is converted to the TimeWindow's timezone for comparison.
        /// </summary>
        public bool IsInRange(DateTime time)
        {
            if (TimeZoneInfo == null)
            {
                return false;
            }

            // Convert the input time to the window's timezone
            DateTime timeInWindowTz;
            if (time.Kind == DateTimeKind.Utc)
            {
                timeInWindowTz = TimeZoneInfo.ConvertTimeFromUtc(time, TimeZoneInfo);
            }
            else
            {
                // Treat as unspecified and convert from UTC (assuming the input is already in a known timezone)
                timeInWindowTz = TimeZoneInfo.ConvertTime(time, TimeZoneInfo);
            }

            TimeSpan timeOfDay = timeInWindowTz.TimeOfDay;

            // Handle case where End < Start (window spans midnight)
            if (End < Start)
            {
                return timeOfDay >= Start || timeOfDay < End;
            }
            else
            {
                return timeOfDay >= Start && timeOfDay < End;
            }
        }
        #endregion

        #region PrivateMethods
        private TimeSpan TimeIn(TimeSpan time, string timeZoneId)
        {
            LastConvertToTimeZoneId = timeZoneId;

            if (LastConvertToTimeZoneInfo == null)
            {
                logger.Error("LastConvertToTimeZoneInfo null. Unable to find TimeZoneInfo for TimeZoneID:" + timeZoneId);
                return TimeSpan.Zero;
            }

            // specify the time
            DateTime srcDT = DateTime.Now.Date.Add(time);
            srcDT = DateTime.SpecifyKind(srcDT, DateTimeKind.Unspecified);
            // convert from source timezone to utc
            DateTime srcDTUTC = TimeZoneInfo.ConvertTimeToUtc(srcDT, TimeZoneInfo);
            // now from utc to the destination tz
            DateTime destDT = TimeZoneInfo.ConvertTimeFromUtc(srcDTUTC, LastConvertToTimeZoneInfo);

            // now return the converted time
            return destDT.TimeOfDay;
        }
        #endregion
    }
}

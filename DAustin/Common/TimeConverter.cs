using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.SuperDomColumns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TimeConverter
    {
        #region Properties
        TimeZoneInfo DataTimeZone { get; } = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;

        public string TimeZoneId
        { 
            get { return TimeZoneInfo?.Id; }
            set
            {
                TimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
            }
        }
        public TimeZoneInfo TimeZoneInfo { get; set; }

        public string LastConvertFromTimeZoneId
        {
            get { return LastConvertFromTimeZoneInfo?.Id; }
            set
            {
                if (LastConvertFromTimeZoneInfo == null || LastConvertFromTimeZoneInfo.Id != value)
                {
                    LastConvertFromTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(value);
                }
            }
        }
        public TimeZoneInfo LastConvertFromTimeZoneInfo { get; set; }

        #endregion
        public TimeConverter()
        {         
        }

        public TimeSpan ToDataTimeOfDay(
            string time,
            string timeZoneID)
        {
            TimeSpan ts = DateTime.Parse(time).TimeOfDay;

            return ToDataTimeOfDay(ts, timeZoneID);
        }

        public TimeSpan ToDataTimeOfDay(
            TimeSpan srcTime, 
            string srcTTimeZoneId)
        {
            TimeZoneId = srcTTimeZoneId;
            TimeZoneInfo destTZInfo = DataTimeZone;
            LastConvertFromTimeZoneId = srcTTimeZoneId;

            // specify the time
            DateTime srcDT = DateTime.Now.Date.Add(srcTime);
            srcDT = DateTime.SpecifyKind(srcDT, DateTimeKind.Unspecified);
            // convert from source timezone to utc
            DateTime srcDTUTC = TimeZoneInfo.ConvertTimeToUtc(srcDT, TimeZoneInfo);
            // now from utc to the destination tz
            DateTime destDT = TimeZoneInfo.ConvertTimeFromUtc(srcDTUTC, destTZInfo);

            // now return the converted time
            return destDT.TimeOfDay;
        }

        public TimeSpan FromDataTimeOfDay(
            TimeSpan srcTime,
            string destTimeZoneId)
        {
            TimeZoneInfo destTZInfo = TimeZoneInfo.FindSystemTimeZoneById(destTimeZoneId);

            return FromDataTimeOfDay(srcTime, destTZInfo);
        }

        public TimeSpan FromDataTimeOfDay(
            TimeSpan srcTime,
            TimeZoneInfo destTZInfo)
        {
            DateTime destDT = FromDataTimeOfDayDate(srcTime, destTZInfo);

            // now return the converted time
            return destDT.TimeOfDay;
        }

        public DateTime FromDataTimeOfDayDate(
            TimeSpan srcTime,
            TimeZoneInfo destTZInfo)
        {
            // specify the time
            DateTime srcDT = DateTime.Now.Date.Add(srcTime);
            srcDT = DateTime.SpecifyKind(srcDT, DateTimeKind.Unspecified);
            // convert from source timezone to utc
            DateTime srcDTUTC = TimeZoneInfo.ConvertTimeToUtc(srcDT, DataTimeZone);
            // now from utc to the destination tz
            DateTime destDT = TimeZoneInfo.ConvertTimeFromUtc(srcDTUTC, destTZInfo);

            // now return the converted datetime
            return destDT;
        }
    }
}

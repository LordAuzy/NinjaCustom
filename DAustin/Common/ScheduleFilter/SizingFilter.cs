using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.ScheduleFilter
{
    public class SizingFilter : ScheduleFilterBase<OptimizationParametersBase.ScheduleSizingFilterParameters>
    {
        public SizingFilter(
            TimeWindowTimeZone timeZone,
            string anchorTime,
            List<OptimizationParametersBase.ScheduleSizingFilterParameters> sizingSchedules)
            : base(timeZone, anchorTime, sizingSchedules)
        {
        }

        /// <summary>
        /// Gets the sizing multiplier for the current bar time
        /// </summary>
        /// <param name="currentBarTime">The current bar timestamp</param>
        /// <returns>Multiplier if in a defined time block, otherwise 1.0 (no adjustment)</returns>
        public double GetCurrentSizingMultiplier(System.DateTime currentBarTime)
        {
            double multiplier = 1;

            var activeSchedule = GetActiveSchedule(currentBarTime);
            if (activeSchedule != null)
            {
                if (activeSchedule.Multiplier > 0)
                {
                    multiplier = activeSchedule.Multiplier;
                }
            }
            return multiplier;
        }
    }
}
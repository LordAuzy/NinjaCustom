using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.ScheduleFilter
{
    public class BiasFilter : ScheduleFilterBase<OptimizationParametersBase.ScheduleBiasFilterParameters>
    {
        public BiasFilter(
            TimeWindowTimeZone timeZone,
            string anchorTime,
            List<OptimizationParametersBase.ScheduleBiasFilterParameters> biasSchedules)
            : base(timeZone, anchorTime, biasSchedules)
        {
        }

        /// <summary>
        /// Gets the trading stance for the current bar time.
        /// If multiple schedules match, returns the most restrictive stance.
        /// </summary>
        /// <param name="currentBarTime">The current bar timestamp</param>
        /// <returns>Most restrictive TradingStance if any schedules match, otherwise TradingStance.None</returns>
        public TradingStance GetCurrentTradingStance(DateTime currentBarTime)
        {
            if (_schedules == null || _schedules.Count == 0)
                return TradingStance.All; // Default to allowing all trades if no filters defined

            // Convert to target timezone
            DateTime targetTime = ConvertToTimeZone(currentBarTime);
            DADayOfWeek currentDayOfWeek = ConvertToDayOfWeek(targetTime.DayOfWeek);
            DAMonth currentMonth = (DAMonth)targetTime.Month;

            // Collect all matching schedules
            var matchingSchedules = new List<OptimizationParametersBase.ScheduleBiasFilterParameters>();

            foreach (var schedule in _schedules)
            {
                // Check month match (if specified)
                if (schedule.Month != DAMonth.None && schedule.Month != currentMonth)
                    continue;

                // Check day of week match (if specified)
                if (schedule.DayOfWeek != DADayOfWeek.None && 
                    schedule.DayOfWeek != currentDayOfWeek)
                    continue;

                // Check if current time is within this schedule's time block
                if (IsInTimeBlock(targetTime, schedule.Offset, schedule.Duration))
                {
                    matchingSchedules.Add(schedule);
                }
            }

            // If no matches, return All (no restrictions)
            if (matchingSchedules.Count == 0)
                return TradingStance.All;

            // Return the most restrictive stance from matching schedules
            return GetMostRestrictiveStance(matchingSchedules.Select(s => s.TradingStance));
        }

        /// <summary>
        /// Determines the most restrictive trading stance from a collection.
        /// Order of restrictiveness (most to least):
        /// 1. None (no trading allowed)
        /// 2. Flat (must exit all positions)
        /// 3. LongOnly or ShortOnly (one direction only)
        /// 4. All (both directions allowed)
        /// </summary>
        private TradingStance GetMostRestrictiveStance(IEnumerable<TradingStance> stances)
        {
            if (!stances.Any())
                return TradingStance.All;

            // Check in order of restrictiveness
            if (stances.Contains(TradingStance.None))
                return TradingStance.None;

            // If both LongOnly and ShortOnly are present, that means no direction is allowed
            bool hasLongOnly = stances.Contains(TradingStance.LongOnly);
            bool hasShortOnly = stances.Contains(TradingStance.ShortOnly);

            if (hasLongOnly && hasShortOnly)
                return TradingStance.None; // Conflicting - no trades allowed

            if (hasLongOnly)
                return TradingStance.LongOnly;

            if (hasShortOnly)
                return TradingStance.ShortOnly;

            return TradingStance.All;
        }
    }
}
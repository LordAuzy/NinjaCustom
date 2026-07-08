using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.DataCollectors
{
    /// <summary>
    /// Data collector for TREND entry conditions evaluator
    /// Created: 2026-05-09 19:47:51
    /// </summary>
    public class ECE_TREND_DataCollector
    {
        // Add your data collection properties here
        // Example: public int LongEntryCount { get; set; } = 0;

        public int InTradeTimeWindowCount { get; set; } = 0;
        public int PassedMinAdxFilterCount { get; set; } = 0;
        public int PassedMaxVWAPDistanceFilterCount { get; set; } = 0;
        public int IsValidPullbackCount { get; set; } = 0;
        public int LongTradeTriggeredCount { get; set; } = 0;
        public int ShortTradeTriggeredCount { get; set; } = 0;
    }
}

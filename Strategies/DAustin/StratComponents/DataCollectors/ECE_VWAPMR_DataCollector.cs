using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.DataCollectors
{
    public class ECE_VWAPMR_DataCollector
    {
        public int CheckForEntryCount { get; set; }
        public int PassedMinATRFilter { get; set; }
        public int PassedMaxVWAPSlopeFilter { get; set; }
        public int PassedDeviationFilter { get; set; }
        public int CurrentPriceBelowVWAPCount { get; set; }
        public int CurrentPriceAboveVWAPCount { get; set; }
        public int LongEntryTriggered { get; set; }
        public int ShortEntryTriggered { get; set; }
    }
}

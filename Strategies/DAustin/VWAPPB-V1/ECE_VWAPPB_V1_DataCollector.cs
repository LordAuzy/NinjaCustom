using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB_V1
{
    public class ECE_VWAPPB_V1_DataCollector
    {
        // EvaluateGeminiNoChop counters
        public int BiasLongCount { get; set; } = 0;
        public int BiasShortCount { get; set; } = 0;
        public int EnvironmentHealthyCountLong { get; set; } = 0;
        public int EnvironmentHealthyCountShort { get; set; } = 0;
        public int EntryLongCount { get; set; } = 0;
        public int EntryShortCount { get; set; } = 0;
        public int LongDepartureCount { get; set; } = 0;
        public int LongChopCount { get; set; } = 0;
        public int ShortDepartureCount { get; set; } = 0;
        public int ShortChopCount { get; set; } = 0;
        public int ShortEntryTooFarFromVWap { get; set; } = 0;
        public int LongEntryTooFarFromVWap { get; set; } = 0;

        // EvaluateChatGPTNoChop counters
        public int AboveVWAPCount { get; set; } = 0;
        public int BelowVWAPCount { get; set; } = 0;
        public int UpTrendCount { get; set; } = 0;
        public int DownTrendCount { get; set; } = 0;
        public int UpTrendChopZoneCount { get; set; } = 0;
        public int DownTrendChopZoneCount { get; set; } = 0;
        public int ValidPullbackLongCount { get; set; } = 0;
        public int ValidPullShortCount { get; set; } = 0;
        public int BullishTriggerCount { get; set; } = 0;
        public int BearishTriggerCount { get; set; } = 0;
        public int LongEntryTriggeredCount { get; set; } = 0; 
        public int ShortEntryTriggeredCount { get; set; } = 0;
    }
}

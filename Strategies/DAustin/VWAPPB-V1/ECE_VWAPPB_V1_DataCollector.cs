using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB_V1
{
    [StrategyComponentId("DC-VWAPPB_V1")]
    public class ECE_VWAPPB_V1_DataCollector : DataCollectorBase
    {
        #region Properties
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
        public int BullishTriggerBullishRegimeCount { get; set; } = 0;
        public int BullishTriggerBearishRegimeCount { get; set; } = 0;
        public int BearishTriggerBearishRegimeCount { get; set; } = 0;
        public int BearishTriggerBullishRegimeCount { get; set; } = 0;
        #endregion

        #region Constructors
        public ECE_VWAPPB_V1_DataCollector(StratBase strat) : base(strat)
        {

        }

        public ECE_VWAPPB_V1_DataCollector() : base()
        {

        }
        #endregion

        #region PublicMethods
        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("==Entry Trigger Counters==");
            sb.AppendFormat("  AboveVWAPCount:{0}", AboveVWAPCount).AppendLine();
            sb.AppendFormat("  UpTrendCount:{0}", UpTrendCount).AppendLine();
            sb.AppendFormat("  UpTrendChopZoneCount:{0}", UpTrendChopZoneCount).AppendLine();
            sb.AppendFormat("  ValidPullbackLongCount:{0}", ValidPullbackLongCount).AppendLine();
            sb.AppendFormat("  BullishTriggerCount:{0}", BullishTriggerCount).AppendLine();
            sb.AppendFormat("  LongEntryTriggeredCount:{0}", LongEntryTriggeredCount).AppendLine();
            sb.AppendFormat("  BelowVWAPCount:{0}", BelowVWAPCount).AppendLine();
            sb.AppendFormat("  DownTrendCount:{0}", DownTrendCount).AppendLine();
            sb.AppendFormat("  DownTrendChopZoneCount:{0}", DownTrendChopZoneCount).AppendLine();
            sb.AppendFormat("  ValidPullShortCount:{0}", ValidPullShortCount).AppendLine();
            sb.AppendFormat("  BearishTriggerCount:{0}", BearishTriggerCount).AppendLine();
            sb.AppendFormat("  ShortEntryTriggeredCount:{0}", ShortEntryTriggeredCount).AppendLine();
            sb.AppendFormat("  BullishTriggerBullishRegimeCount:{0}", BullishTriggerBullishRegimeCount).AppendLine();
            sb.AppendFormat("  BullishTriggerBearishRegimeCount:{0}", BullishTriggerBearishRegimeCount).AppendLine();
            sb.AppendFormat("  BearishTriggerBearishRegimeCount:{0}", BearishTriggerBearishRegimeCount).AppendLine();
            sb.AppendFormat("  BearishTriggerBullishRegimeCount:{0}", BearishTriggerBullishRegimeCount).AppendLine();
        }
        #endregion
    }
}

using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.OPNDRV;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    [StrategyComponentId("DC-OPNDRV")]
    public class DataCollector_OPNDRV : DataCollectorBase
    {
        #region Properties
        public int DriveSetupLongCount { get; set; } = 0;
        public int DriveSetupLongCandidateBars { get; set; } = 0;
        public int LongRetracementValidCount { get; set; } = 0;
        public int LongVWAPValidCount { get; set; } = 0;
        public int LongTrendValidCount { get; set; } = 0;
        public int LongControlledBarCount { get; set; } = 0;
        public int LongEntryDistanceValidCount { get; set; } = 0;
        public int DriveSetupLongTriggeredCount { get; set; } = 0;

        public int DriveSetupShortCount { get; set; } = 0;
        public int DriveSetupShortCandidateBars { get; set; } = 0;
        public int ShortRetracementValidCount { get; set; } = 0;
        public int ShortVWAPValidCount { get; set; } = 0;
        public int ShortTrendValidCount { get; set; } = 0;
        public int ShortControlledBarCount { get; set; } = 0;
        public int ShortEntryDistanceValidCount { get; set; } = 0;
        public int DriveSetupShortTriggeredCount { get; set; } = 0;
        #endregion

        #region constructors
        public DataCollector_OPNDRV(StratBase strat) : base(strat)
        {

        }

        public DataCollector_OPNDRV() : base()
        {

        }
        #endregion

        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("==Entry Trigger Data==");
            sb.AppendLine("  ==Long==");
            sb.AppendLine($"  DriveSetupLongCount: {DriveSetupLongCount}");
            sb.AppendLine($"  DriveSetupLongCandidateBars: {DriveSetupLongCandidateBars}");
            sb.AppendLine($"  DriveSetupLongTriggeredCount: {DriveSetupLongTriggeredCount}");
            sb.AppendLine("  ==Independent candidate tests==");
            sb.AppendLine($"    LongRetracementValidCount: {LongRetracementValidCount}");
            sb.AppendLine($"    LongVWAPValidCount: {LongVWAPValidCount}");
            sb.AppendLine($"    LongTrendValidCount: {LongTrendValidCount}");
            sb.AppendLine($"    LongControlledBarCount: {LongControlledBarCount}");
            sb.AppendLine($"    LongEntryDistanceValidCount: {LongEntryDistanceValidCount}");
            sb.AppendLine($"  LongEntryOrderSubmittedCount: {Orders.LongMarketOrderSubmittedCount + Orders.LongStopMarketOrderSubmittedCount}");
            sb.AppendLine($"    LongMarketOrderSubmittedCount: {Orders.LongMarketOrderSubmittedCount}");
            sb.AppendLine($"    LongStopMarketOrderSubmittedCount: {Orders.LongStopMarketOrderSubmittedCount}");
            sb.AppendLine("  ==Short==");
            sb.AppendLine($"  DriveSetupShortCount: {DriveSetupShortCount}");
            sb.AppendLine($"  DriveSetupShortCandidateBars: {DriveSetupShortCandidateBars}");
            sb.AppendLine($"  DriveSetupShortTriggeredCount: {DriveSetupShortTriggeredCount}");
            sb.AppendLine("  ==Independent candidate tests==");
            sb.AppendLine($"    ShortRetracementValidCount: {ShortRetracementValidCount}");
            sb.AppendLine($"    ShortVWAPValidCount: {ShortVWAPValidCount}");
            sb.AppendLine($"    ShortTrendValidCount: {ShortTrendValidCount}");
            sb.AppendLine($"    ShortControlledBarCount: {ShortControlledBarCount}");
            sb.AppendLine($"    ShortEntryDistanceValidCount: {ShortEntryDistanceValidCount}");
            sb.AppendLine($"  ShortEntryOrderSubmittedCount: {Orders.ShortMarketOrderSubmittedCount + Orders.ShortStopMarketOrderSubmittedCount}");
            sb.AppendLine($"    ShortMarketOrderSubmittedCount: {Orders.ShortMarketOrderSubmittedCount}");
            sb.AppendLine($"    ShortStopMarketOrderSubmittedCount: {Orders.ShortStopMarketOrderSubmittedCount}");

        }
    }
}

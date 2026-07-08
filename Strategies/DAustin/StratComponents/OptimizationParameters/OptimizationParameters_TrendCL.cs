using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters
{
    public class TRENDCL_EntryParameters
    {
        public int FastEMAPeriod { get; set; }
        public int MidEMAPeriod { get; set; }
        public int SlowEMAPeriod { get; set; }
        public int ATRPeriod { get; set; }
        public int ADXPeriod { get; set; }
        public int AdxMinimum { get; set; }
        public double MinAtrPoints { get; set; }
        public EntryOrderType OrderType { get; set; }
        public int OrderExpiryBars { get; set; }
        public double AtrStopMultiplier { get; set; }
        public double AtrTargetMultiplier { get; set; }
    }

    [StrategyComponentId("OP-TRENDCL")]
    public class OptimizationParameters_TRENDCL : OptimizationParametersBase
    {
        #region Properties
        public StopLossTrailingMode SLTrailingMode { get; set; }
        public double EquityRiskPct { get; set; }

        public TimeParameters Time { get; set; } = new TimeParameters();
        public BreakEvenParameters BreakEven { get; set; } = new BreakEvenParameters();
        public TRENDCL_EntryParameters Entry { get; set; } = new TRENDCL_EntryParameters();
        public ChandelierGuardStopParameters ChandelierGuardStop { get; set; } = new ChandelierGuardStopParameters();
        public AdaptiveTrailingStopParameters AdaptiveTrailingStop { get; set; } = new AdaptiveTrailingStopParameters();
        public TrendStructuralTrailingStopParameters TrendStructuralTrailingStop { get; set; } = new TrendStructuralTrailingStopParameters();
        #endregion

        #region constructors
        public OptimizationParameters_TRENDCL(StratBase strat) : base(strat)
        {

        }
        #endregion

        #region overrides
        public override void SetDefaultValues()
        {
            base.SetDefaultValues();

            // General Parameters
            EquityRiskPct = 2.0;
            SLTrailingMode = StopLossTrailingMode.Fixed;

            // Time parameters
            Time.TimeZone = TimeWindowTimeZone.Eastern;
            Time.FlattenTOD = "3:55pm";
            Time.MaxMinutesInTrade = 0;
            Time.TWAnchorTime = "9:30am";
            Time.TWOffset1 = 6;
            Time.TWDuration1 = 124;
            Time.TWOffset2 = 0;
            Time.TWDuration2 = 0;

            // Entry Parameters
            Entry.FastEMAPeriod = 9;
            Entry.MidEMAPeriod = 21;
            Entry.SlowEMAPeriod = 50;
            Entry.ATRPeriod = 14;
            Entry.ADXPeriod = 14;
            Entry.AdxMinimum = 29;
            Entry.MinAtrPoints = 8;
            Entry.OrderType = EntryOrderType.Market;
            Entry.OrderExpiryBars = 0;
            Entry.AtrStopMultiplier = 0.75;
            Entry.AtrTargetMultiplier = 0.0;

            TrendStructuralTrailingStop.EMAPeriod = 19;
            TrendStructuralTrailingStop.ATRPeriod = 14;
            TrendStructuralTrailingStop.ATRMultiplier = 1.75;
            TrendStructuralTrailingStop.ActivationR = 3.6;
        }

        public override void UpdateStratParamValues()
        {
            base.UpdateStratParamValues();

            Strat_TrendCL strat = Strategy as Strat_TrendCL;

            strat.EquityRiskPct = EquityRiskPct;
            strat.SLTrailingMode = SLTrailingMode;

            strat.TI_TimeZone = Time.TimeZone;
            strat.TI_FlattenTOD = Time.FlattenTOD;
            strat.TI_MaxMinutesInTrade = Time.MaxMinutesInTrade;
            strat.TI_TWAnchorTime = Time.TWAnchorTime;
            strat.TI_TWOffset1 = Time.TWOffset1;
            strat.TI_TWDuration1 = Time.TWDuration1;
            strat.TI_TWOffset2 = Time.TWOffset2;
            strat.TI_TWDuration2 = Time.TWDuration2;

            strat.EntryFastEMAPeriod = Entry.FastEMAPeriod;
            strat.EntryMidEMAPeriod = Entry.MidEMAPeriod;
            strat.EntrySlowEMAPeriod = Entry.SlowEMAPeriod;
            strat.EntryATRPeriod = Entry.ATRPeriod;
            strat.EntryADXPeriod = Entry.ADXPeriod;
            strat.EntryAdxMinimum = Entry.AdxMinimum;
            strat.EntryMinAtrPoints = Entry.MinAtrPoints;
            strat.EntryOrderType = Entry.OrderType;
            strat.EntryExpiryBars = Entry.OrderExpiryBars;
            strat.EntryAtrStopMultiplier = Entry.AtrStopMultiplier;
            strat.EntryAtrTargetMultiplier = Entry.AtrTargetMultiplier;

            strat.TST_EMAPeriod = TrendStructuralTrailingStop.EMAPeriod;
            strat.TST_ATRPeriod = TrendStructuralTrailingStop.ATRPeriod;
            strat.TST_ATRMultiplier = TrendStructuralTrailingStop.ATRMultiplier;
            strat.TST_ActivationR = TrendStructuralTrailingStop.ActivationR;
        }

        public override void UpdateFromStrat()
        {
            base.UpdateFromStrat();

            Strat_TrendCL strat = Strategy as Strat_TrendCL;

            EquityRiskPct = strat.EquityRiskPct;
            SLTrailingMode = strat.SLTrailingMode;

            Time.TimeZone = strat.TI_TimeZone;
            Time.FlattenTOD = strat.TI_FlattenTOD;
            Time.MaxMinutesInTrade = strat.TI_MaxMinutesInTrade;
            Time.TWAnchorTime = strat.TI_TWAnchorTime;
            Time.TWOffset1 = strat.TI_TWOffset1;
            Time.TWDuration1 = strat.TI_TWDuration1;
            Time.TWOffset2 = strat.TI_TWOffset2;
            Time.TWDuration2 = strat.TI_TWDuration2;

            Entry.FastEMAPeriod = strat.EntryFastEMAPeriod;
            Entry.MidEMAPeriod = strat.EntryMidEMAPeriod;
            Entry.SlowEMAPeriod = strat.EntrySlowEMAPeriod;
            Entry.ATRPeriod = strat.EntryATRPeriod;
            Entry.ADXPeriod = strat.EntryADXPeriod;
            Entry.AdxMinimum = strat.EntryAdxMinimum;
            Entry.MinAtrPoints = strat.EntryMinAtrPoints;
            Entry.OrderType = strat.EntryOrderType;
            Entry.OrderExpiryBars = strat.EntryExpiryBars;
            Entry.AtrStopMultiplier = strat.EntryAtrStopMultiplier;
            Entry.AtrTargetMultiplier = strat.EntryAtrTargetMultiplier;

            TrendStructuralTrailingStop.EMAPeriod = strat.TST_EMAPeriod;
            TrendStructuralTrailingStop.ATRPeriod = strat.TST_ATRPeriod;
            TrendStructuralTrailingStop.ATRMultiplier = strat.TST_ATRMultiplier;
            TrendStructuralTrailingStop.ActivationR = strat.TST_ActivationR;
        }

        public override TimeParameters GetTimeParameters() { return Time; }
        public override TrendStructuralTrailingStopParameters GetTrendStructuralTrailingStopParameters() { return TrendStructuralTrailingStop; }

        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("======OptimizationParameters=Start=====");
            sb.AppendFormat("Percent of account to risk per trade={0}", EquityRiskPct).AppendLine();
            sb.AppendFormat("StopLoss trailing mode={0}", SLTrailingMode).AppendLine();
            sb.AppendLine("==Time Parameters===");
            sb.AppendFormat("  TimeZone={0}", Time.TimeZone).AppendLine();
            sb.AppendFormat("  FlattenTOD={0}", Time.FlattenTOD).AppendLine();
            sb.AppendFormat("  MaxMinutesInTrade={0}", Time.MaxMinutesInTrade).AppendLine();
            sb.AppendFormat("  TWAnchorTime={0}", Time.TWAnchorTime).AppendLine();
            sb.AppendFormat("  TWOffset1={0}", Time.TWOffset1).AppendLine();
            sb.AppendFormat("  TWDuration1={0}", Time.TWDuration1).AppendLine();
            sb.AppendFormat("  TWOffset2={0}", Time.TWOffset2).AppendLine();
            sb.AppendFormat("  TWDuration2={0}", Time.TWDuration2).AppendLine();
            sb.AppendLine("==Entry Parameters===");
            sb.AppendFormat("  FastEMAPeriod={0}", Entry.FastEMAPeriod).AppendLine();
            sb.AppendFormat("  MidEMAPeriod={0}", Entry.MidEMAPeriod).AppendLine();
            sb.AppendFormat("  SlowEMAPeriod={0}", Entry.SlowEMAPeriod).AppendLine();
            sb.AppendFormat("  ATRPeriod={0}", Entry.ATRPeriod).AppendLine();
            sb.AppendFormat("  ADXPeriod={0}", Entry.ADXPeriod).AppendLine();
            sb.AppendFormat("  AdxMinimum={0}", Entry.AdxMinimum).AppendLine();
            sb.AppendFormat("  MinAtrPoints={0}", Entry.MinAtrPoints).AppendLine();
            sb.AppendFormat("  EntryOrderType={0}", Entry.OrderType).AppendLine();
            sb.AppendFormat("  EntryOrderExpiryBars={0}", Entry.OrderExpiryBars).AppendLine();
            sb.AppendFormat("  AtrStopMultiplier={0}", Entry.AtrStopMultiplier).AppendLine();
            sb.AppendFormat("  AtrTargetMultiplier={0}", Entry.AtrTargetMultiplier).AppendLine();
            sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

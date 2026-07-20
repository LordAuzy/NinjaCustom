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


namespace NinjaTrader.Custom.Strategies.DAustin.VWAPMR
{
    public class VWAPMR_EntryParameters
    {
        // --- Indicators ---
        public int ATRPeriod { get; set; }
        public int VWAPSlopeLookback { get; set; }
        // --- Filters ---
        public double MinATRFilter { get; set; }
        public double MaxVWAPSlopeATR { get; set; }
        // --- Deviation ---
        public double DeviationATR { get; set; }
        // --- Risk ---
        public double StopATR { get; set; }
        // --- Order Behavior ---
        public EntryOrderType OrderType { get; set; }
        public int OrderExpiryBars { get; set; }
    }

    [StrategyComponentId("OP-VWAPMR")]
    public class OptimizationParameters_VWAPMR : OptimizationParametersBase
    {
        #region Properties
        public StopLossTrailingMode SLTrailingMode { get; set; }
        public double EquityRiskPct { get; set; }

        // Test Parameters
        public int FastEMAPeriod { get; set; }
        public int SlowEMAPeriod { get; set; }
        public int TrendEMAPeriod { get; set; }
        public int RSIPeriod { get; set; }
        public int RSIOverBought { get; set; }
        public int RSIOverSold { get; set; }
        public int ADXPeriod { get; set; }
        public int ADXThreshold { get; set; }
        //

        public GeneralParameters General { get; set; } = new GeneralParameters();
        public TimeParameters Time { get; set; } = new TimeParameters();
        public VWAPMR_EntryParameters Entry { get; set; } = new VWAPMR_EntryParameters();
        #endregion

        #region constructors
        public OptimizationParameters_VWAPMR(StratBase strat) : base(strat)
        {

        }
        #endregion

        #region overrides
        public override void SetDefaultValues()
        {
            base.SetDefaultValues();

            // General Parameters
            General.EquityRiskPercent = 2.0;
            General.SLTrailingMode = StopLossTrailingMode.Fixed;
            General.TimeWindowTimeZone = TimeWindowTimeZone.Eastern;
            General.TWAnchorTime = "9:30am";
            General.MaxTradesPerSession = 4;
            General.LoggingMode = LoggingMode.Production;

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
            // --- Indicators ---
            Entry.ATRPeriod = 14;
            Entry.VWAPSlopeLookback = 3;
            // --- Filters ---
            Entry.MinATRFilter = 2.0;
            Entry.MaxVWAPSlopeATR = 0.05;
            // --- Deviation ---
            Entry.DeviationATR = 1.25;
            // --- Risk ---
            Entry.StopATR = 0.75;
            // --- Order Behavior ---
            Entry.OrderType = EntryOrderType.Market;
            Entry.OrderExpiryBars = 3;

            // Test Parameters
            FastEMAPeriod = 14;
            SlowEMAPeriod = 21;
            TrendEMAPeriod = 50;
            RSIPeriod = 14;
            RSIOverSold = 30;
            RSIOverBought = 70;
            ADXPeriod = 14;
            ADXThreshold = 25;
        }

        public override void UpdateStratParamValues()
        {
            base.UpdateStratParamValues();

            Strat_VWAPMR strat = Strategy as Strat_VWAPMR;

            strat.GEN_EquityRiskPct = General.EquityRiskPercent;
            strat.GEN_SLTrailingMode = General.SLTrailingMode;
            strat.GEN_TimeWindowTimeZone = General.TimeWindowTimeZone;
            strat.GEN_TWAnchorTime = General.TWAnchorTime;
            strat.GEN_MaxTradesPerSession = General.MaxTradesPerSession;
            strat.GEN_LoggingMode = General.LoggingMode;

            strat.TI_TimeZone = Time.TimeZone;
            strat.TI_FlattenTOD = Time.FlattenTOD;
            strat.TI_MaxMinutesInTrade = Time.MaxMinutesInTrade;
            strat.TI_TWAnchorTime = Time.TWAnchorTime;
            strat.TI_TWOffset1 = Time.TWOffset1;
            strat.TI_TWDuration1 = Time.TWDuration1;
            strat.TI_TWOffset2 = Time.TWOffset2;
            strat.TI_TWDuration2 = Time.TWDuration2;

            strat.EntryATRPeriod = Entry.ATRPeriod;
            strat.EntryVWAPSlopeLookback = Entry.VWAPSlopeLookback;
            strat.EntryMinATRFilter = Entry.MinATRFilter;
            strat.EntryMaxVWAPSlopeATR = Entry.MaxVWAPSlopeATR;
            strat.EntryDeviationATR = Entry.DeviationATR;
            strat.EntryStopATR = Entry.StopATR;
            strat.EntryOrderType = Entry.OrderType;
            strat.EntryExpiryBars = Entry.OrderExpiryBars;

            //Test
            strat.TestFastEMAPeriod = FastEMAPeriod;
            strat.TestSlowEMAPeriod = SlowEMAPeriod;
            strat.TestTrendEMAPeriod = TrendEMAPeriod;
            strat.TestRSIPeriod = RSIPeriod;
            strat.TestRSIOverSold = RSIOverSold;
            strat.TestRSIOverBought = RSIOverBought;
            strat.TestADXPeriod = ADXPeriod;
            strat.TestADXThreshold = ADXThreshold;
        }

        public override void UpdateFromStrat()
        {
            base.UpdateFromStrat();

            Strat_VWAPMR strat = Strategy as Strat_VWAPMR;

            General.EquityRiskPercent = strat.GEN_EquityRiskPct;
            General.SLTrailingMode = strat.GEN_SLTrailingMode;
            General.TimeWindowTimeZone = strat.GEN_TimeWindowTimeZone;
            General.TWAnchorTime = strat.GEN_TWAnchorTime;
            General.MaxTradesPerSession = strat.GEN_MaxTradesPerSession;
            General.LoggingMode = strat.GEN_LoggingMode;

            Time.TimeZone = strat.TI_TimeZone;
            Time.FlattenTOD = strat.TI_FlattenTOD;
            Time.MaxMinutesInTrade = strat.TI_MaxMinutesInTrade;
            Time.TWAnchorTime = strat.TI_TWAnchorTime;
            Time.TWOffset1 = strat.TI_TWOffset1;
            Time.TWDuration1 = strat.TI_TWDuration1;
            Time.TWOffset2 = strat.TI_TWOffset2;
            Time.TWDuration2 = strat.TI_TWDuration2;

            Entry.ATRPeriod = strat.EntryATRPeriod;
            Entry.VWAPSlopeLookback = strat.EntryVWAPSlopeLookback;
            Entry.MinATRFilter = strat.EntryMinATRFilter;
            Entry.MaxVWAPSlopeATR = strat.EntryMaxVWAPSlopeATR;
            Entry.DeviationATR = strat.EntryDeviationATR;
            Entry.StopATR = strat.EntryStopATR;
            Entry.OrderType = strat.EntryOrderType;
            Entry.OrderExpiryBars = strat.EntryExpiryBars;

            //
            //Test
            FastEMAPeriod = strat.TestFastEMAPeriod;
            SlowEMAPeriod = strat.TestSlowEMAPeriod;
            TrendEMAPeriod = strat.TestTrendEMAPeriod;
            RSIPeriod = strat.TestRSIPeriod;
            RSIOverSold = strat.TestRSIOverSold;
            RSIOverBought = strat.TestRSIOverBought;
            ADXPeriod = strat.TestADXPeriod;
            ADXThreshold = strat.TestADXThreshold;
        }

        public override TimeParameters GetTimeParameters() { return Time; }
        public override GeneralParameters GetGeneralParameters() { return General; }


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
            sb.AppendFormat("  ATRPeriod={0}", Entry.ATRPeriod).AppendLine();
            sb.AppendFormat("  VWAPSlopeLookback={0}", Entry.VWAPSlopeLookback).AppendLine();
            sb.AppendFormat("  MinATRFilter={0}", Entry.MinATRFilter).AppendLine();
            sb.AppendFormat("  MaxVWAPSlopeATR={0}", Entry.MaxVWAPSlopeATR).AppendLine();
            sb.AppendFormat("  DeviationATR={0}", Entry.DeviationATR).AppendLine();
            sb.AppendFormat("  StopATR={0}", Entry.StopATR).AppendLine();
            sb.AppendFormat("  OrderType={0}", Entry.OrderType).AppendLine();
            sb.AppendFormat("  OrderExpiryBars={0}", Entry.OrderExpiryBars).AppendLine();
            sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB_V1
{
    public class VWAPPB_V1_EntryParameters
    {
        // --- Indicators ---
        public int ATRPeriod { get; set; }
        public int FastEMAPeriod { get; set; }
        public int SlowEMAPeriod { get; set; }
        public int VWAPStdDevBandCount { get; set; }
        // --- VWAP Chop Filter ---
        public double MinVWAPDistanceATR { get; set; }
        public double MinVWAPSlopeATR { get; set; }
        public double MinEMASpreadATR { get; set; }
        // --- Pullback ---
        public double MaxPullbackATR { get; set; }
        public int PullbackLookbackBars { get; set; }
        // --- Entry Control ---
        public double MaxEntryDistanceATR { get; set; }
        public int VWAPConfirmationBars { get; set; }
        public double InitialStopATRBuffer { get; set; }
        // --- Order Behavior ---
        public EntryOrderType OrderType { get; set; }
        public int OrderExpiryBars { get; set; }
    }

    [StrategyComponentId("OP-VWAPPB_V1")]
    public class OptimizationParameters_VWAPPB_V1 : OptimizationParametersBase
    {
        #region Properties
        public GeneralParameters General { get; set; } = new GeneralParameters();
        public TimeParameters Time { get; set; } = new TimeParameters();
        public BreakEvenParameters BreakEven { get; set; } = new BreakEvenParameters();
        public VWAPPB_V1_EntryParameters Entry { get; set; } = new VWAPPB_V1_EntryParameters();
        public ChandelierGuardStopParameters ChandelierGuardStop { get; set; } = new ChandelierGuardStopParameters();
        public AdaptiveTrailingStopParameters AdaptiveTrailingStop { get; set; } = new AdaptiveTrailingStopParameters();
        public TrendStructuralTrailingStopParameters TrendStructuralTrailingStop { get; set; } = new TrendStructuralTrailingStopParameters();
        public List<ScheduleBiasFilterParameters> ScheduleBiasFilters { get; set; } = new List<ScheduleBiasFilterParameters>();
        public List<ScheduleSizingFilterParameters> ScheduleSizingFilters { get; set; } = new List<ScheduleSizingFilterParameters>();   
        #endregion

        #region constructors
        public OptimizationParameters_VWAPPB_V1(StratBase strat) : base(strat)
        {
            // in our start parameters we have 3 ScheduleBiasFilters and 3 ScheduleSizingFilters,
            // so we initialize the lists with 3 default entries to make it easier to work with
            // in the UI and optimization
            for (int i = 0; i < 6; i++)
            {
                ScheduleBiasFilters.Add(new ScheduleBiasFilterParameters());
                ScheduleSizingFilters.Add(new ScheduleSizingFilterParameters());
            }   
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

            ScheduleBiasFilters[0].Offset = 30;
            ScheduleBiasFilters[0].Duration = 60;
            ScheduleBiasFilters[0].DayOfWeek = DADayOfWeek.Monday;
            ScheduleBiasFilters[0].TradingStance = TradingStance.None;
            ScheduleBiasFilters[1].Offset = 0;
            ScheduleBiasFilters[1].Duration = 30;
            ScheduleBiasFilters[1].DayOfWeek = DADayOfWeek.Thursday;
            ScheduleBiasFilters[1].TradingStance = TradingStance.LongOnly;
            ScheduleBiasFilters[2].Offset = 110;
            ScheduleBiasFilters[2].Duration = 240;
            ScheduleBiasFilters[2].DayOfWeek = DADayOfWeek.Friday;
            ScheduleBiasFilters[2].TradingStance = TradingStance.LongOnly;
            ScheduleBiasFilters[3].Offset = 0;
            ScheduleBiasFilters[3].Duration = 240;
            ScheduleBiasFilters[3].DayOfWeek = DADayOfWeek.Tuesday;
            ScheduleBiasFilters[3].Month = DAMonth.January;
            ScheduleBiasFilters[3].TradingStance = TradingStance.None;
            ScheduleBiasFilters[4].Offset = 0;
            ScheduleBiasFilters[4].Duration = 240;
            ScheduleBiasFilters[4].DayOfWeek = DADayOfWeek.Wednesday;
            ScheduleBiasFilters[4].Month = DAMonth.January;
            ScheduleBiasFilters[4].TradingStance = TradingStance.None;
            ScheduleBiasFilters[5].Offset = 0;
            ScheduleBiasFilters[5].Duration = 240;
            ScheduleBiasFilters[5].DayOfWeek = DADayOfWeek.Thursday;
            ScheduleBiasFilters[5].Month = DAMonth.January;
            ScheduleBiasFilters[5].TradingStance = TradingStance.None;

            ScheduleSizingFilters[0].Offset = 7;
            ScheduleSizingFilters[0].Duration = 15;
            ScheduleSizingFilters[0].DayOfWeek = DADayOfWeek.Monday;
            ScheduleSizingFilters[0].Multiplier = 2;
            ScheduleSizingFilters[1].Offset = 30;
            ScheduleSizingFilters[1].Duration = 70;
            ScheduleSizingFilters[1].DayOfWeek = DADayOfWeek.Tuesday;
            ScheduleSizingFilters[1].Multiplier = 2;

            // BreakEven Parameters
            BreakEven.R = 1.0;
            BreakEven.UseATR = true;
            BreakEven.ATRPeriod = 14;
            BreakEven.Expanding_R = 0.2;
            BreakEven.Contracting_R = 0.6;

            // Entry Parameters
            // --- Indicators ---
            Entry.VWAPStdDevBandCount = 0;
            Entry.ATRPeriod = 14;
            Entry.FastEMAPeriod = 9;
            Entry.SlowEMAPeriod = 14;
            // --- VWAP Chop Filter ---
            Entry.MinVWAPDistanceATR = 0.3;
            Entry.MinVWAPSlopeATR = 0.02;
            Entry.MinEMASpreadATR = 0.3;
            // --- Pullback ---
            Entry.MaxPullbackATR = 1.5;
            Entry.PullbackLookbackBars = 2;
            // --- Entry Control ---
            Entry.MaxEntryDistanceATR = 0.3;
            Entry.VWAPConfirmationBars = 2;
            Entry.InitialStopATRBuffer = 0.3;
            // --- Order Behavior ---
            Entry.OrderType = EntryOrderType.StopMarket;
            Entry.OrderExpiryBars = 3;

            // GroupName = StopLoss Parameters
            ChandelierGuardStop.ATRPeriod = 14;
            ChandelierGuardStop.InitialATRBuffer = 0.4;
            ChandelierGuardStop.BE_Expanding_R = 0.8;
            ChandelierGuardStop.BE_Contracting_R = 1.1;
            ChandelierGuardStop.ChandelierATRMult = 2.2;
            ChandelierGuardStop.TightATRMult = 1.6;
            ChandelierGuardStop.TightenTriggerR = 2.0;

            AdaptiveTrailingStop.FastEMAPeriod = 9;
            AdaptiveTrailingStop.SlowEMAPeriod = 21;
            AdaptiveTrailingStop.ATRPeriod = 14;
            AdaptiveTrailingStop.ATRSpreadMultiplier = 2.5;

            TrendStructuralTrailingStop.EMAPeriod = 19;
            TrendStructuralTrailingStop.ATRPeriod = 14;
            TrendStructuralTrailingStop.ATRMultiplier = 1.75;
            TrendStructuralTrailingStop.ActivationR = 3.6;
        }

        public override void UpdateStratParamValues()
        {
            base.UpdateStratParamValues();

            Strat_VWAPPB_V1 strat = Strategy as Strat_VWAPPB_V1;

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

            ScheduleBiasFilterParameters SBFp = ScheduleBiasFilters[0];
            strat.SBF_TWOffset1 = SBFp.Offset;
            strat.SBF_TWDuration1 = SBFp.Duration;
            strat.SBF_DOW1 = SBFp.DayOfWeek;
            strat.SBF_TradingStance1 = SBFp.TradingStance;
            SBFp = ScheduleBiasFilters[1];
            strat.SBF_TWOffset2 = SBFp.Offset;
            strat.SBF_TWDuration2 = SBFp.Duration;
            strat.SBF_DOW2 = SBFp.DayOfWeek;
            strat.SBF_TradingStance2 = SBFp.TradingStance;
            SBFp = ScheduleBiasFilters[2];
            strat.SBF_TWOffset3 = SBFp.Offset;
            strat.SBF_TWDuration3 = SBFp.Duration;
            strat.SBF_DOW3 = SBFp.DayOfWeek;
            strat.SBF_TradingStance3 = SBFp.TradingStance;

            ScheduleSizingFilterParameters SSFp = ScheduleSizingFilters[0];
            strat.SSF_TWOffset1 = SSFp.Offset;
            strat.SSF_TWDuration1 = SSFp.Duration;
            strat.SSF_DOW1 = SSFp.DayOfWeek;
            strat.SSF_RiskMultiplier1 = SSFp.Multiplier;
            SSFp = ScheduleSizingFilters[1];
            strat.SSF_TWOffset2 = SSFp.Offset;
            strat.SSF_TWDuration2 = SSFp.Duration;
            strat.SSF_DOW2 = SSFp.DayOfWeek;
            strat.SSF_RiskMultiplier2 = SSFp.Multiplier;
            SSFp = ScheduleSizingFilters[2];
            strat.SSF_TWOffset3 = SSFp.Offset;
            strat.SSF_TWDuration3 = SSFp.Duration;
            strat.SSF_DOW3 = SSFp.DayOfWeek;
            strat.SSF_RiskMultiplier3 = SSFp.Multiplier;

            strat.BE_R = BreakEven.R;
            strat.BE_UseATR = BreakEven.UseATR;
            strat.BE_ATRPeriod = BreakEven.ATRPeriod;
            strat.BE_Expanding_R = BreakEven.Expanding_R;
            strat.BE_Contracting_R = BreakEven.Contracting_R;

            strat.EntryVWAPStdDevBandCount = Entry.VWAPStdDevBandCount;
            strat.EntryATRPeriod = Entry.ATRPeriod;
            strat.EntryFastEMAPeriod = Entry.FastEMAPeriod;
            strat.EntrySlowEMAPeriod = Entry.SlowEMAPeriod;
            strat.EntryMinVWAPDistanceATR = Entry.MinVWAPDistanceATR;
            strat.EntryMinVWAPSlopeATR = Entry.MinVWAPSlopeATR;
            strat.EntryMinEMASpreadATR = Entry.MinEMASpreadATR;
            strat.EntryMaxPullbackATR = Entry.MaxPullbackATR;
            strat.EntryPullbackLookbackBars = Entry.PullbackLookbackBars;
            strat.EntryMaxEntryDistanceATR = Entry.MaxEntryDistanceATR;
            strat.EntryVWAPConfirmationBars = Entry.VWAPConfirmationBars;
            strat.EntryInitialStopATRBuffer = Entry.InitialStopATRBuffer;
            strat.EntryOrderType = Entry.OrderType;
            strat.EntryExpiryBars = Entry.OrderExpiryBars;

            strat.CGS_ATRPeriod = ChandelierGuardStop.ATRPeriod;
            strat.CGS_InitialATRBuffer = ChandelierGuardStop.InitialATRBuffer;
            strat.CGS_BE_Expanding_R = ChandelierGuardStop.BE_Expanding_R;
            strat.CGS_BE_Contracting_R = ChandelierGuardStop.BE_Contracting_R;
            strat.CGS_ChandelierATRMult = ChandelierGuardStop.ChandelierATRMult;
            strat.CGS_TightATRMult = ChandelierGuardStop.TightATRMult;
            strat.CGS_TightenTriggerR = ChandelierGuardStop.TightenTriggerR;

            strat.ATS_FastEMAPeriod = AdaptiveTrailingStop.FastEMAPeriod;
            strat.ATS_SlowEMAPeriod = AdaptiveTrailingStop.SlowEMAPeriod;
            strat.ATS_ATRPeriod = AdaptiveTrailingStop.ATRPeriod;
            strat.ATS_ATRSpreadMultiplier = AdaptiveTrailingStop.ATRSpreadMultiplier;

            strat.TST_EMAPeriod = TrendStructuralTrailingStop.EMAPeriod;
            strat.TST_ATRPeriod = TrendStructuralTrailingStop.ATRPeriod;
            strat.TST_ATRMultiplier = TrendStructuralTrailingStop.ATRMultiplier;
            strat.TST_ActivationR = TrendStructuralTrailingStop.ActivationR;
        }

        public override void UpdateFromStrat()
        {
            base.UpdateFromStrat();

            Strat_VWAPPB_V1 strat = Strategy as Strat_VWAPPB_V1;

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

            ScheduleBiasFilterParameters SBFp = ScheduleBiasFilters[0];
            SBFp.Offset = strat.SBF_TWOffset1;
            SBFp.Duration = strat.SBF_TWDuration1;
            SBFp.DayOfWeek = strat.SBF_DOW1;
            SBFp.TradingStance = strat.SBF_TradingStance1;
            SBFp = ScheduleBiasFilters[1];
            SBFp.Offset = strat.SBF_TWOffset2;
            SBFp.Duration = strat.SBF_TWDuration2;
            SBFp.DayOfWeek = strat.SBF_DOW2;
            SBFp.TradingStance = strat.SBF_TradingStance2;
            SBFp = ScheduleBiasFilters[2];
            SBFp.Offset = strat.SBF_TWOffset3;
            SBFp.Duration = strat.SBF_TWDuration3;
            SBFp.DayOfWeek = strat.SBF_DOW3;
            SBFp.TradingStance = strat.SBF_TradingStance3;

            ScheduleSizingFilterParameters SSFp = ScheduleSizingFilters[0];
            SSFp.Offset = strat.SSF_TWOffset1;
            SSFp.Duration = strat.SSF_TWDuration1;
            SSFp.DayOfWeek = strat.SSF_DOW1;
            SSFp.Multiplier = strat.SSF_RiskMultiplier1;
            SSFp = ScheduleSizingFilters[1];
            SSFp.Offset = strat.SSF_TWOffset2;
            SSFp.Duration = strat.SSF_TWDuration2;
            SSFp.DayOfWeek = strat.SSF_DOW2;
            SSFp.Multiplier = strat.SSF_RiskMultiplier2;
            SSFp = ScheduleSizingFilters[2];
            SSFp.Offset = strat.SSF_TWOffset3;
            SSFp.Duration = strat.SSF_TWDuration3;
            SSFp.DayOfWeek = strat.SSF_DOW3;
            SSFp.Multiplier = strat.SSF_RiskMultiplier3;

            BreakEven.R = strat.BE_R;
            BreakEven.UseATR = strat.BE_UseATR;
            BreakEven.ATRPeriod = strat.BE_ATRPeriod;
            BreakEven.Expanding_R = strat.BE_Expanding_R;
            BreakEven.Contracting_R = strat.BE_Contracting_R;

            Entry.VWAPStdDevBandCount = strat.EntryVWAPStdDevBandCount;
            Entry.ATRPeriod = strat.EntryATRPeriod;
            Entry.FastEMAPeriod = strat.EntryFastEMAPeriod;
            Entry.SlowEMAPeriod = strat.EntrySlowEMAPeriod;
            Entry.MinVWAPDistanceATR = strat.EntryMinVWAPDistanceATR;
            Entry.MinVWAPSlopeATR = strat.EntryMinVWAPSlopeATR;
            Entry.MinEMASpreadATR = strat.EntryMinEMASpreadATR;
            Entry.MaxPullbackATR = strat.EntryMaxPullbackATR;
            Entry.PullbackLookbackBars = strat.EntryPullbackLookbackBars;
            Entry.MaxEntryDistanceATR = strat.EntryMaxEntryDistanceATR;
            Entry.VWAPConfirmationBars = strat.EntryVWAPConfirmationBars;
            Entry.InitialStopATRBuffer = strat.EntryInitialStopATRBuffer;
            Entry.OrderType = strat.EntryOrderType;
            Entry.OrderExpiryBars = strat.EntryExpiryBars;

            ChandelierGuardStop.ATRPeriod = strat.CGS_ATRPeriod;
            ChandelierGuardStop.InitialATRBuffer = strat.CGS_InitialATRBuffer;
            ChandelierGuardStop.BE_Expanding_R = strat.CGS_BE_Expanding_R;
            ChandelierGuardStop.BE_Contracting_R = strat.CGS_BE_Contracting_R;
            ChandelierGuardStop.ChandelierATRMult = strat.CGS_ChandelierATRMult;
            ChandelierGuardStop.TightATRMult = strat.CGS_TightATRMult;
            ChandelierGuardStop.TightenTriggerR = strat.CGS_TightenTriggerR;

            AdaptiveTrailingStop.FastEMAPeriod = strat.ATS_FastEMAPeriod;
            AdaptiveTrailingStop.SlowEMAPeriod = strat.ATS_SlowEMAPeriod;
            AdaptiveTrailingStop.ATRPeriod = strat.ATS_ATRPeriod;
            AdaptiveTrailingStop.ATRSpreadMultiplier = strat.ATS_ATRSpreadMultiplier;

            TrendStructuralTrailingStop.EMAPeriod = strat.TST_EMAPeriod;
            TrendStructuralTrailingStop.ATRPeriod = strat.TST_ATRPeriod;
            TrendStructuralTrailingStop.ATRMultiplier = strat.TST_ATRMultiplier;
            TrendStructuralTrailingStop.ActivationR = strat.TST_ActivationR;
        }

        public override ChandelierGuardStopParameters GetChandelierGuardStopParameters() { return ChandelierGuardStop; }
        public override TimeParameters GetTimeParameters() { return Time; }
        public override BreakEvenParameters GetBreakEvenParameters() { return BreakEven; }
        public override AdaptiveTrailingStopParameters GetAdaptiveTrailingStopParameters() { return AdaptiveTrailingStop; }
        public override TrendStructuralTrailingStopParameters GetTrendStructuralTrailingStopParameters() { return TrendStructuralTrailingStop; }
        public override GeneralParameters GetGeneralParameters() { return General; }

        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("======OptimizationParameters=Start=====");
            sb.AppendFormat("Percent of account to risk per trade={0}", General.EquityRiskPercent).AppendLine();
            sb.AppendFormat("StopLoss trailing mode={0}", General.SLTrailingMode).AppendLine();
            sb.AppendFormat("Default Time Zone={0}", General.TimeWindowTimeZone).AppendLine();
            sb.AppendFormat("Default Anchor Time={0}", General.TWAnchorTime).AppendLine();
            sb.AppendLine("==Time Parameters===");
            sb.AppendFormat("  TimeZone={0}", Time.TimeZone).AppendLine();
            sb.AppendFormat("  FlattenTOD={0}", Time.FlattenTOD).AppendLine();
            sb.AppendFormat("  MaxMinutesInTrade={0}", Time.MaxMinutesInTrade).AppendLine();
            sb.AppendFormat("  TWAnchorTime={0}", Time.TWAnchorTime).AppendLine();
            sb.AppendFormat("  TWOffset1={0}", Time.TWOffset1).AppendLine();
            sb.AppendFormat("  TWDuration1={0}", Time.TWDuration1).AppendLine();
            sb.AppendFormat("  TWOffset2={0}", Time.TWOffset2).AppendLine();
            sb.AppendFormat("  TWDuration2={0}", Time.TWDuration2).AppendLine();
            if (ScheduleBiasFilters[0].Duration > 0)
            {
                sb.AppendLine("==Schedule Bias Filter 1===");
                sb.AppendFormat("  Offset={0}", ScheduleBiasFilters[0].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleBiasFilters[0].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleBiasFilters[0].DayOfWeek).AppendLine();
                sb.AppendFormat("  TradingStance={0}", ScheduleBiasFilters[0].TradingStance).AppendLine();
            }
            if (ScheduleBiasFilters[1].Duration > 0)
            {
                sb.AppendLine("==Schedule Bias Filter 2===");
                sb.AppendFormat("  Offset={0}", ScheduleBiasFilters[1].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleBiasFilters[1].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleBiasFilters[1].DayOfWeek).AppendLine();
                sb.AppendFormat("  TradingStance={0}", ScheduleBiasFilters[1].TradingStance).AppendLine();
            }
            if (ScheduleBiasFilters[2].Duration > 0)
            {
                sb.AppendLine("==Schedule Bias Filter 3===");
                sb.AppendFormat("  Offset={0}", ScheduleBiasFilters[2].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleBiasFilters[2].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleBiasFilters[2].DayOfWeek).AppendLine();
                sb.AppendFormat("  TradingStance={0}", ScheduleBiasFilters[2].TradingStance).AppendLine();
            }
            if (ScheduleSizingFilters[0].Duration > 0)
            {
                sb.AppendLine("==Schedule Sizing Filter 1===");
                sb.AppendFormat("  Offset={0}", ScheduleSizingFilters[0].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleSizingFilters[0].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleSizingFilters[0].DayOfWeek).AppendLine();
                sb.AppendFormat("  Multiplier={0}", ScheduleSizingFilters[0].Multiplier).AppendLine();
            }
            if (ScheduleSizingFilters[1].Duration > 0)
            {
                sb.AppendLine("==Schedule Sizing Filter 2===");
                sb.AppendFormat("  Offset={0}", ScheduleSizingFilters[1].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleSizingFilters[1].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleSizingFilters[1].DayOfWeek).AppendLine();
                sb.AppendFormat("  Multiplier={0}", ScheduleSizingFilters[1].Multiplier).AppendLine();
            }
            if (ScheduleSizingFilters[2].Duration > 0)
            {
                sb.AppendLine("==Schedule Sizing Filter 3===");
                sb.AppendFormat("  Offset={0}", ScheduleSizingFilters[2].Offset).AppendLine();
                sb.AppendFormat("  Duration={0}", ScheduleSizingFilters[2].Duration).AppendLine();
                sb.AppendFormat("  DayOfWeek={0}", ScheduleSizingFilters[2].DayOfWeek).AppendLine();
                sb.AppendFormat("  Multiplier={0}", ScheduleSizingFilters[2].Multiplier).AppendLine();
            }
            sb.AppendLine("==BreakEven Parameters===");
            sb.AppendFormat("  R={0}", BreakEven.R).AppendLine();
            sb.AppendFormat("  UseATR={0}", BreakEven.UseATR).AppendLine();
            sb.AppendFormat("  Expanding_R={0}", BreakEven.Expanding_R).AppendLine();
            sb.AppendFormat("  Contracting_R={0}", BreakEven.Contracting_R).AppendLine();
            sb.AppendLine("==Entry Parameters===");
            sb.AppendLine("  --- Indicators ---");
            sb.AppendFormat("  ATRPeriod={0}", Entry.ATRPeriod).AppendLine();
            sb.AppendFormat("  FastEMAPeriod={0}", Entry.FastEMAPeriod).AppendLine();
            sb.AppendFormat("  SlowEMAPeriod={0}", Entry.SlowEMAPeriod).AppendLine();
            sb.AppendLine("  --- VWAP Chop Filter ---");
            sb.AppendFormat("  MinVWAPDistanceATR={0}", Entry.MinVWAPDistanceATR).AppendLine();
            sb.AppendFormat("  MinVWAPSlopeATR={0}", Entry.MinVWAPSlopeATR).AppendLine();
            sb.AppendFormat("  MinEMASpreadATR={0}", Entry.MinEMASpreadATR).AppendLine();
            sb.AppendLine("  --- Pullback ---");
            sb.AppendFormat("  MaxPullbackATR={0}", Entry.MaxPullbackATR).AppendLine();
            sb.AppendFormat("  PullbackLookbackBars={0}", Entry.PullbackLookbackBars).AppendLine();
            sb.AppendLine("  --- Entry Control ---");
            sb.AppendFormat("  MaxEntryDistanceATR={0}", Entry.MaxEntryDistanceATR).AppendLine();
            sb.AppendFormat("  VWAPConfirmationBars={0}", Entry.VWAPConfirmationBars).AppendLine();
            sb.AppendFormat("  InitialStopATRBuffer={0}", Entry.InitialStopATRBuffer).AppendLine();
            sb.AppendLine("  --- Order Behavior ---");
            sb.AppendFormat("  OrderType={0}", Entry.OrderType).AppendLine();
            sb.AppendFormat("  OrderExpiryBars={0}", Entry.OrderExpiryBars).AppendLine();
            sb.AppendLine("==ChandelierGuardStop Parameters===");
            sb.AppendFormat("  ATRPeriod={0}", ChandelierGuardStop.ATRPeriod).AppendLine();
            sb.AppendFormat("  InitialATRBuffer={0}", ChandelierGuardStop.InitialATRBuffer).AppendLine();
            sb.AppendFormat("  BE_Expanding_R={0}", ChandelierGuardStop.BE_Expanding_R).AppendLine();
            sb.AppendFormat("  BE_Contracting_R={0}", ChandelierGuardStop.BE_Contracting_R).AppendLine();
            sb.AppendFormat("  ChandelierATRMult={0}", ChandelierGuardStop.ChandelierATRMult).AppendLine();
            sb.AppendFormat("  TightATRMult={0}", ChandelierGuardStop.TightATRMult).AppendLine();
            sb.AppendFormat("  TightenTriggerR={0}", ChandelierGuardStop.TightenTriggerR).AppendLine();
            sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

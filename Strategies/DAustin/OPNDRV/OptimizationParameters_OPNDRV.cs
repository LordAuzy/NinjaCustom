using NinjaTrader.Core;
using NinjaTrader.Custom.DAustin.Common;
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

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class OPNDRV_EntryParameters
    {
        // -----------------------------
        // Opening drive definition
        // -----------------------------
        public int DriveOffset { get; set; } = 0;
        public int DriveDuration { get; set; } = 15;
        public int ATRPeriod { get; set; } = 14;

        // Minimum total opening drive size
        public double MinNetMoveATR { get; set; } = 1.00;
        // Maximum opening drive size.
        // Avoid entering after an already absurdly extended move.
        public double MaxNetMoveATR { get; set; } = 0.00;
        // Minimum directional efficiency:
        //
        // abs(Close - Open) / (High - Low)
        //
        // 1.0 = highly directional
        public double MinDriveEfficiency { get; set; } = 0.60;
        // 0.0 = opened and closed at approximately the same price
        // How close the drive close must be to the drive extreme.
        // Example .25 means close must be within top/bottom 25% of drive.
        public double MaxCloseFromExtremePct { get; set; } = 0.25;
        // Minimum displacement from VWAP at end of drive
        public double MinVWAPDistanceATR { get; set; } = 0.50;

        // -----------------------------
        // Trend confirmation
        // -----------------------------
        public int FastEMAPeriod { get; set; } = 9;
        public int SlowEMAPeriod { get; set; } = 21;
        public int VWAPSlopeLookback { get; set; } = 5;
        public double MinVWAPSlopeATR { get; set; } = 0.03;
        public double MinEMASpreadATR { get; set; } = 0.10;

        // -----------------------------
        // Pullback / consolidation
        // -----------------------------
        public int PullbackMinBars { get; set; } = 2;
        public int PullbackMaxBars { get; set; } = 8;
        // Maximum retracement of opening drive.
        // Example .50 = cannot retrace more than 50% of drive.
        public double MaxRetracementPct { get; set; } = 0.50;
        // Minimum retracement.
        // Prevents chasing when no meaningful pause occurred.
        public double MinRetracementPct { get; set; } = 0.10;
        // Pullback cannot penetrate VWAP beyond this amount
        public double MaxVWAPPenetrationATR { get; set; } = 0.10;
        // Require range contraction relative to ATR
        public double MaxPullbackBarRangeATR { get; set; } = 1.00;

        // -----------------------------
        // Entry
        // -----------------------------
        public EntryOrderType OrderType { get; set; } = EntryOrderType.StopMarket;
        public int OrderExpiryBars { get; set; } = 3;
        // Stop buffer beyond pullback structure
        public double InitialStopATRBuffer { get; set; } = 0.10;
        // Reject entry if too far from VWAP
        public double MaxEntryDistanceATR { get; set; } = 2.50;
    }

    [StrategyComponentId("OP-OPNDRV")]
    public class OptimizationParameters_OPNDRV : OptimizationParametersBase
    {
        #region Properties
        public GeneralParameters General { get; set; } = new GeneralParameters();
        public TimeParameters Time { get; set; } = new TimeParameters();
        public BreakEvenParameters BreakEven { get; set; } = new BreakEvenParameters();
        public OPNDRV_EntryParameters Entry { get; set; } = new OPNDRV_EntryParameters();
        public ChandelierGuardStopParameters ChandelierGuardStop { get; set; } = new ChandelierGuardStopParameters();
        public AdaptiveTrailingStopParameters AdaptiveTrailingStop { get; set; } = new AdaptiveTrailingStopParameters();
        public TrendStructuralTrailingStopParameters TrendStructuralTrailingStop { get; set; } = new TrendStructuralTrailingStopParameters();
        public List<ScheduleBiasFilterParameters> ScheduleBiasFilters { get; set; } = new List<ScheduleBiasFilterParameters>();
        public List<ScheduleSizingFilterParameters> ScheduleSizingFilters { get; set; } = new List<ScheduleSizingFilterParameters>();
        #endregion

        #region constructors
        public OptimizationParameters_OPNDRV() : base()
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

        public OptimizationParameters_OPNDRV(StratBase strat) : base(strat)
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
            General.MaxTradesPerSession = 2;
            General.LoggingMode = LoggingMode.Off;

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
            ScheduleSizingFilters[2].Offset = 30;
            ScheduleSizingFilters[2].Duration = 30;
            ScheduleSizingFilters[2].DayOfWeek = DADayOfWeek.Wednesday;
            ScheduleSizingFilters[2].Multiplier = 2;

            // BreakEven Parameters
            BreakEven.R = 1.0;
            BreakEven.UseATR = true;
            BreakEven.ATRPeriod = 14;
            BreakEven.Expanding_R = 0.2;
            BreakEven.Contracting_R = 0.6;

            // Entry Parameters
            //-- Opening drive definition --
            Entry.DriveOffset = 0;
            Entry.DriveDuration = 15;
            Entry.ATRPeriod = 14;
            Entry.MinNetMoveATR = 1.00;
            Entry.MaxNetMoveATR = 3.00;
            Entry.MinDriveEfficiency = 0.60;
            Entry.MaxCloseFromExtremePct = 0.25;
            Entry.MinVWAPDistanceATR = 0.50;
            //-- Trend confirmation --
            Entry.FastEMAPeriod = 9;
            Entry.SlowEMAPeriod = 21;
            Entry.VWAPSlopeLookback = 5;
            Entry.MinVWAPSlopeATR = 0.03;
            Entry.MinEMASpreadATR = 0.10;
            //-- Pullback / consolidation --
            Entry.PullbackMinBars = 2;
            Entry.PullbackMaxBars = 8;
            Entry.MaxRetracementPct = 0.50;
            Entry.MinRetracementPct = 0.10;
            Entry.MaxVWAPPenetrationATR = 0.10;
            Entry.MaxPullbackBarRangeATR = 1.00;
            Entry.OrderType = EntryOrderType.StopMarket;
            Entry.OrderExpiryBars = 3;
            Entry.InitialStopATRBuffer = 0.10;
            Entry.MaxEntryDistanceATR = 2.50;

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

            Strat_OPNDRV strat = Strategy as Strat_OPNDRV;

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

            strat.EntryDriveOffset = Entry.DriveOffset;
            strat.EntryDriveDuration = Entry.DriveDuration;
            strat.EntryATRPeriod = Entry.ATRPeriod;
            strat.EntryMinNetMoveATR = Entry.MinNetMoveATR;
            strat.EntryMaxNetMoveATR = Entry.MaxNetMoveATR;
            strat.EntryMinDriveEfficiency = Entry.MinDriveEfficiency;
            strat.EntryMaxCloseFromExtremePct = Entry.MaxCloseFromExtremePct;
            strat.EntryMinVWAPDistanceATR = Entry.MinVWAPDistanceATR;
            strat.EntryFastEMAPeriod = Entry.FastEMAPeriod;
            strat.EntrySlowEMAPeriod = Entry.SlowEMAPeriod;
            strat.EntryVWAPSlopeLookback = Entry.VWAPSlopeLookback;
            strat.EntryMinVWAPSlopeATR = Entry.MinVWAPSlopeATR;
            strat.EntryMinEMASpreadATR = Entry.MinEMASpreadATR;
            strat.EntryPullbackMinBars = Entry.PullbackMinBars;
            strat.EntryPullbackMaxBars = Entry.PullbackMaxBars;
            strat.EntryMaxRetracementPct = Entry.MaxRetracementPct;
            strat.EntryMinRetracementPct = Entry.MinRetracementPct;
            strat.EntryMaxVWAPPenetrationATR = Entry.MaxVWAPPenetrationATR;
            strat.EntryMaxPullbackBarRangeATR = Entry.MaxPullbackBarRangeATR;
            strat.EntryOrderType = Entry.OrderType;
            strat.EntryOrderExpiryBars = Entry.OrderExpiryBars;
            strat.EntryInitialStopATRBuffer = Entry.InitialStopATRBuffer;
            strat.EntryMaxEntryDistanceATR = Entry.MaxEntryDistanceATR;

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

            Strat_OPNDRV strat = Strategy as Strat_OPNDRV;

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

            Entry.DriveOffset = strat.EntryDriveOffset;
            Entry.DriveDuration = strat.EntryDriveDuration;
            Entry.ATRPeriod = strat.EntryATRPeriod;
            Entry.MinNetMoveATR = strat.EntryMinNetMoveATR;
            Entry.MaxNetMoveATR = strat.EntryMaxNetMoveATR;
            Entry.MinDriveEfficiency = strat.EntryMinDriveEfficiency;
            Entry.MaxCloseFromExtremePct = strat.EntryMaxCloseFromExtremePct;
            Entry.MinVWAPDistanceATR = strat.EntryMinVWAPDistanceATR;
            Entry.FastEMAPeriod = strat.EntryFastEMAPeriod;
            Entry.SlowEMAPeriod = strat.EntrySlowEMAPeriod;
            Entry.VWAPSlopeLookback = strat.EntryVWAPSlopeLookback;
            Entry.MinVWAPSlopeATR = strat.EntryMinVWAPSlopeATR;
            Entry.MinEMASpreadATR = strat.EntryMinEMASpreadATR;
            Entry.PullbackMinBars = strat.EntryPullbackMinBars;
            Entry.PullbackMaxBars = strat.EntryPullbackMaxBars;
            Entry.MaxRetracementPct = strat.EntryMaxRetracementPct;
            Entry.MinRetracementPct = strat.EntryMinRetracementPct;
            Entry.MaxVWAPPenetrationATR = strat.EntryMaxVWAPPenetrationATR;
            Entry.MaxPullbackBarRangeATR = strat.EntryMaxPullbackBarRangeATR;
            Entry.OrderType = strat.EntryOrderType;
            Entry.OrderExpiryBars = strat.EntryOrderExpiryBars;
            Entry.InitialStopATRBuffer = strat.EntryInitialStopATRBuffer;
            Entry.MaxEntryDistanceATR = strat.EntryMaxEntryDistanceATR;

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

        public override void CopyFrom(OptimizationParametersBase opf)
        {
            base.CopyFrom(opf);
            OptimizationParameters_OPNDRV opFrom = opf as OptimizationParameters_OPNDRV;

            General.EquityRiskPercent = opFrom.General.EquityRiskPercent;
            General.SLTrailingMode = opFrom.General.SLTrailingMode;
            General.TimeWindowTimeZone = opFrom.General.TimeWindowTimeZone;
            General.TWAnchorTime = opFrom.General.TWAnchorTime;
            General.MaxTradesPerSession = opFrom.General.MaxTradesPerSession;
            General.LoggingMode = opFrom.General.LoggingMode;

            Time.TimeZone = opFrom.Time.TimeZone;
            Time.FlattenTOD = opFrom.Time.FlattenTOD;
            Time.MaxMinutesInTrade = opFrom.Time.MaxMinutesInTrade;
            Time.TWAnchorTime = opFrom.Time.TWAnchorTime;
            Time.TWOffset1 = opFrom.Time.TWOffset1;
            Time.TWDuration1 = opFrom.Time.TWDuration1;
            Time.TWOffset2 = opFrom.Time.TWOffset2;
            Time.TWDuration2 = opFrom.Time.TWDuration2;

            ScheduleBiasFilterParameters SBFp = ScheduleBiasFilters[0];
            ScheduleBiasFilterParameters SBFpFrom = opFrom.ScheduleBiasFilters[0];
            SBFp.Offset = SBFpFrom.Offset;
            SBFp.Duration = SBFpFrom.Duration;
            SBFp.DayOfWeek = SBFpFrom.DayOfWeek;
            SBFp.TradingStance = SBFpFrom.TradingStance;
            SBFp = ScheduleBiasFilters[1];
            SBFpFrom = opFrom.ScheduleBiasFilters[1];
            SBFp.Offset = SBFpFrom.Offset;
            SBFp.Duration = SBFpFrom.Duration;
            SBFp.DayOfWeek = SBFpFrom.DayOfWeek;
            SBFp.TradingStance = SBFpFrom.TradingStance;
            SBFp = ScheduleBiasFilters[2];
            SBFpFrom = opFrom.ScheduleBiasFilters[2];
            SBFp.Offset = SBFpFrom.Offset;
            SBFp.Duration = SBFpFrom.Duration;
            SBFp.DayOfWeek = SBFpFrom.DayOfWeek;
            SBFp.TradingStance = SBFpFrom.TradingStance;

            ScheduleSizingFilterParameters SSFp = ScheduleSizingFilters[0];
            ScheduleSizingFilterParameters SSFpFrom = opFrom.ScheduleSizingFilters[0];
            SSFp.Offset = SSFpFrom.Offset;
            SSFp.Duration = SSFpFrom.Duration;
            SSFp.DayOfWeek = SSFpFrom.DayOfWeek;
            SSFp.Multiplier = SSFpFrom.Multiplier;
            SSFp = ScheduleSizingFilters[1];
            SSFpFrom = opFrom.ScheduleSizingFilters[1];
            SSFp.Offset = SSFpFrom.Offset;
            SSFp.Duration = SSFpFrom.Duration;
            SSFp.DayOfWeek = SSFpFrom.DayOfWeek;
            SSFp.Multiplier = SSFpFrom.Multiplier;
            SSFp = ScheduleSizingFilters[2];
            SSFpFrom = opFrom.ScheduleSizingFilters[2];
            SSFp.Offset = SSFpFrom.Offset;
            SSFp.Duration = SSFpFrom.Duration;
            SSFp.DayOfWeek = SSFpFrom.DayOfWeek;
            SSFp.Multiplier = SSFpFrom.Multiplier;

            BreakEven.R = opFrom.BreakEven.R;
            BreakEven.UseATR = opFrom.BreakEven.UseATR;
            BreakEven.ATRPeriod = opFrom.BreakEven.ATRPeriod;
            BreakEven.Expanding_R = opFrom.BreakEven.Expanding_R;
            BreakEven.Contracting_R = opFrom.BreakEven.Contracting_R;

            Entry.DriveOffset = opFrom.Entry.DriveOffset;
            Entry.DriveDuration = opFrom.Entry.DriveDuration;
            Entry.ATRPeriod = opFrom.Entry.ATRPeriod;
            Entry.MinNetMoveATR = opFrom.Entry.MinNetMoveATR;
            Entry.MaxNetMoveATR = opFrom.Entry.MaxNetMoveATR;
            Entry.MinDriveEfficiency = opFrom.Entry.MinDriveEfficiency;
            Entry.MaxCloseFromExtremePct = opFrom.Entry.MaxCloseFromExtremePct;
            Entry.MinVWAPDistanceATR = opFrom.Entry.MinVWAPDistanceATR;
            Entry.FastEMAPeriod = opFrom.Entry.FastEMAPeriod;
            Entry.SlowEMAPeriod = opFrom.Entry.SlowEMAPeriod;
            Entry.VWAPSlopeLookback = opFrom.Entry.VWAPSlopeLookback;
            Entry.MinVWAPSlopeATR = opFrom.Entry.MinVWAPSlopeATR;
            Entry.MinEMASpreadATR = opFrom.Entry.MinEMASpreadATR;
            Entry.PullbackMinBars = opFrom.Entry.PullbackMinBars;
            Entry.PullbackMaxBars = opFrom.Entry.PullbackMaxBars;
            Entry.MaxRetracementPct = opFrom.Entry.MaxRetracementPct;
            Entry.MinRetracementPct = opFrom.Entry.MinRetracementPct;
            Entry.MaxVWAPPenetrationATR = opFrom.Entry.MaxVWAPPenetrationATR;
            Entry.MaxPullbackBarRangeATR = opFrom.Entry.MaxPullbackBarRangeATR;
            Entry.OrderType = opFrom.Entry.OrderType;
            Entry.OrderExpiryBars = opFrom.Entry.OrderExpiryBars;
            Entry.InitialStopATRBuffer = opFrom.Entry.InitialStopATRBuffer;
            Entry.MaxEntryDistanceATR = opFrom.Entry.MaxEntryDistanceATR;

            ChandelierGuardStop.ATRPeriod = opFrom.ChandelierGuardStop.ATRPeriod;
            ChandelierGuardStop.InitialATRBuffer = opFrom.ChandelierGuardStop.InitialATRBuffer;
            ChandelierGuardStop.BE_Expanding_R = opFrom.ChandelierGuardStop.BE_Expanding_R;
            ChandelierGuardStop.BE_Contracting_R = opFrom.ChandelierGuardStop.BE_Contracting_R;
            ChandelierGuardStop.ChandelierATRMult = opFrom.ChandelierGuardStop.ChandelierATRMult;
            ChandelierGuardStop.TightATRMult = opFrom.ChandelierGuardStop.TightATRMult;
            ChandelierGuardStop.TightenTriggerR = opFrom.ChandelierGuardStop.TightenTriggerR;

            AdaptiveTrailingStop.FastEMAPeriod = opFrom.AdaptiveTrailingStop.FastEMAPeriod;
            AdaptiveTrailingStop.SlowEMAPeriod = opFrom.AdaptiveTrailingStop.SlowEMAPeriod;
            AdaptiveTrailingStop.ATRPeriod = opFrom.AdaptiveTrailingStop.ATRPeriod;
            AdaptiveTrailingStop.ATRSpreadMultiplier = opFrom.AdaptiveTrailingStop.ATRSpreadMultiplier;

            TrendStructuralTrailingStop.EMAPeriod = opFrom.TrendStructuralTrailingStop.EMAPeriod;
            TrendStructuralTrailingStop.ATRPeriod = opFrom.TrendStructuralTrailingStop.ATRPeriod;
            TrendStructuralTrailingStop.ATRMultiplier = opFrom.TrendStructuralTrailingStop.ATRMultiplier;
            TrendStructuralTrailingStop.ActivationR = opFrom.TrendStructuralTrailingStop.ActivationR;
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
            sb.AppendLine("==Entry Parameters===");
            sb.AppendLine("  --Opening drive definition--");
            sb.AppendFormat("  DriveOffset={0}", Entry.DriveOffset).AppendLine();
            sb.AppendFormat("  DriveDuration", Entry.DriveDuration).AppendLine();
            sb.AppendFormat("  ATRPeriod={0}", Entry.ATRPeriod).AppendLine();
            sb.AppendFormat("  MinNetMoveATR={0}", Entry.MinNetMoveATR).AppendLine();
            sb.AppendFormat("  MaxNetMoveATR={0}", Entry.MaxNetMoveATR).AppendLine();
            sb.AppendFormat("  MinDriveEfficiency={0}", Entry.MinDriveEfficiency).AppendLine();
            sb.AppendFormat("  MaxCloseFromExtremePct={0}", Entry.MaxCloseFromExtremePct).AppendLine();
            sb.AppendFormat("  MinVWAPDistanceATR={0}", Entry.MinVWAPDistanceATR).AppendLine();
            sb.AppendLine("  --Trend confirmation--");
            sb.AppendFormat("  FastEMAPeriod={0}", Entry.FastEMAPeriod).AppendLine();
            sb.AppendFormat("  SlowEMAPeriod={0}", Entry.SlowEMAPeriod).AppendLine();
            sb.AppendFormat("  VWAPSlopeLookback={0}", Entry.VWAPSlopeLookback).AppendLine();
            sb.AppendFormat("  MinVWAPSlopeATR={0}", Entry.MinVWAPSlopeATR).AppendLine();
            sb.AppendFormat("  MinEMASpreadATR={0}", Entry.MinEMASpreadATR).AppendLine();
            sb.AppendLine("  --- Pullback/consolidation ---");
            sb.AppendFormat("  PullbackMinBars={0}", Entry.PullbackMinBars).AppendLine();
            sb.AppendFormat("  PullbackMaxBars={0}", Entry.PullbackMaxBars).AppendLine();
            sb.AppendFormat("  MaxRetracementPct={0}", Entry.MaxRetracementPct).AppendLine();
            sb.AppendFormat("  MinRetracementPct={0}", Entry.MinRetracementPct).AppendLine();
            sb.AppendFormat("  MaxVWAPPenetrationATR={0}", Entry.MaxVWAPPenetrationATR).AppendLine();
            sb.AppendFormat("  MaxPullbackBarRangeATR={0}", Entry.MaxPullbackBarRangeATR).AppendLine();
            sb.AppendLine("  --- Order Behavior ---");
            sb.AppendFormat("  OrderType={0}", Entry.OrderType).AppendLine();
            sb.AppendFormat("  OrderExpiryBars={0}", Entry.OrderExpiryBars).AppendLine();
            sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

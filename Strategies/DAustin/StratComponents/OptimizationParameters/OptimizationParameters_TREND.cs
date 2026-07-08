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
    public class TREND_EntryParameters
    {
        // --- Indicators ---
        public int ATRPeriod { get; set; }
        public int FastEMAPeriod { get; set; }
        public int SlowEMAPeriod { get; set; }
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
        public EntryTriggerType TriggerType { get; set; }
        public EntryOrderType OrderType { get; set; }
        public int OrderExpiryBars { get; set; }
    }

    [StrategyComponentId("OP-TREND")]
    public class OptimizationParameters_TREND : OptimizationParametersBase
    {
        #region Properties
        public StopLossTrailingMode SLTrailingMode { get; set; }
        public double EquityRiskPct { get; set; }

        public TimeParameters Time { get; set; } = new TimeParameters();
        public BreakEvenParameters BreakEven { get; set; } = new BreakEvenParameters();
        public TREND_EntryParameters Entry { get; set; } = new TREND_EntryParameters();
        public ChandelierGuardStopParameters ChandelierGuardStop { get; set; } = new ChandelierGuardStopParameters();
        public AdaptiveTrailingStopParameters AdaptiveTrailingStop { get; set; } = new AdaptiveTrailingStopParameters();
        public TrendStructuralTrailingStopParameters TrendStructuralTrailingStop { get; set; } = new TrendStructuralTrailingStopParameters();

        // ChatGPT params
        public TimeWindowTimeZone GPT_TI_TimeZone { get; set; }
        public string GPT_TI_ORBStartTime { get; set; }
        public int GPT_TI_ORBMinutes { get; set; }
        public int GPT_TI_ORBTradingWindow { get; set; }
        public int GPT_EMAFastPeriod { get; set; }
        public int GPT_EMASlowPeriod { get; set; }
        public int GPT_ATRPeriod { get; set; }
        public int GPT_ADXPeriod { get; set; }
        public double GPT_MinORBreakATR { get; set; }
        public int GPT_PullbackMinBars { get; set; }
        public int GPT_PullbackMaxBars { get; set; }
        public double GPT_PullbackMaxATR { get; set; }
        public double GPT_RiskATR { get; set; }
        public double GPT_RewardR { get; set; }
        public double GPT_BETriggerR { get; set; }
        public double GPT_TrailATR { get; set; }
        public double GPT_MinADX { get; set; }
        public double GPT_MaxVWAPDistanceATR { get; set; }
        #endregion

        #region constructors
        public OptimizationParameters_TREND(StratBase strat) : base(strat)
        {

        }
        #endregion

        #region overrides
        public override void SetDefaultValues()
        {
            base.SetDefaultValues();

            // General Parameters
            EquityRiskPct = 2.0;
            SLTrailingMode = StopLossTrailingMode.TrendStructuralTrailing;

            // Time parameters
            Time.TimeZone = TimeWindowTimeZone.Eastern;
            Time.FlattenTOD = "3:55pm";
            Time.MaxMinutesInTrade = 0;
            Time.TWAnchorTime = "9:30am";
            Time.TWOffset1 = 6;
            Time.TWDuration1 = 124;
            Time.TWOffset2 = 0;
            Time.TWDuration2 = 0;

            // BreakEven Parameters
            BreakEven.R = 1.0;
            BreakEven.UseATR = true;
            BreakEven.ATRPeriod = 14;
            BreakEven.Expanding_R = 0.2;
            BreakEven.Contracting_R = 0.6;

            // Entry parameters
            Entry.TriggerType = EntryTriggerType.ChatGPT;
            Entry.OrderType = EntryOrderType.StopMarket;
            Entry.OrderExpiryBars = 3;

            // Chatgpt params
            GPT_TI_TimeZone = TimeWindowTimeZone.Eastern;
            GPT_TI_ORBStartTime = "9:30am";
            GPT_TI_ORBMinutes = 22;
            GPT_TI_ORBTradingWindow = 230;
            GPT_EMAFastPeriod = 20;
            GPT_EMASlowPeriod = 50;
            GPT_ATRPeriod = 14;
            GPT_ADXPeriod = 14;
            GPT_MinORBreakATR = 1.3;
            GPT_PullbackMinBars = 1;
            GPT_PullbackMaxBars = 4;
            GPT_PullbackMaxATR = 1;
            GPT_RiskATR = 1.2;
            GPT_RewardR = 1.5;
            GPT_BETriggerR = 1.0;
            GPT_TrailATR = 1.5;
            GPT_MinADX = 20;
            GPT_MaxVWAPDistanceATR = 2.5;

            // GroupName = StopLoss Parameters
            ChandelierGuardStop.ATRPeriod = 14;
            ChandelierGuardStop.InitialATRBuffer = 0.4;
            ChandelierGuardStop.BE_Expanding_R = 0.8;
            ChandelierGuardStop.BE_Contracting_R = 1.1;
            ChandelierGuardStop.ChandelierATRMult = .6;
            ChandelierGuardStop.TightATRMult = 1.3;
            ChandelierGuardStop.TightenTriggerR = 1.2;

            AdaptiveTrailingStop.FastEMAPeriod = 9;
            AdaptiveTrailingStop.SlowEMAPeriod = 21;
            AdaptiveTrailingStop.ATRPeriod = 14;
            AdaptiveTrailingStop.ATRSpreadMultiplier = 1.2;

            TrendStructuralTrailingStop.EMAPeriod = 19;
            TrendStructuralTrailingStop.ATRPeriod = 14;
            TrendStructuralTrailingStop.ATRMultiplier = 1.75;
            TrendStructuralTrailingStop.ActivationR = 3.6;
        }

        public override void UpdateStratParamValues()
        {
            base.UpdateStratParamValues();

            Strat_Trend strat = Strategy as Strat_Trend;

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

            strat.BE_R = BreakEven.R;
            strat.BE_UseATR = BreakEven.UseATR;
            strat.BE_ATRPeriod = BreakEven.ATRPeriod;
            strat.BE_Expanding_R = BreakEven.Expanding_R;
            strat.BE_Contracting_R = BreakEven.Contracting_R;

            strat.EntryTriggerType = Entry.TriggerType;
            strat.EntryOrderType = Entry.OrderType;
            strat.EntryExpiryBars = Entry.OrderExpiryBars;

            // Chatgpt params
            strat.TREND_GPT_TI_TimeZone = GPT_TI_TimeZone;
            strat.TREND_GPT_TI_ORBStartTime = GPT_TI_ORBStartTime;
            strat.TREND_GPT_TI_ORBMinutes = GPT_TI_ORBMinutes;
            strat.TREND_GPT_TI_ORBTradingWindow = GPT_TI_ORBTradingWindow;
            strat.TREND_GPT_EMAFastPeriod = GPT_EMAFastPeriod;
            strat.TREND_GPT_EMASlowPeriod = GPT_EMASlowPeriod;
            strat.TREND_GPT_ATRPeriod = GPT_ATRPeriod;
            strat.TREND_GPT_ADXPeriod = GPT_ADXPeriod;
            strat.TREND_GPT_MinORBreakATR = GPT_MinORBreakATR;
            strat.TREND_GPT_PullbackMinBars = GPT_PullbackMinBars;
            strat.TREND_GPT_PullbackMaxBars = GPT_PullbackMaxBars;
            strat.TREND_GPT_PullbackMaxATR = GPT_PullbackMaxATR;
            strat.TREND_GPT_RiskATR = GPT_RiskATR;
            strat.TREND_GPT_RewardR = GPT_RewardR;
            strat.TREND_GPT_BETriggerR = GPT_BETriggerR;
            strat.TREND_GPT_TrailATR = GPT_TrailATR;
            strat.TREND_GPT_MinADX = GPT_MinADX;
            strat.TREND_GPT_MaxVWAPDistanceATR = GPT_MaxVWAPDistanceATR;

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

            Strat_Trend strat = Strategy as Strat_Trend;

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

            BreakEven.R = strat.BE_R;
            BreakEven.UseATR = strat.BE_UseATR;
            BreakEven.ATRPeriod = strat.BE_ATRPeriod;
            BreakEven.Expanding_R = strat.BE_Expanding_R;
            BreakEven.Contracting_R = strat.BE_Contracting_R;

            Entry.TriggerType = strat.EntryTriggerType;
            Entry.OrderType = strat.EntryOrderType;
            Entry.OrderExpiryBars = strat.EntryExpiryBars;

            // Chatgpt params
            GPT_TI_TimeZone = strat.TREND_GPT_TI_TimeZone;
            GPT_TI_ORBStartTime = strat.TREND_GPT_TI_ORBStartTime;
            GPT_TI_ORBMinutes = strat.TREND_GPT_TI_ORBMinutes;
            GPT_TI_ORBTradingWindow = strat.TREND_GPT_TI_ORBTradingWindow;
            GPT_EMAFastPeriod = strat.TREND_GPT_EMAFastPeriod;
            GPT_EMASlowPeriod = strat.TREND_GPT_EMASlowPeriod;
            GPT_ATRPeriod = strat.TREND_GPT_ATRPeriod;
            GPT_ADXPeriod = strat.TREND_GPT_ADXPeriod;
            GPT_MinORBreakATR = strat.TREND_GPT_MinORBreakATR;
            GPT_PullbackMinBars = strat.TREND_GPT_PullbackMinBars;
            GPT_PullbackMaxBars = strat.TREND_GPT_PullbackMaxBars;
            GPT_PullbackMaxATR = strat.TREND_GPT_PullbackMaxATR;
            GPT_RiskATR = strat.TREND_GPT_RiskATR;
            GPT_RewardR = strat.TREND_GPT_RewardR;
            GPT_BETriggerR = strat.TREND_GPT_BETriggerR;
            GPT_TrailATR = strat.TREND_GPT_TrailATR;
            GPT_MinADX = strat.TREND_GPT_MinADX;
            GPT_MaxVWAPDistanceATR = strat.TREND_GPT_MaxVWAPDistanceATR;

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

        public override TimeParameters GetTimeParameters() { return Time; }
        public override BreakEvenParameters GetBreakEvenParameters() { return BreakEven; }
        public override ChandelierGuardStopParameters GetChandelierGuardStopParameters() { return ChandelierGuardStop; }
        public override AdaptiveTrailingStopParameters GetAdaptiveTrailingStopParameters() { return AdaptiveTrailingStop; }
        public override TrendStructuralTrailingStopParameters GetTrendStructuralTrailingStopParameters() { return TrendStructuralTrailingStop; }

        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("======OptimizationParameters=Start=====");
            sb.AppendFormat("Percent of account to risk per trade={0}", EquityRiskPct).AppendLine();
            sb.AppendFormat("StopLoss trailing mode={0}", SLTrailingMode).AppendLine();
            sb.AppendLine("==Parameters===");
            sb.AppendFormat("  TimeZone={0}", GPT_TI_TimeZone).AppendLine();
            sb.AppendFormat("  ORBStartTime={0}", GPT_TI_ORBStartTime).AppendLine();
            sb.AppendFormat("  ORBMinutes={0}", GPT_TI_ORBMinutes).AppendLine();
            sb.AppendFormat("  TradingWindowMinutes={0}", GPT_TI_ORBTradingWindow).AppendLine();
            sb.AppendFormat("  EMAFastPeriod={0}", GPT_EMAFastPeriod).AppendLine();
            sb.AppendFormat("  EMASlowPeriod={0}", GPT_EMASlowPeriod).AppendLine();
            sb.AppendFormat("  ATRPeriod={0}", GPT_ATRPeriod).AppendLine();
            sb.AppendFormat("  MinORBreakATR={0}", GPT_MinORBreakATR).AppendLine();
            sb.AppendFormat("  PullbackMinBars={0}", GPT_PullbackMinBars).AppendLine();
            sb.AppendFormat("  PullbackMaxBars={0}", GPT_PullbackMaxBars).AppendLine();
            sb.AppendFormat("  PullbackMaxATR={0}", GPT_PullbackMaxATR).AppendLine();
            sb.AppendFormat("  MinADX={0}", GPT_MinADX).AppendLine();
            sb.AppendFormat("  MaxVWAPDistanceATR={0}", GPT_MaxVWAPDistanceATR).AppendLine();
            sb.AppendFormat("  ADXPeriod={0}", GPT_ATRPeriod).AppendLine();
            sb.AppendFormat("  ADXPeriod={0}", GPT_ATRPeriod).AppendLine();
            sb.AppendLine("==TrendStructuralTrailingStop===");
            sb.AppendFormat("  EMAPeriod={0}", TrendStructuralTrailingStop.EMAPeriod).AppendLine();
            sb.AppendFormat("  ATRPeriod={0}", TrendStructuralTrailingStop.ATRPeriod).AppendLine();
            sb.AppendFormat("  ATRMultiplier={0}", TrendStructuralTrailingStop.ATRMultiplier).AppendLine();
            sb.AppendFormat("  ActivationR={0}", TrendStructuralTrailingStop.ActivationR).AppendLine();
             sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

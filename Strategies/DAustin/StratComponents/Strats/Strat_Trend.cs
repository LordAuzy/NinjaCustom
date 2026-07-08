#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.Custom.Strategies.DAustin.TradeManagers;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.AccountData;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using NLog;
using NLog.Config;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    #region CategoryValues
    [Gui.CategoryOrder(StratPropertyGroups.GeneralParameters, 1), Gui.CategoryExpanded(StratPropertyGroups.GeneralParameters, true)]
    [Gui.CategoryOrder(StratPropertyGroups.TimeParams, 2), Gui.CategoryExpanded(StratPropertyGroups.TimeParams, false)]
    [Gui.CategoryOrder(StratPropertyGroups.TrendCHATGPT, 3), Gui.CategoryExpanded(StratPropertyGroups.TrendCHATGPT, false)]
    [Gui.CategoryOrder(StratPropertyGroups.BreakEven, 4), Gui.CategoryExpanded(StratPropertyGroups.BreakEven, false)]
    [Gui.CategoryOrder(StratPropertyGroups.ChandelierGuardStop, 5), Gui.CategoryExpanded(StratPropertyGroups.ChandelierGuardStop, false)]
    [Gui.CategoryOrder(StratPropertyGroups.AdaptiveTrailingStop, 6), Gui.CategoryExpanded(StratPropertyGroups.AdaptiveTrailingStop, false)]
    [Gui.CategoryOrder(StratPropertyGroups.TrendStructureTrail, 7), Gui.CategoryExpanded(StratPropertyGroups.TrendStructureTrail, false)]
    [Gui.CategoryOrder(StratPropertyGroups.Entry, 8), Gui.CategoryExpanded(StratPropertyGroups.Entry, false)]
    #endregion
    public class Strat_Trend : StratBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region GeneralParameters[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(.25, 4.0)]
        [Display(Name = "EquityRiskPct", GroupName = StratPropertyGroups.GeneralParameters, Order = 1)]
        public double EquityRiskPct { get; set; }
        [NinjaScriptProperty] 
        [Display(   Name = "SLTrailingMode", 
                    Description = "How to move stop loss after initial placement", 
                    Order = 2, 
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public StopLossTrailingMode SLTrailingMode { get; set; }
        #endregion

        #region TradingTimeWindow[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Display(   Name = "TimeZone",
                    Description = "Choose the time zone for time calculations",
                    Order = 1,
                    GroupName = StratPropertyGroups.TimeParams)]
        public TimeWindowTimeZone TI_TimeZone { get; set; }
        [Display(Name = "FlattenTOD",
                    Description = "Time to close all trades",
                    Order = 2,
                    GroupName = StratPropertyGroups.TimeParams)]
        public string TI_FlattenTOD { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "MaxTimeInTrade",
                    Description = "Maximum number of minutes in trade",
                    Order = 3,
                    GroupName = StratPropertyGroups.TimeParams)]
        public int TI_MaxMinutesInTrade { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "AnchorTime",
                    Description = "Choose the anchor time the offsets a calculated from",
                    Order = 4,
                    GroupName = StratPropertyGroups.TimeParams)]
        public string TI_TWAnchorTime { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "#1 Offset",
                    Description = "Choose the offset from the anchor time",
                    Order = 5,
                    GroupName = StratPropertyGroups.TimeParams)]
        public int TI_TWOffset1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#1 Duration",
                    Description = "Choose the duration for the first time window",
                    Order = 6,
                    GroupName = StratPropertyGroups.TimeParams)]
        public int TI_TWDuration1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(Name = "#2 Offset",
                    Description = "Choose the offset from the anchor time",
                    Order = 7,
                    GroupName = StratPropertyGroups.TimeParams)]
        public int TI_TWOffset2 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Duration",
                    Description = "Choose the duration for the second time window",
                    Order = 8,
                    GroupName = StratPropertyGroups.TimeParams)]
        public int TI_TWDuration2 { get; set; } = 0;
        #endregion

        #region TrendChatGPT[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Display(Name = "TimeZone",
                    Description = "Choose the time zone for time calculations",
                    Order = 1,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public TimeWindowTimeZone TREND_GPT_TI_TimeZone { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ORBStart",
                    Description = "Choose the ORB start time.",
                    Order = 2,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public string TREND_GPT_TI_ORBStartTime { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ORBMinutes",
                    Description = "The number of minutes in ORB window",
                    Order = 3,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_TI_ORBMinutes { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "TradingWindowMinutes",
                    Description = "The number of minutes you can trade after ORB time",
                    Order = 4,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_TI_ORBTradingWindow { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "EMAFastPeriod",
                    Description = "The period for the fast EMA",
                    Order = 5,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_EMAFastPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "EMASlowPeriod",
                    Description = "The period for the slow EMA",
                    Order = 6,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_EMASlowPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "ATRPeriod",
                    Description = "The period for the ATR",
                    Order = 7,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_ATRPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "ADXPeriod",
                    Description = "The period for the ADX",
                    Order = 8,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_ADXPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "MinORBreakATR",
                    Description = "The period for the ADX",
                    Order = 9,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_MinORBreakATR { get; set; } = 0.3;
        [NinjaScriptProperty]
        [Display(   Name = "PullbackMinBars",
                    Description = "The minimum number of bars for a pullback",
                    Order = 10,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_PullbackMinBars { get; set; } = 2;
        [NinjaScriptProperty]
        [Display(   Name = "PullbackMaxBars",
                    Description = "The maximum number of bars for a pullback",
                    Order = 11,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public int TREND_GPT_PullbackMaxBars { get; set; } = 5;
        [NinjaScriptProperty]
        [Display(   Name = "PullbackMaxATR",
                    Description = "The maximum ATR value for a pullback",
                    Order = 12,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_PullbackMaxATR { get; set; } = 1.5;
        [NinjaScriptProperty]
        [Display(   Name = "RiskATR",
                    Description = "The risk ATR value",
                    Order = 13,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_RiskATR { get; set; } = 1.2;
        [NinjaScriptProperty]
        [Display(   Name = "RewardR",
                    Description = "The reward ratio",
                    Order = 14,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_RewardR { get; set; } = 1.5;
        [NinjaScriptProperty]
        [Display(   Name = "BETriggerR",
                    Description = "The break-even trigger ratio",
                    Order = 15,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_BETriggerR { get; set; } = 1.0;
        [NinjaScriptProperty]
        [Display(   Name = "TrailATR",
                    Description = "The trailing ATR value",
                    Order = 16,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_TrailATR { get; set; } = 1.5;
        [NinjaScriptProperty] 
        [Display(   Name = "MinADX",
                    Description = "The minimum ADX value",
                    Order = 17,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_MinADX { get; set; } = 20;
        [NinjaScriptProperty]
        [Display(   Name = "MaxVWAPDistanceATR",
                    Description = "The maximum VWAP distance in ATR",
                    Order = 18,
                    GroupName = StratPropertyGroups.TrendCHATGPT)]
        public double TREND_GPT_MaxVWAPDistanceATR { get; set; } = 2.5;
        #endregion

        #region BreakEven[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "BreakEven R", Order = 1, GroupName = StratPropertyGroups.BreakEven)]
        public double BE_R { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Display(Name = "Use ATR Regime for BE", Order = 2, GroupName = StratPropertyGroups.BreakEven)]
        public bool BE_UseATR { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "ATR Period for BE", Order = 2, GroupName = StratPropertyGroups.BreakEven)]
        public int BE_ATRPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "BE Expanding R", Order = 3, GroupName = StratPropertyGroups.BreakEven)]
        public double BE_Expanding_R { get; set; } = 0.8;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "BE Contracting R", Order = 4, GroupName = StratPropertyGroups.BreakEven)]
        public double BE_Contracting_R { get; set; } = 1.2;
        #endregion

        #region TrendStructuralTrail[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "EMAPeriod", Order = 1, GroupName = StratPropertyGroups.TrendStructureTrail)]
        public int TST_EMAPeriod { get; set; } = 20;

        [NinjaScriptProperty]
        [Range(5, 50)]
        [Display(Name = "ATRPeriod", Order = 2, GroupName = StratPropertyGroups.TrendStructureTrail)]
        public int TST_ATRPeriod { get; set; } = 20;

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name = "ActivationR", Order = 3, GroupName = StratPropertyGroups.TrendStructureTrail)]
        public double TST_ActivationR { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Range(0.0, 3.0)]
        [Display(Name = "ATRMultiplier", Order = 4, GroupName = StratPropertyGroups.TrendStructureTrail)]
        public double TST_ATRMultiplier { get; set; } = 0.75;
        #endregion

        #region ChandelierTrailingStop[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period", Order = 1, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public int CGS_ATRPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Initial ATR Buffer", Order = 2, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_InitialATRBuffer { get; set; } = 0.4;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "BreakEven Trigger R (Expanding)", Order = 3, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_BE_Expanding_R { get; set; } = 0.8;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "BreakEven Trigger R (Contracting)", Order = 4, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_BE_Contracting_R { get; set; } = 1.1;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Chandelier ATR Multiplier", Order = 5, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_ChandelierATRMult { get; set; } = 2.2;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Tightened ATR Multiplier", Order = 6, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_TightATRMult { get; set; } = 1.6;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Tighten Trigger R", Order = 7, GroupName = StratPropertyGroups.ChandelierGuardStop)]
        public double CGS_TightenTriggerR { get; set; } = 2.0;
        #endregion

        #region AdaptiveTrailingStop[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "FastEMAPeriod", Description = "Fast EMA Period", Order = 1, GroupName = StratPropertyGroups.AdaptiveTrailingStop)]
        public int ATS_FastEMAPeriod { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SlowEMAPeriod", Description = "Slow EMA Period", Order = 2, GroupName = StratPropertyGroups.AdaptiveTrailingStop)]
        public int ATS_SlowEMAPeriod { get; set; }
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATRPeriod", Description = "ATR Period", Order = 3, GroupName = StratPropertyGroups.AdaptiveTrailingStop)]
        public int ATS_ATRPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ATRSpreadMultiplier", Description = "ATRSpreadMultiplier", Order = 4, GroupName = StratPropertyGroups.AdaptiveTrailingStop)]
        public double ATS_ATRSpreadMultiplier { get; set; }
        #endregion

        #region EntryParameters[NinjaScriptProperty]
        //Entry parameters
        [NinjaScriptProperty]
        [Display(Name = "TriggerType",
                    Description = "Entry Trigger Type - EntryTrigger",
                    Order = 1,
                    GroupName = StratPropertyGroups.Entry)]
        public EntryTriggerType EntryTriggerType { get; set; } = EntryTriggerType.None;

        [NinjaScriptProperty]
        [Display(   Name = "OrderType",
                    Description = "Order behavior - OrderType",
                    Order = 1,
                    GroupName = StratPropertyGroups.Entry)]
        public EntryOrderType EntryOrderType { get; set; } = EntryOrderType.Market;

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(   Name = "EntryExpiryBars",
                    Description = "Order behavior - EntryExpiryBars",
                    Order = 2,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryExpiryBars { get; set; } = 3;
        #endregion

        #region Properties
        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.TREND;
        #endregion

        #region overrides
        protected override void OnStateChange()
        {
            // nlog gets configured in base class so
            // we shouldn't log anything until after this call.
            base.OnStateChange();

            logger.Trace($"State = {State}");

            if (State == State.SetDefaults)
            {
                Description = @"Trend Strategy";
                Name = "DA--" + stratIdentifier;
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = false;  // Or true if you want auto-exit at session close
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;  // Standard safe default
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = true;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

                // Default parameter values
                // initially set in the optimization parameters class and transferred to here
                // when we are initializing this strategy. The OptimizationParameters_ORB class
                // may be used outsisde this strategy.
                OptimizationParameters_TREND OptParams = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_TREND;
                OptParams.SetDefaultValues();
                OptParams.UpdateStratParamValues();
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                OptimizationParameters_TREND OptParams = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_TREND;
                OptParams.UpdateFromStrat();

                Indicators_TREND indicators = GetIndicators("IDC-" + stratIdentifier) as Indicators_TREND;
                indicators.OptParams = OptParams;
                indicators.Initialize();

                IEntryConditionsEvaluator ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier);
                ece.OrderIdPrefix = "DA" + stratIdentifier;
                ece.Reset();
                ece.Indicators = indicators;
                ece.OptParams = OptParams;

                TradeContext tc = new TradeContext();
                List<TradeState> stateList = new List<TradeState>()
                {
                    TradeState.Idle,
                    TradeState.FillPending,
                };

                if (OptParams.SLTrailingMode == StopLossTrailingMode.TrendStructuralTrailing)
                {
                    stateList.Add(TradeState.TrailingStopTrendStructural);
                }
                if (OptParams.SLTrailingMode == StopLossTrailingMode.TrailingChandelierGuard)
                {   
                    stateList.Add(TradeState.TrailingStopChandelierGuard);
                }
                else if (OptParams.SLTrailingMode == StopLossTrailingMode.TrailingAdaptive)
                {
                    stateList.Add(TradeState.TrailingStopAdaptive);
                }
                if (OptParams.SLTrailingMode == StopLossTrailingMode.BreakEven)
                {
                    stateList.Add(TradeState.BreakEvenPending);
                    stateList.Add(TradeState.InPosition);
                }
                else
                {   // if not one we've implemented special handling for,
                    // just add the InPosition state
                    stateList.Add(TradeState.InPosition);
                }

                stateList.Add(TradeState.Exited);
                // this state is explicitely set in the code along with
                // a pendingNextState. We put it here so it will never get hit
                // when incrementing from state to state. When we hit exited the
                // tradeContext resets.
                stateList.Add(TradeState.StopMovePending);

                tc.StateList = stateList;
                tc.SetState(TradeState.Idle);
                tc.EntryConditionsEvaluator = ece;
                if (OptParams.Time.TimeZone != TimeWindowTimeZone.None && !String.IsNullOrEmpty(OptParams.Time.FlattenTOD))
                {
                    TradeManager.FlattenTOD = new TimeConverter().ToDataTimeOfDay(
                        OptParams.Time.FlattenTOD, 
                        OptParams.Time.TimeZone.GetDisplayName());
                }
                TradeManager.AddTradeContext(tc);
                TradeManager.Indicators = indicators;
                TradeManager.OptParams = OptParams;
                TradeManager.OnDataLoaded();
            }
            else if (State == State.Historical)
            {

            }
            else if (State == State.Terminated)
            {
                LogManager.Flush();
            }
        }

        protected override void OnBacktestComplete()
        {   //do whatever you need to do at the end of a backtest here. Logging final results, etc.
            ECE_TREND ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier) as ECE_TREND;
            OptimizationParameters_TREND optParamsTREND = ece.OptParamsTREND;
            Indicators_TREND indicatorsTREND = ece.IndicatorsTREND;
            TimeConverter tc = new TimeConverter();
            TimeZoneInfo EastTZI = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("==Backtest complete==");
            sb.AppendFormat("Backtest date range from {0:M/d/yy} to {1:M/d/yy}", Bars.GetTime(0), Bars.GetTime(Bars.Count - 1)).AppendLine();
            optParamsTREND.ToStringBuilder(sb);
            sb.AppendLine("==Entry Trigger Data==");
            sb.AppendFormat("  InTradeTimeWindowCount:{0}", ece.DataCollector.InTradeTimeWindowCount).AppendLine();
            sb.AppendFormat("  PassedMinAdxFilterCount:{0}", ece.DataCollector.PassedMinAdxFilterCount).AppendLine();
            sb.AppendFormat("  PassedMaxVWAPDistanceFilterCount:{0}", ece.DataCollector.PassedMaxVWAPDistanceFilterCount).AppendLine();
            sb.AppendFormat("  IsValidPullbackCount:{0}", ece.DataCollector.IsValidPullbackCount).AppendLine();
            sb.AppendFormat("  LongEntryTriggeredCount:{0}", ece.DataCollector.LongTradeTriggeredCount).AppendLine();
            sb.AppendFormat("  ShortEntryTriggeredCount:{0}", ece.DataCollector.ShortTradeTriggeredCount).AppendLine();
            logger.Info(sb.ToString());
        }
        #endregion
    }
}

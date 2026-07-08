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
    [Gui.CategoryDefaultExpanded(false)]
    [Gui.CategoryOrder(StratPropertyGroups.Parameters, 1), Gui.CategoryExpanded(StratPropertyGroups.Parameters, true)]
    [Gui.CategoryOrder(StratPropertyGroups.Indicators, 2)]
    [Gui.CategoryOrder(StratPropertyGroups.ATRRegimeFilter, 3)]
    [Gui.CategoryOrder(StratPropertyGroups.StopLoss, 4)]
    [Gui.CategoryOrder(StratPropertyGroups.AdaptiveTrailingStop, 5)]
    #endregion
    public class Strat_ORB : StratBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        public override TradeManagerBase TradeManager
        {
            get
            {
                if (_tmb == null)
                {
                    _tmb = new TradeManager_ORB(this);
                }
                return _tmb;
            }
        }
        #endregion

        #region NinjascriptProperties
        [NinjaScriptProperty]
        [Range(5, 45)]
        [Display(Name = "Opening Range Minutes", Description = "Duration of the opening range in minutes", Order = 1, GroupName = StratPropertyGroups.Parameters)]
        public int OpeningRangeMinutes { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 2.5)]
        [Display(Name = "BreakoutBarVolMult", Description = "BreakoutBarVolumeMultiplier", Order = 2, GroupName = StratPropertyGroups.Parameters)]
        public double BreakoutBarVolumeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0, 150)]
        [Display(Name = "ORMaxWidth", Description = "MaxRangeWidthPercentOfAverage", Order = 3, GroupName = StratPropertyGroups.Parameters)]
        public int ORMaxWidth { get; set; }

        [NinjaScriptProperty]
        [Range(0, 50)]
        [Display(Name = "ORMinWidth", Description = "MinRangeWidthPercentOfAverage", Order = 4, GroupName = StratPropertyGroups.Parameters)]
        public int ORMinWidth { get; set; }

        [NinjaScriptProperty]
        [Range(45, 180)]
        [Display(Name = "SessionTradingMinutes", Description = "Minutes from NYOpen you can trade", Order = 5, GroupName = StratPropertyGroups.Parameters)]
        public int SessionTradingMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EarlyExitMode", Description = "How to handle early exit", Order = 7, GroupName = StratPropertyGroups.Parameters)]
        public EarlyExitMode EarlyExitMode { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 3.0)]
        [Display(Name = "TPMultiple", Description = "TakeProfitRiskMultiple", Order = 8, GroupName = StratPropertyGroups.Parameters)]
        public double TakeProfitRiskMultiple { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVWAP", Description = "EnableVWAP", Order = 9, GroupName = StratPropertyGroups.Parameters)]
        public bool EnableVWAP { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVWAPSlope", Description = "EnableVWAP", Order = 10, GroupName = StratPropertyGroups.Parameters)]
        public bool EnableVWAPSlope { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "BreakAndRetest", Description = "BreakAndRetest", Order = 11, GroupName = StratPropertyGroups.Parameters)]
        public bool BreakAndRetest { get; set; }

        [NinjaScriptProperty]
        [Range(0, 300)]
        [Display(Name = "MaxMinutesInTrade", Description = "Maximum minutes a trade is allowed to be active.", Order = 12, GroupName = StratPropertyGroups.Parameters)]
        public int MaxMinutesInTrade { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PreMarketFiltering", Description = "PreMarketFiltering", Order = 13, GroupName = StratPropertyGroups.Parameters)]
        public bool PreMarketFiltering { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "RiskAccountPercent", GroupName = StratPropertyGroups.Indicators, Order = 1)]
        public int RiskAccountPercent { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ProfitRDynoExit", GroupName = StratPropertyGroups.Indicators, Order = 2)]
        public double ProfitRForDynamicExit { get; set; }
        // ATR Regime filter parameters
        [NinjaScriptProperty]
        [Display(Name = "Enable", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 1)]
        public bool EnableAtrRegimeFilter { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ATRFastPeriod", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 2)]
        public int AtrRegimeFastPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ATRSlowPeriod", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 3)]
        public int AtrRegimeSlowPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "ATRPeriod", Description = "ATR base period for the normalized ATR.", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 4)]
        public int AtrRegimeAtrPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "MinAtrRatio", Description = "Threshold for allowing trades.", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 5)]
        public double MinAtrRegimeRatio { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "MinAtrPercent", Description = "Absolute volatility floor.", GroupName = StratPropertyGroups.ATRRegimeFilter, Order = 6)]
        public double MinAtrPercent { get; set; }
        // Stop loss parameters
        [NinjaScriptProperty]
        [Display(Name = "SLTrailingMode", Description = "How to move stop loss after initial placement", Order = 1, GroupName = StratPropertyGroups.StopLoss)]
        public StopLossTrailingMode SLTrailingMode { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "SLInitialPlacement", Description = "Method used to place the initial stop loss on entry", Order = 2, GroupName = StratPropertyGroups.StopLoss)]
        public StopLossInitialPlacement SLInitialPlacement { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "SLIP_ATRPeriod", Description = "ATR Period to use in ATR initial SL calculations", Order = 3, GroupName = StratPropertyGroups.StopLoss)]
        public int StopLossIP_ATRPeriod { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "SLIP_ATRMultiplier", Description = "ATR Multiplier to use in ATR initial SL calculations", Order = 4, GroupName = StratPropertyGroups.StopLoss)]
        public double StopLossIP_ATRMult { get; set; }
        [NinjaScriptProperty]
        [Range(0.0, 2.0)]
        [Display(Name = "MoonshotORD", Description = "MoonshotOutsideRangeDistance", Order = 5, GroupName = StratPropertyGroups.StopLoss)]
        public double MoonshotORD { get; set; }
        [NinjaScriptProperty]
        [Range(0.0, 2.0)]
        [Display(Name = "MidpointORD", Description = "MidpointOutsideRangeDistance", Order = 6, GroupName = StratPropertyGroups.StopLoss)]
        public double MidpointORD { get; set; }
        [NinjaScriptProperty]
        [Range(0, 200)]
        [Display(Name = "MidpointAvgRange", Description = "MidpointAboveAverageRange", Order = 7, GroupName = StratPropertyGroups.StopLoss)]
        public int MidpointAvgRange { get; set; }
        [NinjaScriptProperty]
        [Range(7, 30)]
        [Display(Name = "TrailingATRPeriod", Description = "TrailingATRPeriod", Order = 8, GroupName = StratPropertyGroups.StopLoss)]
        public int TrailingATRPeriod { get; set; }
        [NinjaScriptProperty]
        [Range(.1, 5.0)]
        [Display(Name = "TrailingATRMultiplier", Description = "TrailingATRMultiplier", Order = 9, GroupName = StratPropertyGroups.StopLoss)]
        public double TrailingATRMultiplier { get; set; }
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

        #region Properties
        // not making thesevisible just yet
        [Browsable(false)]
        public int RSIPeriod { get; set; }
        [Browsable(false)]
        public int ATRPeriod { get; set; }
        [Browsable(false)]
        public int FastEMAPeriod { get; set; }
        [Browsable(false)]
        public int SlowEMAPeriod { get; set; }
        public int VolumeAverageBarCount { get; set; }

        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.ORB;
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
                Description = @"Opening Range Breakout";
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
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

                // Default parameter values
                // initially set in the optimization parameters class and transferred to here
                // when we are initializing this strategy. The OptimizationParameters_ORB class
                // may be used outsisde this strategy.
                OptimizationParameters_ORB OptParamsORB = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_ORB;
                OptParamsORB.SetDefaultValues();
                OptParamsORB.UpdateStratParamValues();
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                OptimizationParameters_ORB OptParamsORB = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_ORB;
                OptParamsORB.UpdateFromStrat();

                Indicators_ORB indicators = GetIndicators("IDC-" + stratIdentifier) as Indicators_ORB;
                indicators.OptParams = OptParamsORB;
                indicators.Initialize();

                IEntryConditionsEvaluator ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier);
                ece.OrderIdPrefix = "DA" + stratIdentifier;
                ece.Reset();
                ece.Indicators = indicators;
                ece.OptParams = OptParamsORB;

                TradeContext tc = new TradeContext();
                List<TradeState> stateList = new List<TradeState>()
                {
                    TradeState.Idle,
                    TradeState.FillPending,
                };

                if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.Fixed)
                {
                    stateList.Add(TradeState.InPosition);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.BreakEven)
                {
                    stateList.Add(TradeState.BreakEvenPending);
                    stateList.Add(TradeState.FillPending);
                    stateList.Add(TradeState.InPosition);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.BreakEvenStaged)
                {
                    stateList.Add(TradeState.BreakEvenPending2Stage);
                    stateList.Add(TradeState.FillPending);
                    stateList.Add(TradeState.InPosition);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.BreakEvenThenTrail)
                {
                    stateList.Add(TradeState.BreakEvenPending);
                    stateList.Add(TradeState.FillPending);
                    stateList.Add(TradeState.TrailingStop);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.Trailing)
                {
                    stateList.Add(TradeState.TrailingStop);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.TrailingAdaptive)
                {
                    stateList.Add(TradeState.TrailingStopAdaptive);
                }
                else if (OptParamsORB.StopLoss.SLTrailingMode == StopLossTrailingMode.TrailingATRRatchet)
                {
                    stateList.Add(TradeState.TrailingStopATRRatchet);
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
                TradeManager.FlattenTOD = new TimeConverter().ToDataTimeOfDay("2:55pm", "Eastern Standard Time");
                TradeManager.AddTradeContext(tc);
                TradeManager.Indicators = indicators;
                TradeManager.OptParams = OptParamsORB;
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
            ECE_ORB ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier) as ECE_ORB;
            OptimizationParameters_ORB optParamsORB = ece.OptParamsORB;
            Indicators_ORB indicatorsORB = ece.IndicatorsORB;
            TimeConverter tc = new TimeConverter();
            TimeZoneInfo EastTZI = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            int totalTrades = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("==Backtest complete==");
            optParamsORB.ToStringBuilder(sb);
            sb.AppendFormat("Backtest date range from {0:M/d/yy} to {1:M/d/yy}", Bars.GetTime(0), Bars.GetTime(Bars.Count - 1)).AppendLine();
            sb.AppendFormat("All times in {0}", EastTZI.DisplayName).AppendLine();
            sb.AppendLine("==EntryConditionsEvaluator==");
            sb.AppendFormat("Opening range established from {0:hh:mmtt} to {1:hh:mmtt}",
                tc.FromDataTimeOfDayDate(indicatorsORB.OpeningRange.RangeStartTOD, EastTZI),
                tc.FromDataTimeOfDayDate(indicatorsORB.OpeningRange.RangeEndTOD, EastTZI)).AppendLine();
            sb.AppendFormat("Entering trades allowed until {0:hh:mm tt}",
                tc.FromDataTimeOfDayDate(indicatorsORB.OpeningRange.TradingEndTime(), EastTZI)).AppendLine();
            sb.AppendLine("  ==Triggers==");
            if (ece.OptParamsORB.BreakAndRetest == true)
            {
                totalTrades = ece.DataCollector.BreakAndRetestTriggerLongCount + ece.DataCollector.BreakAndRetestTriggerShortCount;
                sb.AppendFormat("  BreakAndRetestTriggerLongCount = {0}", ece.DataCollector.BreakAndRetestTriggerLongCount).AppendLine();
                sb.AppendFormat("  BreakAndRetestTriggerShortCount = {0}", ece.DataCollector.BreakAndRetestTriggerShortCount).AppendLine();
                sb.AppendFormat("  TotalCount = {0}", totalTrades).AppendLine();
            }
            else
            {
                totalTrades = ece.DataCollector.CloseOutsideOpeningRangeTriggerLongCount + ece.DataCollector.CloseOutsideOpeningRangeTriggerShortCount;
                sb.AppendFormat("  CloseOutsideOpeningRangeTriggerLongCount = {0}", ece.DataCollector.CloseOutsideOpeningRangeTriggerLongCount).AppendLine();
                sb.AppendFormat("  CloseOutsideOpeningRangeTriggerShortCount = {0}", ece.DataCollector.CloseOutsideOpeningRangeTriggerShortCount).AppendLine();
                sb.AppendFormat("  TotalCount = {0}", totalTrades).AppendLine();
            }
            sb.AppendLine("  ==Filters==");
            if (optParamsORB.BreakoutBarVolumeMultiplier == 0)
            {
                sb.AppendLine("  BreakoutBarVolumeFilter => DISABLED");
            }
            else
            {
                totalTrades -= ece.DataCollector.BreakoutBarVolumeCheckFailedCount;
                sb.AppendFormat("  BreakoutBarVolumeFilter FilteredOut = {0}  RunningTotal = {1}", ece.DataCollector.BreakoutBarVolumeCheckFailedCount, totalTrades).AppendLine();
                sb.AppendFormat("    Must be > average of previous {0} bars * {1}", optParamsORB.VolumeAverageBarCount, optParamsORB.BreakoutBarVolumeMultiplier).AppendLine();
            }
            if (optParamsORB.ORMinWidth > 0 && optParamsORB.ORMaxWidth > 0)
            {
                totalTrades -= ece.DataCollector.OpeningRangeWidthCheckFailedCount;
                sb.AppendFormat("  OpeningRangeWidthFilter FilteredOut = {0}  RunningTotal = {1}", ece.DataCollector.OpeningRangeWidthCheckFailedCount, totalTrades).AppendLine();
                sb.AppendFormat("    Opening Range must be > {0}% of AverageRange && < {1}% of AverageRange", optParamsORB.ORMinWidth, optParamsORB.ORMaxWidth).AppendLine();
            }
            else
            {
                sb.AppendLine("  OpeningRangeWidthFilter => DISABLED");
            }
            if (optParamsORB.EnableVWAP == true)
            {
                totalTrades -= ece.DataCollector.VWAPLongCheckFailedCount;
                sb.AppendFormat("  VWAP Long FilteredOut = {0}  RunningTotal = {1}", ece.DataCollector.VWAPLongCheckFailedCount, totalTrades).AppendLine();
                totalTrades -= ece.DataCollector.VWAPShortCheckFailedCount;
                sb.AppendFormat("  VWAP Short FilteredOut = {0}  Running Total = {1}", ece.DataCollector.VWAPShortCheckFailedCount, totalTrades).AppendLine();
                sb.AppendLine("    VWAP Anchored to beginning of NY RTH.");
            }
            else
            {
                sb.AppendLine("  VWAP Filtering => DISABLED");
            }
            if (optParamsORB.EnableVWAPSlope == true)
            {
                totalTrades -= ece.DataCollector.VWAPLongCheckSlopeFailedCount;
                sb.AppendFormat("  VWAP Long Slope FilteredOut = {0}  RunningTotal = {1}", ece.DataCollector.VWAPLongCheckSlopeFailedCount, totalTrades).AppendLine();
                totalTrades -= ece.DataCollector.VWAPShortCheckSlopeFailedCount;
                sb.AppendFormat("  VWAP Short Slope FilteredOut = {0}  Running Total = {1}", ece.DataCollector.VWAPShortCheckSlopeFailedCount, totalTrades).AppendLine();
                sb.AppendLine("    VWAP Anchored to beginning of NY RTH.");
            }
            else
            {
                sb.AppendLine("  VWAP Slope Filtering => DISABLED");
            }
            sb.AppendLine("==InitialStopPlacement==");
            sb.AppendLine("  ==Parameters==");
            sb.AppendFormat("  BreakoutAboveOpeningRangeMoonshot = {0}", optParamsORB.StopLoss.MoonshotORD).AppendLine();
            sb.AppendFormat("  BreakoutAboveOpeningRangeMidpoint = {0}", optParamsORB.StopLoss.MidpointORD).AppendLine();
            sb.AppendFormat("  OpeningRangeMidpointPercentageAboveNormal = {0}", optParamsORB.StopLoss.MidpointAvgRange).AppendLine();
            sb.AppendLine("  ==Results==");
            sb.AppendFormat("  MoonshotOpeningRangeLong = {0}", ece.DataCollector.SIPLongMoonshotCount).AppendLine();
            sb.AppendFormat("  MoonshotOpeningRangeShort = {0}", ece.DataCollector.SIPShortMoonshotCount).AppendLine();
            sb.AppendFormat("  MidpointOpeningRangeLong = {0}", ece.DataCollector.SIPLongMidpointCount).AppendLine();
            sb.AppendFormat("  MidpointOpeningRangeShort = {0}", ece.DataCollector.SIPShortMidpointCount).AppendLine();
            sb.AppendFormat("  OppositeSideOpeningRangeLong = {0}", ece.DataCollector.SIPLongOppositeBreakCount).AppendLine();
            sb.AppendFormat("  OppositeSideOpeningRangeShort = {0}", ece.DataCollector.SIPShortOppositeBreakCount).AppendLine();
            sb.AppendLine("==TradeManager==");
            sb.AppendFormat("  Flatten time set to {0:hh:mmtt}",
                tc.FromDataTimeOfDayDate(TradeManager.FlattenTOD, EastTZI)).AppendLine();
            sb.AppendFormat("  FlattenCountLong = {0}", TradeManager.TradeData.FlattenCountLong).AppendLine();
            sb.AppendFormat("  FlattenCountShort = {0}", TradeManager.TradeData.FlattenCountShort).AppendLine();
            if (optParamsORB.MaxMinutesInTrade == 0)
            {
                sb.AppendLine("  MaxMinutesInTrade => DISABLED");
            }
            else
            {
                sb.AppendFormat("  MaxMinutesInTrade = {0}", optParamsORB.GetMaxMinutesInTrade()).AppendLine();
                sb.AppendFormat("  MaxMinutesLongTriggeredCount = {0}", TradeManager.TradeData.MaxTimeInTradeCountLong).AppendLine();
                sb.AppendFormat("  MaxMinutesShortTriggeredCount = {0}", TradeManager.TradeData.MaxTimeInTradeCountShort).AppendLine();
            }

            logger.Info(sb.ToString());
        }
        #endregion
    }
}

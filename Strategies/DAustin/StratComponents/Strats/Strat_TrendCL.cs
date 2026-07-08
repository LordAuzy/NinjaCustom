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
    [Gui.CategoryOrder(StratPropertyGroups.Entry, 3), Gui.CategoryExpanded(StratPropertyGroups.Entry, false)]
    [Gui.CategoryOrder(StratPropertyGroups.TrendStructureTrail, 4), Gui.CategoryExpanded(StratPropertyGroups.TrendStructureTrail, false)]
    #endregion
    public class Strat_TrendCL : StratBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region GeneralParameters[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(.25, 4.0)]
        [Display(Name = "EquityRiskPct", GroupName = StratPropertyGroups.GeneralParameters, Order = 1)]
        public double EquityRiskPct { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "MaxTradesPerSession", GroupName = StratPropertyGroups.GeneralParameters, Order = 1)]
        public int MaxTradesPerSession { get; set; }

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

        #region EntryParameters[NinjaScriptProperty]
        //Entry parameters
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "Fast EMA Period",
                    Description = "Fast EMA Period",
                    Order = 1,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryFastEMAPeriod { get; set; } = 9;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Mid EMA Period",
                    Description = "Mid EMA Period",
                    Order = 2,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryMidEMAPeriod { get; set; } = 21;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "Slow EMA Period",
                    Description = "Slow EMA Period",
                    Order = 3,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntrySlowEMAPeriod { get; set; } = 21;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period",
                    Description = "ATR Period",
                    Order = 4,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryATRPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ADX Period",
                    Description = "ADX Period",
                    Order = 5,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryADXPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Range(15, 50)]
        [Display(   Name = "ADXMinValue",
                    Description = "Minimum ADX to consider market trending (default 22)",
                    Order = 6,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryAdxMinimum { get; set; }

        [NinjaScriptProperty]
        [Display(   Name = "MinATR(points)",
                    Description = "Skip entries if ATR is below this (avoid chop). Default 8 pts",
                    Order = 7,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMinAtrPoints { get; set; }

        [NinjaScriptProperty]
        [Display(   Name = "OrderType",
                    Description = "Order behavior - OrderType",
                    Order = 8,
                    GroupName = StratPropertyGroups.Entry)]
        public EntryOrderType EntryOrderType { get; set; } = EntryOrderType.Market;

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "EntryExpiryBars",
                    Description = "Order behavior - EntryExpiryBars",
                    Order = 9,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryExpiryBars { get; set; } = 3;

        [NinjaScriptProperty]
        [Range(0.5, 5.0)]
        [Display(   Name = "ATRStopMultiplier",
                    Description = "Initial stop = ATR × this multiplier (default 1.5)",
                    Order = 10,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryAtrStopMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0.0, 8.0)]
        [Display(   Name = "ATRTargetMultiplier",
                    Description = "Profit target = ATR × this multiplier (default 3.0)",
                    Order = 11,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryAtrTargetMultiplier { get; set; }
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

        #region Properties
        //        public TimeWindows EntryTimeWindows { get; set; }

        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.TRENDCL;
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
                Description = @"Trend Continuation";
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
                OptimizationParameters_TRENDCL OptParams = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_TRENDCL;
                OptParams.SetDefaultValues();
                OptParams.UpdateStratParamValues();
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                OptimizationParameters_TRENDCL OptParams = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_TRENDCL;
                OptParams.UpdateFromStrat();

                Indicators_TRENDCL indicators = GetIndicators("IDC-" + stratIdentifier) as Indicators_TRENDCL;
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
                else if (OptParams.SLTrailingMode == StopLossTrailingMode.TrailingChandelierGuard)
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
            ECE_TRENDCL ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier) as ECE_TRENDCL;
            OptimizationParameters_TRENDCL optParams = ece.OptParamsTRENDCL;
            Indicators_TRENDCL indicators = ece.IndicatorsTRENDCL;
            TimeConverter tc = new TimeConverter();
            TimeZoneInfo EastTZI = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("==Backtest complete==");
            sb.AppendFormat("Backtest date range from {0:M/d/yy} to {1:M/d/yy}", Bars.GetTime(0), Bars.GetTime(Bars.Count - 1)).AppendLine();
            optParams.ToStringBuilder(sb);
            sb.AppendLine("==Entry Trigger Data==");
            sb.AppendFormat("  AboveVWAPCount:{0}", ece.DataCollector.AboveVWAPCount).AppendLine();
            sb.AppendFormat("  UpTrendCount:{0}", ece.DataCollector.UpTrendCount).AppendLine();
            sb.AppendFormat("  UpTrendChopZoneCount:{0}", ece.DataCollector.UpTrendChopZoneCount).AppendLine();
            sb.AppendFormat("  ValidPullbackLongCount:{0}", ece.DataCollector.ValidPullbackLongCount).AppendLine();
            sb.AppendFormat("  BullishTriggerCount:{0}", ece.DataCollector.BullishTriggerCount).AppendLine();
            sb.AppendFormat("  LongEntryTriggeredCount:{0}", ece.DataCollector.LongEntryTriggeredCount).AppendLine();
            sb.AppendFormat("  BelowVWAPCount:{0}", ece.DataCollector.BelowVWAPCount).AppendLine();
            sb.AppendFormat("  DownTrendCount:{0}", ece.DataCollector.DownTrendCount).AppendLine();
            sb.AppendFormat("  DownTrendChopZoneCount:{0}", ece.DataCollector.DownTrendChopZoneCount).AppendLine();
            sb.AppendFormat("  ValidPullShortCount:{0}", ece.DataCollector.ValidPullShortCount).AppendLine();
            sb.AppendFormat("  BearishTriggerCount:{0}", ece.DataCollector.BearishTriggerCount).AppendLine();
            sb.AppendFormat("  ShortEntryTriggeredCount:{0}", ece.DataCollector.ShortEntryTriggeredCount).AppendLine();
            logger.Info(sb.ToString());
        }
        #endregion
    }
}

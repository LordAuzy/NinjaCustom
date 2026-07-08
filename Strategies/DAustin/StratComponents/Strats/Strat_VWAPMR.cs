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
    [Gui.CategoryOrder(StratPropertyGroups.Test, 4), Gui.CategoryExpanded(StratPropertyGroups.Test, false)]
    #endregion
    public class Strat_VWAPMR : StratBase
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

        #region EntryParameters[NinjaScriptProperty]
        //Entry parameters
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "ATR Period",
                    Description = "ATR Period - Indicators",
                    Order = 1,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryATRPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "VWAPSlopeLookback",
                    Description = "VWAPSlopeLookback - Indicators",
                    Order = 2,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryVWAPSlopeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(   Name = "Min ATR Filter",
                    Description = "Min ATR Filter - Filters",
                    Order = 3,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMinATRFilter { get; set; } = 21;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MaxVWAPSlopeATR",
                    Description = "MaxVWAPSlopeATR - Filters",
                    Order = 4,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMaxVWAPSlopeATR { get; set; } = 0.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "DeviationATR",
                    Description = "DeviationATR - Deviation",
                    Order = 5,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryDeviationATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "StopATR",
                    Description = "StopATR - Risk",
                    Order = 6,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryStopATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Display(Name = "OrderType",
                    Description = "Order behavior - OrderType",
                    Order = 7,
                    GroupName = StratPropertyGroups.Entry)]
        public EntryOrderType EntryOrderType { get; set; } = EntryOrderType.Market;

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name = "EntryExpiryBars",
                    Description = "Order behavior - EntryExpiryBars",
                    Order = 8,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryExpiryBars { get; set; } = 3;
        #endregion

        #region TestParameters[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "FastEMAPeriod",
                    Description = "FastEMAPeriod",
                    Order = 1,
                    GroupName = StratPropertyGroups.Test)]
        public int TestFastEMAPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "SlowEMAPeriod",
                    Description = "SlowEMAPeriod",
                    Order = 2,
                    GroupName = StratPropertyGroups.Test)]
        public int TestSlowEMAPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "TrendEMAPeriod",
                    Description = "TrendEMAPeriod",
                    Order = 3,
                    GroupName = StratPropertyGroups.Test)]
        public int TestTrendEMAPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "RSIPeriod",
                    Description = "RSIPeriod",
                    Order = 4,
                    GroupName = StratPropertyGroups.Test)]
        public int TestRSIPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "RSIOverBought",
                    Description = "RSIOverBought",
                    Order = 5,
                    GroupName = StratPropertyGroups.Test)]
        public int TestRSIOverBought { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "RSIOverSold",
                    Description = "RSIOverSold",
                    Order = 6,
                    GroupName = StratPropertyGroups.Test)]
        public int TestRSIOverSold { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "ADXPeriod",
                    Description = "ADXPeriod",
                    Order = 7,
                    GroupName = StratPropertyGroups.Test)]
        public int TestADXPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "ADXThreshold",
                    Description = "ADXThreshold",
                    Order = 8,
                    GroupName = StratPropertyGroups.Test)]
        public int TestADXThreshold { get; set; }
        #endregion

        #region Properties
        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.VWAPMR;
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
                Description = @"VWAP MeanReversion";
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
                OptimizationParameters_VWAPMR OptParamsVWAPMR = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_VWAPMR;
                OptParamsVWAPMR.SetDefaultValues();
                OptParamsVWAPMR.UpdateStratParamValues();
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                OptimizationParameters_VWAPMR OptParamsVWAPMR = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_VWAPMR;
                OptParamsVWAPMR.UpdateFromStrat();

                Indicators_VWAPMR indicators = GetIndicators("IDC-" + stratIdentifier) as Indicators_VWAPMR;
                indicators.OptParams = OptParamsVWAPMR;
                indicators.Initialize();

                IEntryConditionsEvaluator ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier);
                ece.OrderIdPrefix = "DA" + stratIdentifier;
                ece.Reset();
                ece.Indicators = indicators;
                ece.OptParams = OptParamsVWAPMR;

                TradeContext tc = new TradeContext();
                List<TradeState> stateList = new List<TradeState>()
                {
                    TradeState.Idle,
                    TradeState.FillPending,
                };

                if (OptParamsVWAPMR.SLTrailingMode == StopLossTrailingMode.VWAPMeanReversion)
                {   
                    stateList.Add(TradeState.TrailingStopVWAPMeanReversion);
                }
                else if (OptParamsVWAPMR.SLTrailingMode == StopLossTrailingMode.TrailingAdaptive)
                {
                    stateList.Add(TradeState.TrailingStopAdaptive);
                }
                if (OptParamsVWAPMR.SLTrailingMode == StopLossTrailingMode.BreakEven)
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
                if (OptParamsVWAPMR.Time.TimeZone != TimeWindowTimeZone.None && !String.IsNullOrEmpty(OptParamsVWAPMR.Time.FlattenTOD))
                {
                    TradeManager.FlattenTOD = new TimeConverter().ToDataTimeOfDay(
                        OptParamsVWAPMR.Time.FlattenTOD, 
                        OptParamsVWAPMR.Time.TimeZone.GetDisplayName());
                }
                TradeManager.AddTradeContext(tc);
                TradeManager.Indicators = indicators;
                TradeManager.OptParams = OptParamsVWAPMR;
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
            ECE_VWAPMR ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier) as ECE_VWAPMR;
            OptimizationParameters_VWAPMR optParamsVWAPMR = ece.OptParamsVWAPMR;
            Indicators_VWAPMR indicatorsVWAPMR = ece.IndicatorsVWAPMR;
            TimeConverter tc = new TimeConverter();
            TimeZoneInfo EastTZI = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("==Backtest complete==");
            sb.AppendFormat("Backtest date range from {0:M/d/yy} to {1:M/d/yy}", Bars.GetTime(0), Bars.GetTime(Bars.Count - 1)).AppendLine();
            optParamsVWAPMR.ToStringBuilder(sb);
            sb.AppendLine("==Entry Trigger Data==");
            sb.AppendFormat("  CheckForEntryCount:{0}", ece.DataCollector.CheckForEntryCount).AppendLine();
            sb.AppendFormat("  PassedMinATRFilterCount:{0}", ece.DataCollector.PassedMinATRFilter).AppendLine();
            sb.AppendFormat("  PassedMaxVWAPSlopeFilterCount:{0}", ece.DataCollector.PassedMaxVWAPSlopeFilter).AppendLine();
            sb.AppendFormat("  PassedDeviationFilterCount:{0}", ece.DataCollector.PassedDeviationFilter).AppendLine();
            sb.AppendFormat("  CurrentPriceBelowVWAPCount:{0}", ece.DataCollector.CurrentPriceBelowVWAPCount).AppendLine();
            sb.AppendFormat("  LongEntryTriggeredCount:{0}", ece.DataCollector.LongEntryTriggered).AppendLine();
            sb.AppendFormat("  CurrentPriceAboveVWAPCount:{0}", ece.DataCollector.CurrentPriceAboveVWAPCount).AppendLine();
            sb.AppendFormat("  ShortEntryTriggeredCount:{0}", ece.DataCollector.ShortEntryTriggered).AppendLine();
            logger.Info(sb.ToString());
        }
        #endregion
    }
}

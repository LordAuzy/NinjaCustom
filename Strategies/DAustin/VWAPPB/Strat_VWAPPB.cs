#region Using declarations
using ActiproSoftware.Windows.Media.Animation;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.TradeManagers;
using NinjaTrader.Custom.Strategies.DAustin.VWAPPB;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.AccountData;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using NLog;
using NLog.Config;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using static NinjaTrader.CQG.ProtoBuf.Quote.Types;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    #region CategoryValues
    [Gui.CategoryOrder(StratPropertyGroups.GeneralParameters, 1), Gui.CategoryExpanded(StratPropertyGroups.GeneralParameters, true)]
    [Gui.CategoryOrder(StratPropertyGroups.TimeParams, 2), Gui.CategoryExpanded(StratPropertyGroups.TimeParams, false)]
    [Gui.CategoryOrder(StratPropertyGroups.ScheduleBiasFilter, 3), Gui.CategoryExpanded(StratPropertyGroups.ScheduleBiasFilter, false)]
    [Gui.CategoryOrder(StratPropertyGroups.ScheduleSizingFilter, 4), Gui.CategoryExpanded(StratPropertyGroups.ScheduleSizingFilter, false)]
    [Gui.CategoryOrder(StratPropertyGroups.BreakEven, 5), Gui.CategoryExpanded(StratPropertyGroups.BreakEven, false)]
    [Gui.CategoryOrder(StratPropertyGroups.Entry, 6), Gui.CategoryExpanded(StratPropertyGroups.Entry, false)]
    [Gui.CategoryOrder(StratPropertyGroups.TrendStructureTrail, 7), Gui.CategoryExpanded(StratPropertyGroups.TrendStructureTrail, false)]
    [Gui.CategoryOrder(StratPropertyGroups.ChandelierGuardStop, 8), Gui.CategoryExpanded(StratPropertyGroups.ChandelierGuardStop, false)]
    [Gui.CategoryOrder(StratPropertyGroups.AdaptiveTrailingStop, 9), Gui.CategoryExpanded(StratPropertyGroups.AdaptiveTrailingStop, false)]
    #endregion
    public class Strat_VWAPPB : StratBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private Logger _loggerTP = null;
        private bool _fullyInitialized = false;
        private Logger LoggerTP
        {
            get
            {
                if (_loggerTP == null || _fullyInitialized == false)
                {
                    (_loggerTP, _fullyInitialized) = CreateLoggerWithBaseProps(_logger);
                }
                return _loggerTP;
            }
        }

        [Browsable(false)]
        public override String StrategyVersion { get { return "1.0.0"; } }

        #region GeneralParameters[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Range(.25, 4.0)]
        [Display(   Name = "EquityRiskPct",
                    Description = "Percentage of account to risk per trade",
                    Order = 1,
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public double GEN_EquityRiskPct { get; set; }
        [NinjaScriptProperty] 
        [Display(   Name = "SLTrailingMode", 
                    Description = "How to move stop loss after initial placement", 
                    Order = 2, 
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public StopLossTrailingMode GEN_SLTrailingMode { get; set; }
        [Display(   Name = "TimeZone",
                    Description = "Choose the time zone for time calculations",
                    Order = 1,
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public TimeWindowTimeZone GEN_TimeWindowTimeZone { get; set; }
        [NinjaScriptProperty]
        [Display(   Name = "AnchorTime",
                    Description = "Choose the anchor time the offsets a calculated from",
                    Order = 4,
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public string GEN_TWAnchorTime { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(   Name = "MaxTradesPerSession",
                    Description = "Maximum number of trades per session",
                    Order = 5,
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public int GEN_MaxTradesPerSession { get; set; }
        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(   Name = "LoggingMode",
                    Description = "Choose the logging mode for the strategy",
                    Order = 6,
                    GroupName = StratPropertyGroups.GeneralParameters)]
        public LoggingMode GEN_LoggingMode { get; set; }
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

        #region ScheduleBiasFilter[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Display(   Name = "#1 Offset",
                    Description = "Choose the offset from the anchor time for the first time window",
                    Order = 1,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWOffset1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#1 Duration",
                    Description = "Choose the duration for the first time window",
                    Order = 2,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWDuration1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#1 Day of Week",
                    Description = "Choose the day of the week for the first time window",
                    Order = 3,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public DADayOfWeek SBF_DOW1 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Display(Name = "#1 Trading Stance",
                    Description = "Choose the trading stance for the first time window",
                    Order = 4,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public TradingStance SBF_TradingStance1 { get; set; } = TradingStance.None;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Offset",
                    Description = "Choose the offset from the anchor time for the second time window",
                    Order = 5,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWOffset2 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Duration",
                    Description = "Choose the duration for the second time window",
                    Order = 6,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWDuration2 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Day of Week",
                    Description = "Choose the day of the week for the second time window",
                    Order = 7,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public DADayOfWeek SBF_DOW2 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Trading Stance",
                    Description = "Choose the trading stance for the second time window",
                    Order = 8,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public TradingStance SBF_TradingStance2 { get; set; } = TradingStance.None;
        [NinjaScriptProperty]
        [Display(   Name = "#3 Offset",
                    Description = "Choose the offset from the anchor time for the third time window",
                    Order = 9,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWOffset3 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#3 Duration",
                    Description = "Choose the duration for the third time window",
                    Order = 10,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public int SBF_TWDuration3 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#3 Day of Week",
                    Description = "Choose the day of the week for the third time window",
                    Order = 11,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public DADayOfWeek SBF_DOW3 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Display(   Name = "#3 Trading Stance",
                    Description = "Choose the trading stance for the third time window",
                    Order = 12,
                    GroupName = StratPropertyGroups.ScheduleBiasFilter)]
        public TradingStance SBF_TradingStance3 { get; set; } = TradingStance.None;
        #endregion

        #region ScheduleSizingFilter[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Display(   Name = "#1 Offset",
                    Description = "Choose the offset from the anchor time for the first time window",
                    Order = 1,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWOffset1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#1 Duration",
                    Description = "Choose the duration for the first time window",
                    Order = 2,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWDuration1 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#1 Day of Week",
                    Description = "Choose the day of the week for the first time window",
                    Order = 3,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public DADayOfWeek SSF_DOW1 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Range(.25, 4.0)]
        [Display(   Name = "#1 Risk Multiplier",
                    Description = "Choose the risk multiplier for the first time window",
                    Order = 4,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public double SSF_RiskMultiplier1 { get; set; } = 1.0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Offset",
                    Description = "Choose the offset from the anchor time for the second time window",
                    Order = 5,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWOffset2 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Duration",
                    Description = "Choose the duration for the second time window",
                    Order = 6,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWDuration2 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(   Name = "#2 Day of Week",
                    Description = "Choose the day of the week for the second time window",
                    Order = 7,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public DADayOfWeek SSF_DOW2 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Range(.25, 4.0)]
        [Display(   Name = "#2 Risk Multiplier",
                    Description = "Choose the risk multiplier for the second time window",
                    Order = 8,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public double SSF_RiskMultiplier2 { get; set; } = 1.0;
        [NinjaScriptProperty]
        [Display(Name = "#3 Offset",
                    Description = "Choose the offset from the anchor time for the third time window",
                    Order = 9,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWOffset3 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(Name = "#3 Duration",
                    Description = "Choose the duration for the third time window",
                    Order = 10,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public int SSF_TWDuration3 { get; set; } = 0;
        [NinjaScriptProperty]
        [Display(Name = "#3 Day of Week",
                    Description = "Choose the day of the week for the third time window",
                    Order = 11,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public DADayOfWeek SSF_DOW3 { get; set; } = DADayOfWeek.None;
        [NinjaScriptProperty]
        [Range(.25, 2.0)]
        [Display(Name = "#3 Risk Multiplier",
                    Description = "Choose the risk multiplier for the third time window",
                    Order = 12,
                    GroupName = StratPropertyGroups.ScheduleSizingFilter)]
        public double SSF_RiskMultiplier3 { get; set; } = 1.0;
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
        [Display(   Name = "Fast EMA Period",
                    Description = "Fast EMA Period - Indicators",
                    Order = 2,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryFastEMAPeriod { get; set; } = 9;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "Slow EMA Period",
                    Description = "Slow EMA Period - Indicators",
                    Order = 3,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntrySlowEMAPeriod { get; set; } = 21;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MinVWAPDistanceATR",
                    Description = "ChopFilter - MinVWAPDistanceATR",
                    Order = 4,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMinVWAPDistanceATR { get; set; } = 0.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MinVWAPSlopeATR",
                    Description = "ChopFilter - MinVWAPSlopeATR",
                    Order = 5,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMinVWAPSlopeATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MinEMASpreadATR",
                    Description = "ChopFilter - MinEMASpreadATR",
                    Order = 6,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMinEMASpreadATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MaxPullbackATR",
                    Description = "Pullback - MaxPullbackATR",
                    Order = 7,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMaxPullbackATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "PullbackLookbackBars",
                    Description = "Pullback - PullbackLookbackBars",
                    Order = 8,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryPullbackLookbackBars { get; set; } = 5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(   Name = "MaxEntryDistanceATR",
                    Description = "Entry Control - MaxEntryDistanceATR",
                    Order = 9,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryMaxEntryDistanceATR { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(   Name = "VWAPConfirmationBars",
                    Description = "Entry Control - VWAPConfirmationBars",
                    Order = 10,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryVWAPConfirmationBars { get; set; } = 5;

        [NinjaScriptProperty]
        [Range(.001, double.MaxValue)]
        [Display(   Name = "InitialStopATRBuffer",
                    Description = "Entry Control - InitialStopATRBuffer",
                    Order = 11,
                    GroupName = StratPropertyGroups.Entry)]
        public double EntryInitialStopATRBuffer { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Display(   Name = "OrderType",
                    Description = "Order behavior - OrderType",
                    Order = 12,
                    GroupName = StratPropertyGroups.Entry)]
        public EntryOrderType EntryOrderType { get; set; } = EntryOrderType.Market;

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(   Name = "EntryExpiryBars",
                    Description = "Order behavior - EntryExpiryBars",
                    Order = 13,
                    GroupName = StratPropertyGroups.Entry)]
        public int EntryExpiryBars { get; set; } = 3;
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


        #region Properties
        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.VWAPPB;
        #endregion
/*
The Standard Lifecycle Order
State.SetDefaults
When it happens: The moment the strategy is loaded into memory(e.g., when you open the Strategies window).
What to do here: Define your strategy properties, default parameters(like stop-loss ticks, period lengths), and set basic UI flags(Name, Description, Calculate).
Note: No data or chart connection exists yet.

State.Configure
When it happens: Immediately after you click "Apply" or "OK" to enable the strategy, 
  but before historical data is requested.
What to do here: This is where you configure multi-timeframe data feeds via AddDataSeries(). 
  If you chose to force visual indicators via code, this is also where AddChartIndicator() must live.

State.Active
When it happens: NinjaTrader has accepted your configuration and is preparing to bind the 
  strategy to the account and data streams.
What to do here: Rarely used in basic strategies, but useful for initializing 
  background worker threads or specific system resources.

State.DataLoaded
When it happens: NinjaTrader has successfully connected to the data feeds and built the data bars.
What to do here: Initialize your custom variables, instantiate your arrays/lists, 
  and set up your internal logic tracking.At this point, bars exist, so you can safely query things like BarsArray.

State.Historical
When it happens: The strategy enters this state right before it begins processing the past bars on your chart.
What to do here: Good for triggering specific logic that only applies to backtesting 
  or processing historical data before live data starts streaming.

State.Realtime
When it happens: The strategy has finished processing all historical data and is now receiving live, 
  real-time ticks from the market feed.
What to do here: Transition your strategy behavior if needed (e.g., sending email alerts or changing 
  log levels for live trading). OnBarUpdate() begins firing per tick/bar in tandem with this state.

State.Terminated
When it happens: The strategy is disabled by you, the workspace is closed, or the platform shuts down.
*/

        #region overrides
        protected override void OnStateChange()
        {
            if (State == State.DataLoaded)
            {   // this part needs to be executed before the base OnStateChange DataLoaded.
                OptimizationParameters_VWAPPB OptParamsVWAPPB = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_VWAPPB;
                OptParamsVWAPPB.UpdateFromStrat();
                OptimizationParameters = OptParamsVWAPPB;
            }
            // nlog gets configured in base class so
            // we shouldn't log anything until after this call.
            base.OnStateChange();

            LoggerTP.Trace($"State = {State}");

            if (State == State.SetDefaults)
            {
                Description = @"VWAP Pullback";
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
                // when we are initializing this strategy. The OptimizationParameters_VWAPPB class
                // may be used outsisde this strategy.
                NinjaTrader.Custom.Strategies.DAustin.VWAPPB.OptimizationParameters_VWAPPB OptParamsVWAPPB = GetOptimizationParameters("OP-" + stratIdentifier) as OptimizationParameters_VWAPPB;
                OptParamsVWAPPB.SetDefaultValues();
                OptParamsVWAPPB.UpdateStratParamValues();
            }
            else if (State == State.Configure)
            {
                //update our optimization parameters from the strategy properties
                NinjaTrader.Custom.Strategies.DAustin.VWAPPB.OptimizationParameters_VWAPPB OptParamsVWAPPB = GetOptimizationParameters("OP-" + stratIdentifier) as NinjaTrader.Custom.Strategies.DAustin.VWAPPB.OptimizationParameters_VWAPPB;
                OptParamsVWAPPB.UpdateFromStrat();
            }
            else if (State == State.DataLoaded)
            {
                // initialize indicators
                NinjaTrader.Custom.Strategies.DAustin.VWAPPB.OptimizationParameters_VWAPPB OptParamsVWAPPB = GetOptimizationParameters("OP-" + stratIdentifier) as NinjaTrader.Custom.Strategies.DAustin.VWAPPB.OptimizationParameters_VWAPPB;
                NinjaTrader.Custom.Strategies.DAustin.VWAPPB.Indicators_VWAPPB indicators = GetIndicators("IDC-" + stratIdentifier) as NinjaTrader.Custom.Strategies.DAustin.VWAPPB.Indicators_VWAPPB;
                indicators.OptParams = OptParamsVWAPPB;
                indicators.Initialize();

                // now we can initialize the entry conditions evaluator and trade context
                IEntryConditionsEvaluator ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier);
                ece.OrderIdPrefix = "DA" + stratIdentifier;
                ece.Reset();
                ece.Indicators = indicators;
                ece.OptParams = OptParamsVWAPPB;

                // add chart indicators for this strategy.
                // This is done here so that the indicators are only added once.
                NinjaTrader.Custom.Strategies.DAustin.VWAPPB.Indicators_VWAPPB.EntryIndicators entryIndicators = indicators.Entry;

                AddChartIndicator(entryIndicators.AnchoredVWAP);
                AddChartIndicator(entryIndicators.SlowEMA);
                AddChartIndicator(entryIndicators.FastEMA);

                // customizse the chart indicators for this strategy
                entryIndicators.FastEMA.Plots[0].Brush = System.Windows.Media.Brushes.LimeGreen;
                entryIndicators.FastEMA.Plots[0].Width = 1;

                entryIndicators.SlowEMA.Plots[0].Brush = System.Windows.Media.Brushes.OrangeRed;
                entryIndicators.SlowEMA.Plots[0].Width = 1;

                entryIndicators.AnchoredVWAP.Plots[0].Brush = System.Windows.Media.Brushes.Cyan;
                entryIndicators.AnchoredVWAP.Plots[0].Width = 2;

                // setup the trade context and add it to the trade manager
                TradeContext tc = new TradeContext(ece);
                tc.EntryConditionsEvaluator = ece;
                List<TradeState> stateList = new List<TradeState>()
                {
                    TradeState.Idle,
                    TradeState.FillPending,
                };

                StopLossTrailingMode SLTrailMode = OptParamsVWAPPB.General.SLTrailingMode;
                if (SLTrailMode == StopLossTrailingMode.TrendStructuralTrailing)
                {
                    stateList.Add(TradeState.TrailingStopTrendStructural);
                }
                else if (SLTrailMode == StopLossTrailingMode.TrailingChandelierGuard)
                {   
                    stateList.Add(TradeState.TrailingStopChandelierGuard);
                }
                else if (SLTrailMode == StopLossTrailingMode.TrailingAdaptive)
                {
                    stateList.Add(TradeState.TrailingStopAdaptive);
                }
                if (SLTrailMode == StopLossTrailingMode.BreakEven)
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
                if (OptParamsVWAPPB.Time.TimeZone != TimeWindowTimeZone.None && !String.IsNullOrEmpty(OptParamsVWAPPB.Time.FlattenTOD))
                {
                    TradeManager.FlattenTOD = new TimeConverter().ToDataTimeOfDay(
                        OptParamsVWAPPB.Time.FlattenTOD, 
                        OptParamsVWAPPB.Time.TimeZone.GetDisplayName());
                }
                TradeManager.AddTradeContext(tc);
                TradeManager.Indicators = indicators;
                TradeManager.OptParams = OptParamsVWAPPB;
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
            ECE_VWAPPB ece = GetEntryConditionsEvaluator("ECE-" + stratIdentifier) as ECE_VWAPPB;
            OptimizationParameters_VWAPPB optParamsVWAPPB = ece.OptParamsVWAPPB;
            Indicators_VWAPPB indicatorsVWAPPB = ece.IndicatorsVWAPPB;
            TimeConverter tc = new TimeConverter();
            TimeZoneInfo EastTZI = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("==Backtest complete==");
            sb.AppendFormat("Backtest date range from {0:M/d/yy} to {1:M/d/yy}", Bars.GetTime(0), Bars.GetTime(Bars.Count - 1)).AppendLine();
            optParamsVWAPPB.ToStringBuilder(sb);
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
            LoggerTP.Info(sb.ToString());
        }
        #endregion
    }
}

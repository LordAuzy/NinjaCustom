#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
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
using NinjaTrader.Custom.DAustin.Common;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
/*
 * ============================================================
 *  MNQ INTRADAY TREND FOLLOWER  — NinjaTrader 8
 * ============================================================
 *  Core Logic:
 *    Entry   : EMA Ribbon alignment (9/21/50) + ADX trend strength
 *              + price closes above/below Keltner Channel midline
 *    Filter  : Session time window (RTH only), daily trade cap,
 *              minimum ATR filter (avoid choppy conditions)
 *    Exit    : ATR-based initial stop, trailing stop via
 *              Chandelier Exit, profit target at 2× ATR
 * ============================================================
 */
namespace NinjaTrader.NinjaScript.Strategies
{
    public class Strat_TRENDCLAUDE : StratBase
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        // ── Indicators ──────────────────────────────────────────
        private EMA emaFast;
        private EMA emaMid;
        private EMA emaSlow;
        private ADX adxFilter;
        private ATR atrIndicator;
        private Bollinger bbands;       // used for squeeze detection

        // ── State tracking ──────────────────────────────────────
        private double stopPrice;
        private double targetPrice;
        private double trailStop;
        private int tradesThisSession;
        private bool sessionReset;

        #region NinjaScriptProperties

        // ── EMA Settings ───────────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "EMA Fast Period",
                 Description = "Fast EMA period (default 9)",
                 GroupName = "1. EMA Ribbon",
                 Order = 1)]
        public int EmaFastPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA Mid Period",
                 Description = "Middle EMA period (default 21)",
                 GroupName = "1. EMA Ribbon",
                 Order = 2)]
        public int EmaMidPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA Slow Period",
                 Description = "Slow EMA period (default 50)",
                 GroupName = "1. EMA Ribbon",
                 Order = 3)]
        public int EmaSlowPeriod { get; set; }

        // ── ADX / Trend Filter ─────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "ADX Period",
                 Description = "ADX smoothing period (default 14)",
                 GroupName = "2. Trend Filter",
                 Order = 1)]
        public int AdxPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(15, 50)]
        [Display(Name = "ADX Minimum Value",
                 Description = "Minimum ADX to consider market trending (default 22)",
                 GroupName = "2. Trend Filter",
                 Order = 2)]
        public int AdxMinimum { get; set; }

        // ── ATR / Risk Settings ────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "ATR Period",
                 Description = "ATR calculation period (default 14)",
                 GroupName = "3. Risk & Exits",
                 Order = 1)]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 5.0)]
        [Display(Name = "ATR Stop Multiplier",
                 Description = "Initial stop = ATR × this multiplier (default 1.5)",
                 GroupName = "3. Risk & Exits",
                 Order = 2)]
        public double AtrStopMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 8.0)]
        [Display(Name = "ATR Target Multiplier",
                 Description = "Profit target = ATR × this multiplier (default 3.0)",
                 GroupName = "3. Risk & Exits",
                 Order = 3)]
        public double AtrTargetMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 5.0)]
        [Display(Name = "Chandelier Multiplier",
                 Description = "Trailing stop = Highest High - (ATR × this) (default 2.5)",
                 GroupName = "3. Risk & Exits",
                 Order = 4)]
        public double ChandelierMultiplier { get; set; }

        // ── Session / Trade Management ─────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Session Start (HH:MM)",
                 Description = "Earliest entry time in exchange time (default 09:30)",
                 GroupName = "4. Session",
                 Order = 1)]
        public string SessionStart { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Session End (HH:MM)",
                 Description = "Latest entry time; flatten all positions after (default 15:45)",
                 GroupName = "4. Session",
                 Order = 2)]
        public string SessionEnd { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Max Trades Per Session",
                 Description = "Daily trade cap to limit over-trading (default 4)",
                 GroupName = "4. Session",
                 Order = 3)]
        public int MaxTradesPerSession { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Contracts",
                 Description = "Number of MNQ contracts per trade (default 2)",
                 GroupName = "4. Session",
                 Order = 4)]
        public int ContractSize { get; set; }

        // ── Minimum ATR Filter ─────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Min ATR (points)",
                 Description = "Skip entries if ATR is below this (avoid chop). Default 8 pts",
                 GroupName = "5. Volatility Filter",
                 Order = 1)]
        public double     MinAtrPoints { get; set; }

        [Browsable(false)]
        public string stratIdentifier { get; set; } = StratIdentifiers.TRENCLAUD;
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
                Description = "MNQ Intraday Trend Follower — EMA Ribbon + ADX + ATR Exits";
                Name = "MNQ_TrendFollower";

                // Strategy settings
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsInstantiatedOnEachOptimizationIteration = true;

                // ── Default property values ──────────────────
                // EMA Ribbon
                EmaFastPeriod = 9;
                EmaMidPeriod = 21;
                EmaSlowPeriod = 50;

                // ADX Filter
                AdxPeriod = 14;
                AdxMinimum = 29;

                // ATR / Risk
                AtrPeriod = 14;
                AtrStopMultiplier = 0.75;
                AtrTargetMultiplier = 5.0;
                ChandelierMultiplier = 2.5;

                // Session
                SessionStart = "09:30";
                SessionEnd = "15:45";
                MaxTradesPerSession = 4;
                ContractSize = 2;

                // Volatility filter
                MinAtrPoints = 8.0;
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                emaFast = EMA(EmaFastPeriod);
                emaMid = EMA(EmaMidPeriod);
                emaSlow = EMA(EmaSlowPeriod);
                adxFilter = ADX(AdxPeriod);
                atrIndicator = ATR(AtrPeriod);
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

        protected override void OnBarUpdate()
        {
            // Need enough bars for all indicators
            if (CurrentBar < EmaSlowPeriod + 5)
                return;

            // ── Parse session times ──────────────────────────────
            TimeSpan tsStart = ParseTime(SessionStart);
            TimeSpan tsEnd = ParseTime(SessionEnd);
            TimeSpan now = Time[0].TimeOfDay;

            // ── Daily trade counter reset ────────────────────────
            if (!sessionReset && now < tsStart)
            {
                tradesThisSession = 0;
                sessionReset = true;
            }
            if (now >= tsStart)
                sessionReset = false;

            // ── Flatten all positions at session end ─────────────
            if (now >= tsEnd)
            {
                if (Position.MarketPosition != MarketPosition.Flat)
                    ExitLong("EOD_Exit", "");
                ExitShort("EOD_Exit", "");
                return;
            }

            // ── Only trade within session window ─────────────────
            if (now < tsStart || now >= tsEnd)
                return;

            // ── Trade cap check ──────────────────────────────────
            if (tradesThisSession >= MaxTradesPerSession)
                return;

            // ─────────────────────────────────────────────────────
            //  INDICATOR VALUES
            // ─────────────────────────────────────────────────────
            double fast = emaFast[0];
            double mid = emaMid[0];
            double slow = emaSlow[0];
            double adxVal = adxFilter[0];
            double atrVal = atrIndicator[0];
            double closeVal = Close[0];

            // ── Volatility filter ────────────────────────────────
            if (atrVal < MinAtrPoints)
                return;

            // ── ADX trend strength filter ────────────────────────
            bool isTrending = adxVal >= AdxMinimum;

            // ── EMA Ribbon alignment ─────────────────────────────
            bool bullishRibbon = fast > mid && mid > slow;
            bool bearishRibbon = fast < mid && mid < slow;

            // ── Price position relative to slow EMA ──────────────
            bool priceAboveSlow = closeVal > slow;
            bool priceBelowSlow = closeVal < slow;

            // ── Momentum confirmation: bar closes above fast EMA
            //    AND prior bar closed below (breakout candle) ──────
            bool bullBreakout = closeVal > fast && Close[1] <= emaFast[1];
            bool bearBreakout = closeVal < fast && Close[1] >= emaFast[1];

            // ─────────────────────────────────────────────────────
            //  ENTRY LOGIC
            // ─────────────────────────────────────────────────────
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                // LONG ENTRY
                if (isTrending && bullishRibbon && priceAboveSlow && bullBreakout)
                {
                    stopPrice = closeVal - (atrVal * AtrStopMultiplier);
                    targetPrice = closeVal + (atrVal * AtrTargetMultiplier);
                    trailStop = stopPrice;

                    EnterLong(ContractSize, "Long_Entry");
                    tradesThisSession++;
                }
                // SHORT ENTRY
                else if (isTrending && bearishRibbon && priceBelowSlow && bearBreakout)
                {
                    stopPrice = closeVal + (atrVal * AtrStopMultiplier);
                    targetPrice = closeVal - (atrVal * AtrTargetMultiplier);
                    trailStop = stopPrice;

                    EnterShort(ContractSize, "Short_Entry");
                    tradesThisSession++;
                }
            }

            // ─────────────────────────────────────────────────────
            //  EXIT LOGIC — Active Long Position
            // ─────────────────────────────────────────────────────
            if (Position.MarketPosition == MarketPosition.Long)
            {
                // Update Chandelier trailing stop
                double highestHigh = MAX(High, AtrPeriod)[0];
                double chandelier = highestHigh - (atrVal * ChandelierMultiplier);
                if (chandelier > trailStop)
                    trailStop = chandelier;

                // Profit target
                if (closeVal >= targetPrice)
                {
                    ExitLong("TP_Long", "Long_Entry");
                    return;
                }

                // Trailing stop (Chandelier)
                if (closeVal <= trailStop)
                {
                    ExitLong("Trail_Long", "Long_Entry");
                    return;
                }

                // Hard stop (initial ATR stop — emergency)
                if (closeVal <= stopPrice)
                {
                    ExitLong("Stop_Long", "Long_Entry");
                    return;
                }

                // Trend reversal exit: ribbon flips bearish
                if (bearishRibbon)
                {
                    ExitLong("Ribbon_Exit_Long", "Long_Entry");
                    return;
                }
            }

            // ─────────────────────────────────────────────────────
            //  EXIT LOGIC — Active Short Position
            // ─────────────────────────────────────────────────────
            if (Position.MarketPosition == MarketPosition.Short)
            {
                // Update Chandelier trailing stop (for shorts: lowest low + ATR)
                double lowestLow = MIN(Low, AtrPeriod)[0];
                double chandelier = lowestLow + (atrVal * ChandelierMultiplier);
                if (chandelier < trailStop || trailStop == 0)
                    trailStop = chandelier;

                // Profit target
                if (closeVal <= targetPrice)
                {
                    ExitShort("TP_Short", "Short_Entry");
                    return;
                }

                // Trailing stop (Chandelier)
                if (closeVal >= trailStop)
                {
                    ExitShort("Trail_Short", "Short_Entry");
                    return;
                }

                // Hard stop
                if (closeVal >= stopPrice)
                {
                    ExitShort("Stop_Short", "Short_Entry");
                    return;
                }

                // Trend reversal exit: ribbon flips bullish
                if (bullishRibbon)
                {
                    ExitShort("Ribbon_Exit_Short", "Short_Entry");
                    return;
                }
            }
        }

        // ── Helper: parse "HH:MM" string to TimeSpan ─────────────
        private TimeSpan ParseTime(string hhmm)
        {
            try
            {
                string[] parts = hhmm.Split(':');
                return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0);
            }
            catch
            {
                return new TimeSpan(9, 30, 0);
            }
        }
    }
    #endregion
}
    #endregion
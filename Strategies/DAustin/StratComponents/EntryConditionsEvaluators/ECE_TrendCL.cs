using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.DataCollectors;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.SuperDomColumns;
using NTRes.NinjaTrader.Gui.Tools.Account;
using Rules1;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static NinjaTrader.CQG.ProtoBuf.MarketDataSubscription.Types;
using static NinjaTrader.CQG.ProtoBuf.Quote.Types;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Calendars;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators
{
    [StrategyComponentId("ECE-TRENDCL")]
    public class ECE_TRENDCL : EntryConditionsEvaluatorBase
    {
        #region Properties
        public Indicators_TRENDCL IndicatorsTRENDCL { get { return Indicators as Indicators_TRENDCL; } }
        public OptimizationParameters_TRENDCL OptParamsTRENDCL { get { return OptParams as OptimizationParameters_TRENDCL; } }
        public ECE_TRENDCL_DataCollector DataCollector { get; private set; } = new ECE_TRENDCL_DataCollector();

        // Break-and-retest state (reset each session)
        private bool _breakoutLongOccurred = false;
        private bool _breakoutShortOccurred = false;
        private bool _retestLongOccurred = false;
        private bool _retestShortOccurred = false;
        private DateTime _breakRetestDate = DateTime.MinValue;
        #endregion

        #region constructors
        public ECE_TRENDCL(StratBase strat)
        {
            Strategy = strat;
            Initialize();
        }
        #endregion

        #region PublicMethods
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            OptimizationParameters_TRENDCL OptParams = OptParamsTRENDCL;
            Indicators_TRENDCL Indicators = IndicatorsTRENDCL;

            if (FOMCCalendar.IsFOMCDay(Strategy.Time[0]))
            {
                return null;
            }

            if (Indicators.EntryTimeWindows != null && !Indicators.EntryTimeWindows.IsInTimeWindow())
            {   // not in an entry time window
                return null;
            }

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            EMA emaFast = Indicators.Entry.FastEMA;
            double fast = emaFast[0];
            double mid = Indicators.Entry.MidEMA[0];
            double slow = Indicators.Entry.SlowEMA[0];
            double adxVal = Indicators.Entry.ADXFilter[0];
            double atrVal = Indicators.Entry.ATR[0];
            double closeVal = Strategy.Close[0];

            // ── Volatility filter ────────────────────────────────
            if (atrVal < OptParams.Entry.MinAtrPoints)
                return null;

            // ── ADX trend strength filter ────────────────────────
            bool isTrending = adxVal >= OptParams.Entry.AdxMinimum;
            // ── EMA Ribbon alignment ─────────────────────────────
            bool bullishRibbon = fast > mid && mid > slow;
            bool bearishRibbon = fast < mid && mid < slow;

            // ── Price position relative to slow EMA ──────────────
            bool priceAboveSlow = closeVal > slow;
            bool priceBelowSlow = closeVal < slow;

            // ── Momentum confirmation: bar closes above fast EMA
            //    AND prior bar closed below (breakout candle) ──────
            bool bullBreakout = closeVal > fast && Strategy.Close[1] <= emaFast[1];
            bool bearBreakout = closeVal < fast && Strategy.Close[1] >= emaFast[1];

            if (isTrending)
            {
                double stopDist = atrVal * OptParams.Entry.AtrStopMultiplier;
                double TPDist = atrVal * OptParams.Entry.AtrTargetMultiplier;
                double initialStop = 0;
                double initialTP = 0;

                // LONG ENTRY
                if (bullishRibbon && priceAboveSlow && bullBreakout)
                {
                    initialStop = Strategy.Close[0] - stopDist;
                    if (TPDist != 0)
                    {
                        initialTP = Strategy.Close[0] + TPDist;
                    }

                    if (OptParams.Entry.OrderType == EntryOrderType.StopMarket)
                    {
                        double entryPrice = Strategy.High[0] + Strategy.TickSize;

                        // Ensure valid stop placement
                        double bid = Strategy.GetCurrentBid();
                        if (entryPrice <= bid)
                        {
                            entryPrice = bid + Strategy.TickSize;
                        }
                        initialStop = entryPrice - stopDist;
                        orderTicket = CreateOrderTicket(
                            orderType: DAOrderType.LongStopMarket, 
                            initialStop: initialStop, 
                            initialTP: initialTP,
                            entryPrice: entryPrice, 
                            orderExpiryBars: OptParams.Entry.OrderExpiryBars);
                    }
                    else
                    {
                        orderTicket = CreateOrderTicket(DAOrderType.Long, initialStop, initialTP);
                    }
                }
                // SHORT ENTRY
                else if (bearishRibbon && priceBelowSlow && bearBreakout)
                {
                    initialStop = Strategy.Close[0] + stopDist;
                    if (TPDist != 0)
                    {
                        initialTP = Strategy.Close[0] - TPDist;
                    }

                    if (OptParams.Entry.OrderType == EntryOrderType.StopMarket)
                    {
                        double entryPrice = Strategy.Low[0] - Strategy.TickSize;

                        // Ensure valid stop placement
                        double ask = Strategy.GetCurrentAsk();
                        if (entryPrice >= ask)
                        {
                            entryPrice = ask - Strategy.TickSize;
                        }
                        initialStop = entryPrice + stopDist;
                        orderTicket = CreateOrderTicket(
                            orderType: DAOrderType.ShortStopMarket, 
                            initialStop: initialStop, 
                            initialTP: initialTP,
                            entryPrice: entryPrice, 
                            orderExpiryBars: OptParams.Entry.OrderExpiryBars);
                    }
                    else
                    {
                        orderTicket = CreateOrderTicket(DAOrderType.Short, initialStop, initialTP);
                    }
                }
            }

            if (orderTicket != null)
            {
                orderTicket.AllowedRiskPercentOfAccount = OptParams.EquityRiskPct;
            }
            return orderTicket;
        }

        public void Reset()
        {
            Initialize();
        }

        public void Initialize()
        {
            _breakoutLongOccurred = false;
            _breakoutShortOccurred = false;
            _retestLongOccurred = false;
            _retestShortOccurred = false;
            _breakRetestDate = DateTime.MinValue;
        }
        #endregion

        #region VirtualMethods
        #endregion
    }
}

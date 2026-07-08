using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.SuperDomColumns;
using NTRes.NinjaTrader.Gui.Tools.Account;
using Rules1;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross
{
    [StrategyComponentId("ECE-PriceTouch")]
    public class EntryConditionsEvaluatorPriceTouch : IEntryConditionsEvaluator
    {
        #region Properties
        public string OrderIdPrefix { get; set; } = "DAGeneric";
        public StratBase Strategy { get; set; }
        public IIndicators Indicators { get; set; }
        public IIndicators IndicatorsV2 { get; set; }
        public IOptimizationParameters OptParams { get; set; }
        public TimeWindows EntryTimeWindows { get; set; } = null;
        public double ATRSLMultiplier { get; set; } = 1.5;
        public int ADXFilterValue { get; set; } = 25;
        public int AllowedRiskPercentOfAccount { get; set; } = 1;  //defaulting to 1%
        #endregion

        #region constructors
        public EntryConditionsEvaluatorPriceTouch(StratBase strat)
        {
            Strategy = strat;
            OrderIdPrefix = "PHMom";
            EntryTimeWindows = new TimeWindows(strat, "9:30am", "Eastern Standard Time");
            EntryTimeWindows.AddTimeBlock(      // 9:35am-11:30am
                anchorOffsetStart: new TimeSpan(0, minutes: 5, 0),
                anchorOffsetEnd: new TimeSpan(hours: 2, 0, 0));
            EntryTimeWindows.AddTimeBlock(      // 2:30pm-3:45pm
                anchorOffsetStart: new TimeSpan(hours: 5, minutes: 0, 0),
                anchorOffsetEnd: new TimeSpan(hours: 6, minutes: 15, 0));
        }
        #endregion

        #region PublicMethods
        public OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            // Now do our filtering
            if (EntryTimeWindows.IsInDefinedTimeBlock())
            {
                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                if (TriggeredPriceTouch(orderTicket))
                {
                    //orderTicket.CompleteInputParams();
                }
            }

            if (orderTicket != null && orderTicket.Type == DAOrderType.None)
            {
                orderTicket = null;
            }
 
            return orderTicket;
        }

        public void Reset()
        {
            // no state to reset in this implementation, but method is required by interface
        }

        public void SessionReset()
        {
            // no state to reset in this implementation, but method is required by interface
        }
        #endregion

        #region VirtualMethods
        public virtual bool TriggeredPriceTouch(OrderTicket orderTicket)
        {
            return false;
            //int barsAgoCrossed  = BarsAgo921Crossed(20);
            //double VWAP         = Indicators.NYSessionAnchoredVWAP.Value;
            //double ema9         = Indicators.FastEMA[0];
            //double ema21        = Indicators.SlowEMA[0];
            //double RSI          = Strategy.RSI(14, 3)[0];
            //double ATR          = Indicators.ATR[0];
            //double adx          = Indicators.DM[0];
            //double diPlus       = Indicators.DM.DiPlus[0];
            //double diMinus      = Indicators.DM.DiMinus[0];
            //double BODY_THRESHOLD = 0.5;
            //double MIN_ATR      = 8 * Strategy.TickSize;    // instrument-relative, not hardcoded points
            //int    bufferTicks  = 2;

            //// Regime filter: skip all entries when market is choppy
            //if (adx < ADXFilterValue)
            //{
            //    return false;
            //}

            //// Trend Slope check - MAs must be fanning/trending, not flat
            //bool isEma9SlopingUp   = ema9 > Indicators.FastEMA[1];
            //bool isEma21SlopingUp  = ema21 > Indicators.SlowEMA[1];
            //bool isEma9SlopingDown = ema9 < Indicators.FastEMA[1];
            //bool isEma21SlopingDown= ema21 < Indicators.SlowEMA[1];

            //// long entry logic
            //if (Strategy.Close[0] > VWAP && ema9 > ema21 && diPlus > diMinus && isEma9SlopingUp && isEma21SlopingUp)
            //{   
            //    // cross freshness: 3 to 12 bars out (gave a little more room)
            //    if (barsAgoCrossed >= 3 && barsAgoCrossed <= 15)
            //    {
            //        // Valid pullback: prior bar touched/dipped below 9 EMA, but closed ABOVE 21 EMA (trend intact)
            //        bool validPullback = Strategy.Low[1] <= Indicators.FastEMA[1] && Strategy.Close[1] > Indicators.SlowEMA[1];

            //        if (validPullback)
            //        {
            //            // Strong trigger: current bar is green, closes above 9 EMA
            //            bool strongTrigger = Strategy.Close[0] > ema9 && Strategy.Close[0] > Strategy.Open[0];
                        
            //            // We must break the previous body AND push a new high, but don't strictly have to close above the highest wick
            //            bool clearedPriorBody = Strategy.Close[0] > Math.Max(Strategy.Open[1], Strategy.Close[1]);
            //            bool pushedHigherHigh = Strategy.High[0] > Strategy.High[1];

            //            if (strongTrigger && clearedPriorBody && pushedHigherHigh)
            //            {
            //                double candleBody       = Math.Abs(Strategy.Close[0] - Strategy.Open[0]);
            //                double candleTotalRange = Strategy.High[0] - Strategy.Low[0];
                            
            //                // Prevent "rejection wicks": Ensure the candle actually closed near its top (top 35%)
            //                bool strongCloseQuality = candleTotalRange > 0 && ((Strategy.Close[0] - Strategy.Low[0]) / candleTotalRange) >= 0.65;

            //                if (RSI > 50 && candleBody > (candleTotalRange * BODY_THRESHOLD) && ATR > MIN_ATR && strongCloseQuality)
            //                {
            //                    orderTicket.Type = DAOrderType.Long;
            //                    double stop_loss        = Math.Max(Strategy.Low[0] - (bufferTicks * Strategy.TickSize), Strategy.Close[0] - (1.5 * ATR));
            //                    double stop_loss_offset = Strategy.Close[0] - stop_loss;
            //                    orderTicket.Risk        = PriceOffset.FromPoints(stop_loss_offset, Strategy);
            //                }
            //            }
            //        }
            //    }
            //}
            //// short entry logic
            //else if (Strategy.Close[0] < VWAP && ema9 < ema21 && diMinus > diPlus && isEma9SlopingDown && isEma21SlopingDown)
            //{   
            //    if (barsAgoCrossed <= -3 && barsAgoCrossed >= -15) 
            //    {
            //        // Valid pullback: prior bar touched/popped above 9 EMA, but closed BELOW 21 EMA (trend intact)
            //        bool validPullback = Strategy.High[1] >= Indicators.FastEMA[1] && Strategy.Close[1] < Indicators.SlowEMA[1];

            //        if (validPullback)
            //        {
            //            // Strong trigger: current bar is red, closes below 9 EMA
            //            bool strongTrigger = Strategy.Close[0] < ema9 && Strategy.Close[0] < Strategy.Open[0];
                        
            //            // We must break the previous body AND push a new low, but don't strictly have to close below the lowest wick
            //            bool clearedPriorBody = Strategy.Close[0] < Math.Min(Strategy.Open[1], Strategy.Close[1]);
            //            bool pushedLowerLow   = Strategy.Low[0] < Strategy.Low[1];

            //            if (strongTrigger && clearedPriorBody && pushedLowerLow)
            //            {
            //                double candleBody       = Math.Abs(Strategy.Close[0] - Strategy.Open[0]);
            //                double candleTotalRange = Strategy.High[0] - Strategy.Low[0];

            //                // Prevent "rejection wicks": Ensure the candle actually closed near its bottom (bottom 35%)
            //                bool strongCloseQuality = candleTotalRange > 0 && ((Strategy.High[0] - Strategy.Close[0]) / candleTotalRange) >= 0.65;

            //                if (RSI < 50 && candleBody > (candleTotalRange * BODY_THRESHOLD) && ATR > MIN_ATR && strongCloseQuality)
            //                {
            //                    orderTicket.Type = DAOrderType.Short;
            //                     double stop_loss        = Math.Min(Strategy.High[0] + (bufferTicks * Strategy.TickSize), Strategy.Close[0] + (1.5 * ATR));
            //                    double stop_loss_offset = stop_loss - Strategy.Close[0];
            //                    orderTicket.Risk        = PriceOffset.FromPoints(stop_loss_offset, Strategy);
            //                }
            //            }
            //        }
            //    }
            //}

            //return orderTicket.Type != DAOrderType.None;
        }

        // Returns how many bars ago the 9 EMA crossed the 21 EMA,
        // or -1 if no cross was found within the lookback window.
        // A positive result means a bullish cross (9 crossed above 21).
        // A negative result means a bearish cross (9 crossed below 21).
        private int BarsAgo921Crossed(int maxLookback)
        {
            if (Strategy.CurrentBar < 0) //Indicators.SlowPeriod)
            {
                return 0;
            }

            for (int i = 0; i <= maxLookback; i++)
            {
                double fastCurrent = Indicators.GetFastEMA[i];
                double slowCurrent = Indicators.GetSlowEMA[i];
                double fastPrevious = Indicators.GetFastEMA[i + 1];
                double slowPrevious = Indicators.GetSlowEMA[i + 1];

                // Bullish cross: fast was below slow, now above
                if (fastPrevious <= slowPrevious && fastCurrent > slowCurrent)
                {
                    return i + 1;   // positive = bullish cross, value = bars ago
                }

                // Bearish cross: fast was above slow, now below
                if (fastPrevious >= slowPrevious && fastCurrent < slowCurrent)
                {
                    return -(i + 1); // negative = bearish cross, value = bars ago
                }
            }

            return 0; // no cross found within lookback window
        }
        #endregion
    }
}

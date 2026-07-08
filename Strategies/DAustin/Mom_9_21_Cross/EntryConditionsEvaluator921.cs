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
    [StrategyComponentId("ECE-921")]
    public class EntryConditionsEvaluator921 // : IEntryConditionsEvaluator
    {
        public class cStats
        {
            public int EMACrossMomentumTriggerCount { get; set; } = 0;
            public int VWAPInvalidateCount { get; set; } = 0;
            public int ADXValueInvalidateCount { get; set; } = 0;
            public int DiMinusPlusInvalidateCount { get; set; } = 0;
            public int VolInvalidateCount { get; set; } = 0;
        }

        #region Properties
        public string OrderIdPrefix { get; set; } = "DAGeneric";
        public StratBase Strategy { get; set; }
        public StratIndicatorsBase Indicators { get; set; }
        public IIndicators IndicatorsV2 { get; set; }
        public IOptimizationParameters OptParams { get; set; }
        public TimeWindows EntryTimeWindows { get; set; } = null;
        public double ATRSLMultiplier { get; set; } = 1.5;
        public int ADXFilterValue { get; set; } = 25;
        public int AllowedRiskPercentOfAccount { get; set; } = 1;  //defaulting to 1%
        public cStats Stats { get; set; } = new cStats();
        #endregion

        #region constructors
        public EntryConditionsEvaluator921(StratBase strat)
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
        public OrderTicket Evaluate()
        {

            OrderTicket order = new OrderTicket(Strategy, OrderIdPrefix);

            // Now do our filtering
            if (    EntryTimeWindows.IsInDefinedTimeBlock() &&
                    Triggered921(order) &&
                    VWAPConfirm(order) &&
                    VolConfirm(order)  )
            {   // passed all the filters. Now set the rest 
                // of the orderInputParams
                CompleteOrderInputParams(order);
            }

            if (order.Type == DAOrderType.None)
            {
                order = null;
            }
 
            return order;
        }

        public void Reset()
        {
            // no state to reset for this ECE, but if there were, this is where we'd do it
        }

        #endregion

        #region VirtualMethods
        public virtual bool VWAPConfirm(OrderTicket ot)
        {
            //For our setup:
            //Get direction from VWAP
            //  price > VWAP go long
            //  price < VWAP go short
            DAOrderType oType = DirectionFromVWAP();

            if (ot.Type == DAOrderType.None)
            {   // if we don't have a direction yet, set it from VWAP
                ot.Type = oType;
            }
            else if (oType != ot.Type)
            {   // if we have a direction but it doesn't align with VWAP then no order
                Stats.VWAPInvalidateCount++;
                ot.Type = DAOrderType.None;
            }

            if (ot.Type != DAOrderType.None)
            {   // use momentum indicators to verify
                // it aligns with the ordertype from VWAP
                DMConfirm(ot);
            }
            return ot.Type != DAOrderType.None;
        }

        //
        //The trigger answers: "When exactly do I place the order?"
        //
        //The specific, actionable event or price movement that tells you to actually
        //enter the trade("pull the trigger").
        //Examples: A candlestick closing above the high of a signal bar,
        //a breakout above resistance with volume, a moving average crossover,
        //or price breaking a recent swing high/low.
        //
        public virtual bool Triggered921(OrderTicket order)
        {
            // our trigger is the 9-21 EMA crossover
            EMACrossMomentumConfirm(order);
            if (order.Type != DAOrderType.None)
            {
                Stats.EMACrossMomentumTriggerCount++;
            }
            return order.Type != DAOrderType.None;
        }

        public virtual bool Triggered921Momentum(OrderTicket order)
        {
            // This is a hybrid trigger and confirmation. We want the 9-21 EMA cross
            // to trigger the order but we also want to wait for a pullback and
            // a momentum confirmation in the direction of the order before
            // we actually place the order.
            //
            // Logic (well-documented EMA pullback-momentum entry):
            //
            // Phase 1 — Cross: The 9 EMA must have crossed the 21 EMA within
            //           the last N bars, establishing trend direction.
            //
            // Phase 2 — Pullback: After the cross, price must have pulled back
            //           toward (but not through) the 9 EMA, indicating the 
            //           initial momentum spike has cooled.
            //
            // Phase 3 — Momentum Push: The current bar must now be closing back
            //           in the direction of the cross (away from the 9 EMA),
            //           confirming the pullback is over and momentum has resumed.

            if (Strategy.CurrentBar < Indicators.SlowPeriod)
            {
                order.Type = DAOrderType.None;
                return false;
            }

            int crossLookback = 5;  // bars to look back for the initial EMA cross

            if (order.Type == DAOrderType.Long)
            {
                // Phase 1: 9 EMA crossed above 21 EMA within the lookback window
                bool crossDetected = Strategy.CrossAbove(Indicators.FastEMA, Indicators.SlowEMA, crossLookback);

                if (!crossDetected)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }

                // Phase 2: Pullback — at least one of the last 3 bars touched or
                //          dipped near the 9 EMA (low <= FastEMA) without closing
                //          below the 21 EMA (which would negate the cross)
                bool pullbackDetected = false;
                for (int i = 1; i <= 3; i++)
                {
                    if (Strategy.Low[i] <= Indicators.FastEMA[i] &&
                        Strategy.Close[i] >= Indicators.SlowEMA[i])
                    {
                        pullbackDetected = true;
                        break;
                    }
                }

                if (!pullbackDetected)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }

                // Phase 3: Momentum push — current bar closes above the 9 EMA
                //          and the 9 EMA is still above the 21 EMA (trend intact)
                bool momentumPush = Strategy.Close[0] > Indicators.FastEMA[0] &&
                                    Indicators.FastEMA[0] > Indicators.SlowEMA[0];

                if (!momentumPush)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }
            }
            else if (order.Type == DAOrderType.Short)
            {
                // Phase 1: 9 EMA crossed below 21 EMA within the lookback window
                bool crossDetected = Strategy.CrossBelow(Indicators.FastEMA, Indicators.SlowEMA, crossLookback);

                if (!crossDetected)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }

                // Phase 2: Pullback — at least one of the last 3 bars touched or
                //          bounced near the 9 EMA (high >= FastEMA) without closing
                //          above the 21 EMA (which would negate the cross)
                bool pullbackDetected = false;
                for (int i = 1; i <= 3; i++)
                {
                    if (Strategy.High[i] >= Indicators.FastEMA[i] &&
                        Strategy.Close[i] <= Indicators.SlowEMA[i])
                    {
                        pullbackDetected = true;
                        break;
                    }
                }

                if (!pullbackDetected)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }

                // Phase 3: Momentum push — current bar closes below the 9 EMA
                //          and the 9 EMA is still below the 21 EMA (trend intact)
                bool momentumPush = Strategy.Close[0] < Indicators.FastEMA[0] &&
                                    Indicators.FastEMA[0] < Indicators.SlowEMA[0];

                if (!momentumPush)
                {
                    order.Type = DAOrderType.None;
                    return false;
                }
            }

            if (order.Type != DAOrderType.None)
            {
                Stats.EMACrossMomentumTriggerCount++;
            }

            return order.Type != DAOrderType.None;
        }

        //
        //The Confirmation answers: Is the setup and/or trigger legitimate.
        //
        //The Confirmation is the final layer of evidence that reduces risk of
        //whipsaws or fakeouts. Filtering out false signals or add conviction
        //before/after entry. Confirm the move is real.
        //Examples: Volume spike supporting the move, a higher timeframe alignment,
        //price retesting a level without breaking it, multiple indicators agreeing,
        //
        public virtual bool VolConfirm(OrderTicket order)
        {
            // with a volume check against the 20 period average volume
            // default: volume > 1.5 * 20 period average volume
            double VolSMA20 = Indicators.VolSMA20[0];
            double currentBarVol = Strategy.Volume[0];

            if (currentBarVol < VolSMA20 * 1.5)
            {
                order.Type = DAOrderType.None;
                Stats.VolInvalidateCount++;
            }

            return order.Type != DAOrderType.None;
        }
        #endregion

        #region PrivateMethods
        // If price is above VWAP, you only look for longs;
        // below, only shorts.
        // This prevents you from fighting the intraday trend.
        private DAOrderType DirectionFromVWAP()
        {
            DAOrderType direction = DAOrderType.Long;
            double currentPrice = Strategy.Close[0];

            if (currentPrice < Indicators.NYSessionAnchoredVWAP.Value)
            {   // we default to long and switch to short when price below VWAP
                direction = DAOrderType.Short;
            }
            return direction;
        }

        // looking for a 9-21 EMA cross
        private bool EMACrossMomentumConfirm(OrderTicket order)
        {
            int lookback = 2;

            if (Strategy.CurrentBar >= Indicators.SlowPeriod)
            {   // only if we have  enough bars
                if (order.Type == DAOrderType.None)
                {   // if we don't have a direction yet
                    if (Strategy.CrossAbove(Indicators.FastEMA, Indicators.SlowEMA, lookback))
                    {
                        order.Type = DAOrderType.Long;
                    }
                    else if (Strategy.CrossBelow(Indicators.FastEMA, Indicators.SlowEMA, lookback))
                    {
                        order.Type = DAOrderType.Short;
                    }
                }
                else
                {   // if there's already a direction the crossover needs to confirm that
                    // we need to cross in the direction of the order to confirm
                    if (order.Type == DAOrderType.Long &&
                        // when the first argument crosses above the 2nd
                        !Strategy.CrossAbove(Indicators.FastEMA, Indicators.SlowEMA, lookback))
                    {   // we're set to a long but not crossing above then no order
                        order.Type = DAOrderType.None;
                    }
                    else if (order.Type == DAOrderType.Short &&
                        // when the first argument crosses below the 2nd
                        !Strategy.CrossBelow(Indicators.FastEMA, Indicators.SlowEMA, lookback))
                    {   // we're set to a short but not crossing below then no order
                        order.Type = DAOrderType.None;
                    }
                }
            }
            return order.Type != DAOrderType.None;
        }

        // Check ADX to confirm the market trend is strong enough
        // to take a trade.
        // Check DiMinus and DiPlus to confirm the trend is in the
        // same direction as our order specifies.
        private bool DMConfirm(OrderTicket order)
        {   // DM[0] is the same as ADX[0] so if you're using 
            // DiMinus or DiPlus use DM and get the ADX value for free
            double adx = Indicators.DM[0];

            if (adx <= ADXFilterValue)
            {
                order.Type = DAOrderType.None;
                Stats.ADXValueInvalidateCount++;
            }
            else
            {
                // diPlus > diMinus, the buyers are in control.
                // diPlus < diMinus, the sellers are in control.
                double diMinus = this.Indicators.DM.DiMinus[0];
                double diPlus = this.Indicators.DM.DiPlus[0];

                if (order.Type == DAOrderType.Long &&
                    diPlus <= diMinus)
                {
                    order.Type = DAOrderType.None;
                    Stats.DiMinusPlusInvalidateCount++;
                }
                else if (order.Type == DAOrderType.Short &&
                    diPlus >= diMinus)
                {
                    order.Type = DAOrderType.None;
                    Stats.DiMinusPlusInvalidateCount++;
                }
            }
            return order.Type != DAOrderType.None;
        }

        private void CompleteOrderInputParams(OrderTicket ot)
        {
            //ay need to try multiple things here to see what works best.
            //
            //Exit (Stop)	1.2-1.8 × ATR(14) or use the the EMA 21 line.
            //Exit(Target)  A fixed 2:1 reward - to - risk ratio, or "Trailing Stop"
            //once price hits 1:1.
            //
            // wait for price to cross back below the 21 ema
            //
            //
            //Risk = | Entry − Stop | × PointValue × PositionSize
            //Where:
            // Entry − Stop  = price risk
            // PointValue = dollars per price unit
            // PositionSize = number of contracts/ shares
            //
            double cashValue = Strategy.Account.Get(AccountItem.CashValue, Currency.UsDollar);
            double allowedRiskDollars = (cashValue * AllowedRiskPercentOfAccount) / 100;
            double currentPrice = Strategy.Close[0];
            ot.Price = currentPrice;
            double RPrice = Indicators.ATR[0];
            //ot.Contracts = (int)Math.Floor(allowedRiskDollars/RPrice);

            //if (ot.Contracts == 0 || ot.Type == DAOrderType.None)
            //{
            //    ot.Type = DAOrderType.None;
            //    ot.Contracts = 0;
            //}
            //else if (ot.Type == DAOrderType.Long)
            //{
            //    //ot.TPRValue = 2;
            //}
            //else if (ot.Type == DAOrderType.Short)
            //{
            //    //ot.TPRValue = 2;
            //}
        }

        // Returns how many bars ago the 9 EMA crossed the 21 EMA,
        // or -1 if no cross was found within the lookback window.
        // A positive result means a bullish cross (9 crossed above 21).
        // A negative result means a bearish cross (9 crossed below 21).
        private int BarsAgoCrossed(int maxLookback)
        {
            if (Strategy.CurrentBar < Indicators.SlowPeriod)
            {
                return 0;
            }

            for (int i = 0; i <= maxLookback; i++)
            {
                double fastCurrent  = Indicators.FastEMA[i];
                double slowCurrent  = Indicators.SlowEMA[i];
                double fastPrevious = Indicators.FastEMA[i + 1];
                double slowPrevious = Indicators.SlowEMA[i + 1];

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

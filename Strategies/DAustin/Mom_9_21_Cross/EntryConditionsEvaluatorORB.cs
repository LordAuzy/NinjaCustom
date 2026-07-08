using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
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
using NLog;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross
{
    [StrategyComponentId("ECE-ORB-OLD")]
    public class EntryConditionsEvaluatorORB : IEntryConditionsEvaluator
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Properties
        public string OrderIdPrefix { get; set; } = "DAORB";
        public StratBase Strategy { get; set; }
        public IIndicators Indicators { get; set; }
        public TimeWindows EntryTimeWindows { get; set; }
        public IIndicators IndicatorsV2 { get; set; }
        public IOptimizationParameters OptParams { get; set; }
        public int AllowedRiskPercentOfAccount { get; set; } = 1;  //defaulting to 1%
        public int VolumeCheckAverageBarCount { get; set; } = 5;
        public static int Reject_VOL { get; set; }
        public static int Reject_VWAP { get; set; }
        public static int Reject_Width { get; set; }
        public bool BreakoutCandleVolumeCheckEnabled { get; set; } = false;
        public double VolumeCheckHowFarAboveAverage { get; set; } = 1.5;
        public bool OpeningRangeWidthCheckEnabled { get; set; } = false;
        public int OpeningRangeMaxWidth { get; set; } = 80;
        public int OpeningRangeMinWidth { get; set; } = 20;
        public bool VWAPCheckEnabled { get; set; } = false;
        public TimeWindowPriceRange OpeningRange { get; set; }
        public TimeWindowPriceRange PreMarketBig { get; set; }
        public TimeWindowPriceRange PreMarketSmall { get; set; }
        public ValueHistory OpeningRangeHistory { get; set; } = new ValueHistory(60);
        #endregion

        #region constructors
        public EntryConditionsEvaluatorORB(StratBase strat)
        {
            Strategy = strat;
            OrderIdPrefix = "DAORB";
            Initialize();
        }
        #endregion

        #region PublicMethods
        public OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;

            PreMarketBig.Update();
            PreMarketSmall.Update();
            OpeningRange.Update();

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow())
            {
                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                if (OpeningRangeBreakoutTriggered(orderTicket))
                {
//                    orderTicket.CompleteInputParams();
                }
            }

            if (orderTicket != null && orderTicket.Type == DAOrderType.None)
            {
                orderTicket = null;
            }

            return orderTicket;
        }

        public bool OpeningRangeBreakoutTriggered(OrderTicket orderTicket)
        {
            if (    
                    ClosedOutsideOfOpeningRange(orderTicket) &&
                    BreakoutCandleVolumeCheckPassed(orderTicket) &&
                    VWAPCheckPassed(orderTicket) &&
                    OpeningRangeWidthCheckPassed(orderTicket)
                //else if (PreMarketFilteringEnabled && !PreMarketFilteringCheckOK(OrderType))
                //    {
                //        OrderType = Common.DAOrderType.None;
                //    }
                //    else if (ExtermeInbalanceCheckEnabled && !ExtermeInbalanceCheckOK())
                //    {
                //        OrderType = Common.DAOrderType.None;
                //    }
                )
            {
                // calculate the SL and place it on the OrderTicket so that it
                // can be used in the position sizing calculation and order placement
                double stop_loss = 0;
                double stop_loss_offset = 0;

                if (orderTicket.Type == DAOrderType.Long)
                {
                    stop_loss = OpeningRange.RangeLow - (2 * Strategy.TickSize);
                    stop_loss_offset = Strategy.Close[0] - stop_loss;
                }
                else if (orderTicket.Type == DAOrderType.Short)
                {
                    stop_loss = OpeningRange.RangeHigh + (2 * Strategy.TickSize);
                    stop_loss_offset = stop_loss - Strategy.Close[0];
                }
                orderTicket.Risk = FlexibleValue.FromPoints(stop_loss_offset, Strategy);
                orderTicket.TPRValue = 2;  //tp risk multiple
            }

            return orderTicket.Type != DAOrderType.None;
        }

        private bool BreakAndRetestOpeningRange(OrderTicket orderTicket)
        {
            if (Strategy.CurrentBars[0] < 3 || OpeningRange == null || !OpeningRange.RangeSet)
            {
                return false;
            }

            double rangeHigh = OpeningRange.RangeHigh;
            double rangeLow  = OpeningRange.RangeLow;
            double tick      = Strategy.TickSize;

            // How far back we allow the breakout bar to be
            int lookbackBars = 8;

            double close0 = Strategy.Closes[0][0];
            double high0  = Strategy.Highs[0][0];
            double low0   = Strategy.Lows[0][0];

            orderTicket.Type = DAOrderType.None;

            // -------------------------------
            // Long: find a breakout above, then current bar is retest
            // -------------------------------
            int breakoutBarIndex = -1;

            for (int i = 1; i <= lookbackBars && i < Strategy.CurrentBars[0]; i++)
            {
                double closeI = Strategy.Closes[0][i];
                double lowI   = Strategy.Lows[0][i];

                // breakout candle must close clearly above rangeHigh
                if (closeI > rangeHigh && lowI > rangeLow)
                {
                    breakoutBarIndex = i;
                    break;
                }
            }

            if (breakoutBarIndex > 0)
            {
                // Current bar retest: price revisits OR high and closes back above
                bool retestFromAbove =
                    low0 <= rangeHigh + (2 * tick) &&   // touch into the level
                    close0 > rangeHigh;                 // confirm buyers stepped in

                if (retestFromAbove)
                {
                    orderTicket.Type = DAOrderType.Long;
                    return true;
                }
            }

            // -------------------------------
            // Short: find a breakout below, then current bar is retest
            // -------------------------------
            breakoutBarIndex = -1;

            for (int i = 1; i <= lookbackBars && i < Strategy.CurrentBars[0]; i++)
            {
                double closeI = Strategy.Closes[0][i];
                double highI  = Strategy.Highs[0][i];

                // breakout candle must close clearly below rangeLow
                if (closeI < rangeLow && highI < rangeHigh)
                {
                    breakoutBarIndex = i;
                    break;
                }
            }

            if (breakoutBarIndex > 0)
            {
                bool retestFromBelow =
                    high0 >= rangeLow - (2 * tick) &&  // touch into the level
                    close0 < rangeLow;                 // confirm sellers stepped in

                if (retestFromBelow)
                {
                    orderTicket.Type = DAOrderType.Short;
                    return true;
                }
            }

            return false;
        }

        private bool ClosedOutsideOfOpeningRange(OrderTicket orderTicket)
        {
            // if ordertype then we want to know if it matches what the order
            // is set to. If still none then we will set it based on the breakout direction
            
            bool crossedAbove = Strategy.CrossAbove(Strategy.Closes[0], OpeningRange.RangeHigh, 1);
            bool crossedBelow = Strategy.CrossBelow(Strategy.Closes[0], OpeningRange.RangeLow, 1);

            if (crossedAbove && (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Long))
            {
                orderTicket.Type = DAOrderType.Long;
            }
            else if (crossedBelow && (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Short))
            {
                orderTicket.Type = DAOrderType.Short;
            }
            else
            {
                // No valid cross happened, or it crossed in the opposite direction of the pre-set order type
                orderTicket.Type = DAOrderType.None;
            }

            return orderTicket.Type != DAOrderType.None;
        }

        private bool BreakoutCandleVolumeCheckPassed(OrderTicket orderTicket)
        {
            if (BreakoutCandleVolumeCheckEnabled)
            {
                bool ok = false;
                VolumeSeries volume = Strategy.Volume;
                double currentBarVolume = volume[0];
                double barsAverageVolume = 0;
                int idx;

                for (idx = 1; idx <= VolumeCheckAverageBarCount && idx < volume.Count; idx++)
                {
                    barsAverageVolume += volume[idx];
                }
                barsAverageVolume = barsAverageVolume / (idx - 1);

                //now that the average vol calculation is done do the check
                //initially do a 1.4 * average volumme
                if (currentBarVolume >= (barsAverageVolume * VolumeCheckHowFarAboveAverage))
                {
                    ok = true;
                }

                string logString = String.Format("BreakoutCandleVolumeCheckOK returning:{0}.  CandleVol={1}  AverageVol={2}",
                        ok, currentBarVolume, barsAverageVolume);
                logger.Info(logString);
                if (!ok)
                {
                    Reject_VOL += 1;
                    orderTicket.Type = DAOrderType.None;
                }

            }
            return orderTicket.Type != DAOrderType.None;
        }

        private bool VWAPCheckPassed(OrderTicket orderTicket)
        {
            if (VWAPCheckEnabled)
            {
                bool isValid = true;
                double VWAP = 0; // Indicators.NYSessionAnchoredVWAP.Value;
                double candleClose = Strategy.Closes[0][0];
                string logStr = String.Format("VWAPCheckValidated. IsValid={0}", isValid);

                if (orderTicket.Type == DAOrderType.Short && candleClose >= VWAP)
                {
                    logStr = String.Format("Short order invalidated. CandleClose >= VWAP.  VWAP={0}  CandleClose={1}", VWAP, candleClose);
                    isValid = false;
                }
                else if (orderTicket.Type == DAOrderType.Long && candleClose <= VWAP)
                {
                    logStr = String.Format("Long order invalidated. CandleClose <= VWAP.  VWAP={0}  CandleClose={1}", VWAP, candleClose);
                    isValid = false;
                }

                logger.Info(logStr);

                if (!isValid)
                {
                    Reject_VWAP += 1;
                    orderTicket.Type = DAOrderType.None;
                }
            }
            return orderTicket.Type != DAOrderType.None;
        }

        private bool OpeningRangeWidthCheckPassed(OrderTicket orderTicket)
        {
            if (OpeningRangeWidthCheckEnabled)
            {
                bool ok = false;
                string logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.", ok);

                if (OpeningRangeHistory != null && OpeningRangeHistory.Count >= 30)
                {
                    double averageOR = OpeningRangeHistory.Average();
                    double percentage = (OpeningRange.Range * 100) / averageOR;

                    if (percentage >= OpeningRangeMinWidth && percentage <= OpeningRangeMaxWidth)
                    {
                        ok = true;
                    }
                    logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.  Range at {1} percent of average", ok, percentage);
                }
                else
                {   // when opening range history is not ready we'll check against the ATR
                    // Minimum Threshold: The opening range should be greater than about 1.25x
                    // the ATR.Ranges smaller than this often indicate low volatility, leading
                    // to higher risk of whipsaws, false breakouts, or stagnant price action
                    // with poor follow - through.
                    //Maximum Threshold: The opening range should be no larger than 3x the ATR.Wider
                    //ranges suggest excessive volatility, which can result in overly wide stop - losses,
                    //poor risk-reward ratios, and increased chance of reversals or exhaustion.
                    //
                    double currentATR = 0; // Indicators.ATR[0];
                    double percentage = (OpeningRange.Range * 100) / currentATR;

                    if (percentage >= 125 && percentage <= 300)
                    {
                        ok = true;
                    }
                    logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.  Range at {1} percent of ATR", ok, percentage);
                }

                if (!String.IsNullOrEmpty(logString))
                {
                    logger.Info(logString);
                }

                if (!ok)
                {
                    Reject_Width += 1;
                    orderTicket.Type = DAOrderType.None;
                }
            }
            return orderTicket.Type != DAOrderType.None;
        }


        public void Reset()
        {
            Initialize();
        }

        public void SessionReset()
        {
            // new interface item for any logic that needs to be reset at
            // the start of a new session but not necessarily on every bar like in Reset()
        }

        public void Initialize()
        {
            OpeningRange = new TimeWindowPriceRange(Strategy, "9:30am", Strategy.InputParams.OpeningRangeMinutes, "Eastern Standard Time");
            OpeningRange.SetTradeWindowDurationMinutes(Strategy.InputParams.TradingMinutes);
            OpeningRange.HistoryBuffer = OpeningRangeHistory;
            PreMarketBig = new TimeWindowPriceRange(Strategy, "4:30am", 178, "Eastern Standard Time");
            PreMarketSmall = new TimeWindowPriceRange(Strategy, "7:00am", 178, "Eastern Standard Time");
            BreakoutCandleVolumeCheckEnabled = Strategy.InputParams.BreakoutCandleVolumeCheckEnabled;
            VolumeCheckHowFarAboveAverage = Strategy.InputParams.VolumeCheckHowFarAboveAverage;
            OpeningRangeWidthCheckEnabled = Strategy.InputParams.OpeningRangeWidthCheckEnabled;
            VWAPCheckEnabled = Strategy.InputParams.VWAPCheckEnabled;
            OpeningRangeMaxWidth = Strategy.InputParams.OpeningRangeMaxWidth;
            OpeningRangeMinWidth = Strategy.InputParams.OpeningRangeMinWidth;
        }
        #endregion

        #region VirtualMethods
        #endregion
    }
}

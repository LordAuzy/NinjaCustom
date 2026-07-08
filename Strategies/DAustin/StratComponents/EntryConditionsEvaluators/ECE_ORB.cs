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
using NLog;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Calendars;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators
{
    [StrategyComponentId("ECE-ORB")]
    public class ECE_ORB : EntryConditionsEvaluatorBase
    {
        public static Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Properties
        public Indicators_ORB IndicatorsORB { get { return Indicators as Indicators_ORB; } }
        public OptimizationParameters_ORB OptParamsORB { get { return OptParams as OptimizationParameters_ORB; } }
        public ECE_ORB_DataCollector DataCollector { get; private set; } = new ECE_ORB_DataCollector();

        // Break-and-retest state (reset each session)
        private bool _breakoutLongOccurred = false;
        private bool _breakoutShortOccurred = false;
        private bool _retestLongOccurred = false;
        private bool _retestShortOccurred = false;
        private DateTime _breakRetestDate = DateTime.MinValue;
        #endregion

        #region constructors
        public ECE_ORB(StratBase strat)
        {
            Strategy = strat;
            OrderIdPrefix = "DAORB";
            Initialize();
        }
        #endregion

        #region PublicMethods
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            TimeWindowPriceRange OpeningRange = IndicatorsORB.OpeningRange;

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            if (FOMCCalendar.IsFOMCDay(Strategy.Time[0]))
            {
                return null;
            }

            if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow())
            {
                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);

                bool breakoutDetected = OptParamsORB.BreakAndRetest
                    ? BreakAndRetest(orderTicket)
                    : ClosedOutsideOfOpeningRange(orderTicket);

                if (breakoutDetected && !SkipTrade() && VWAPCheckPassed(orderTicket))
                {
                    // new refacto
                    SetupOrderTicket(orderTicket);

                    // for the ORB we are defining the risk (R) as the width of the opening range.
                    orderTicket.Risk = FlexibleValue.FromPoints(OpeningRange.RangeHigh - OpeningRange.RangeLow, Strategy);
                    //if (orderTicket.Type == DAOrderType.Long)
                    //{

                    //}
                    //else if (orderTicket.Type == DAOrderType.Short)
                    //{

                    //}
                    //orderTicket.AllowedRiskPercentOfAccount = OptParamsORB.RiskAccountPercent;
                    //if (OptParamsORB.SLMode == StopLossMode.Trailing)
                    //{   // no take profit set when trailing stop loss mode is enabled. The stop loss
                    //    // will just trail at the specified distance and the trade will be manually
                    //    // exited when the price action indicates.
                    //    orderTicket.TPOffset = null;
                    //    orderTicket.TPRValue = 0;
                    //}
                    //else
                    //{
                    //    orderTicket.TPRValue = OptParamsORB.TakeProfitRiskMultiple;
                    //}

                    // old code
                    //if (orderTicket.Type == DAOrderType.Long)
                    //{
                    //    riskPoints = currentBarClose - OpeningRange.RangeLow;
                    //}
                    //else if (orderTicket.Type == DAOrderType.Short)
                    //{
                    //    riskPoints = OpeningRange.RangeHigh - currentBarClose;
                    //}
                    //orderTicket.Risk = FlexibleValue.FromPoints(riskPoints, Strategy);
                    //orderTicket.AllowedRiskPercentOfAccount = OptParamsORB.RiskAccountPercent;
                    //if (OptParamsORB.SLMode == StopLossMode.Trailing)
                    //{   // no take profit set when trailing stop loss mode is enabled. The stop loss
                    //    // will just trail at the specified distance and the trade will be manually
                    //    // exited when the price action indicates.
                    //    orderTicket.TPOffset = null;
                    //    orderTicket.TPRValue = 0;
                    //}
                    //else
                    //{
                    //    orderTicket.TPRValue = OptParamsORB.TakeProfitRiskMultiple;
                    //}


                }
                else
                {
                    orderTicket = null;
                }
            }

            if (orderTicket != null && orderTicket.Type == DAOrderType.None)
            {
                // if breakout conditions were not met the caller is
                // expecting a null back, not an order ticket with type none
                orderTicket = null;
            }

            if (orderTicket != null)
            {
                logger.Debug("OrderTicket.Type= {0}  ORHigh={1}  ORLow={2}  CurrentCandleClose={3}", 
                    orderTicket.Type, OpeningRange.RangeHigh, OpeningRange.RangeLow, Strategy.Close[0]);
            }

            return orderTicket;
        }

        private void SetupOrderTicket(OrderTicket orderTicket)
        {
            double currentBarClose = Strategy.Close[0];
            TimeWindowPriceRange OpeningRange = IndicatorsORB.OpeningRange;
            ValueHistory OpeningRangeHistory = IndicatorsORB.OpeningRangeHistory;
            StopLossParameters slParams = OptParamsORB.GetStopLossParameters();

            orderTicket.Risk = FlexibleValue.FromPoints(OpeningRange.Range, Strategy);
            orderTicket.AllowedRiskPercentOfAccount = OptParamsORB.RiskAccountPercent;

            // set takeprofit multiple or offset to null if trailing stop loss mode is
            // enabled since we won't be using a fixed take profit in that case
            if (slParams.SLTrailingMode == StopLossTrailingMode.Trailing)
            {   // no take profit set when trailing stop loss mode is enabled. The stop loss
                // will just trail at the specified distance and the trade will be manually
                // exited when the price action indicates.
                orderTicket.TPOffset = null;
                orderTicket.TPRValue = 0;
            }
            else
            {
                orderTicket.TPRValue = OptParamsORB.TakeProfitRiskMultiple;
            }

            // Determine where to set the stop loss
            double initialStopPoints = 0;
            if (slParams.SLInitialPlacement == StopLossInitialPlacement.Distance)
            {
                initialStopPoints = StopLossInitialPlacementByDistance(orderTicket, slParams);
            }
            else if (slParams.SLInitialPlacement == StopLossInitialPlacement.ATR)
            {
                initialStopPoints = StopLossInitialPlacementByATR(orderTicket, slParams);
            }

            // points and price are the same
            double SLDistanceFromEntry = orderTicket.Type == DAOrderType.Long ? currentBarClose - initialStopPoints : initialStopPoints - currentBarClose;
            orderTicket.SLOffset = new FlexibleValue(Strategy) { Points = SLDistanceFromEntry };
        }

        public double StopLossInitialPlacementByDistance(
            OrderTicket orderTicket, 
            StopLossParameters slParams)
        {
            double currentBarClose = Strategy.Close[0];
            TimeWindowPriceRange OpeningRange = IndicatorsORB.OpeningRange;
            ValueHistory OpeningRangeHistory = IndicatorsORB.OpeningRangeHistory;
            double breakoutDistance = Math.Abs(currentBarClose - (orderTicket.Type == DAOrderType.Long ? OpeningRange.RangeHigh : OpeningRange.RangeLow));
            double averageOR = OpeningRangeHistory != null ? OpeningRangeHistory.Average() : 0;
            double ORPercentageOfAverage = averageOR != 0 ? (OpeningRange.Range * 100) / averageOR : 0;
            double initialStopPoints = 0;

            // Determine initial stop based on how far we 'stretched'
            if (breakoutDistance > (OpeningRange.Range * OptParamsORB.StopLoss.MoonshotORD))
            {
                // Scenario: Moonshot. The breakout is massive. 
                // Use the low/high of the breakout candle itself.
                if (orderTicket.Type == DAOrderType.Long)
                {
                    initialStopPoints = Strategy.Low[0] - (2 * Strategy.TickSize);
                    DataCollector.SIPLongMoonshotCount++;
                }
                else if (orderTicket.Type == DAOrderType.Short)
                {
                    initialStopPoints = Strategy.High[0] + (2 * Strategy.TickSize);
                    DataCollector.SIPShortMoonshotCount++;
                }
            }
            else if ((breakoutDistance > (OpeningRange.Range * OptParamsORB.StopLoss.MidpointORD)) ||
                            ORPercentageOfAverage != 0 && ORPercentageOfAverage > OptParamsORB.StopLoss.MidpointAvgRange)
            {
                // Scenario: Strong Drive OR OR at least 125% of normal
                initialStopPoints = (OpeningRange.RangeHigh + OpeningRange.RangeLow) / 2.0;
                if (orderTicket.Type == DAOrderType.Long)
                {
                    DataCollector.SIPLongMidpointCount++;
                }
                else if (orderTicket.Type == DAOrderType.Short)
                {
                    DataCollector.SIPShortMidpointCount++;
                }
            }
            else
            {
                // Scenario: Tight Breakout. Use the 'Opposite Side'.
                if (orderTicket.Type == DAOrderType.Long)
                {
                    initialStopPoints = OpeningRange.RangeLow - (2 * Strategy.TickSize);
                    DataCollector.SIPLongOppositeBreakCount++;
                }
                else if (orderTicket.Type == DAOrderType.Short)
                {
                    initialStopPoints = OpeningRange.RangeHigh + (2 * Strategy.TickSize);
                    DataCollector.SIPShortOppositeBreakCount++;
                }
            }
            return initialStopPoints;
        }

        public double StopLossInitialPlacementByATR(
            OrderTicket orderTicket,
            StopLossParameters slParams)
        {
            double currentBarClose = Strategy.Close[0];
            double atrMultiplier = slParams.InitialPlacement_ATRMult;
            ATR atr = IndicatorsORB.GetStopLossIndicators().InitialPlacementATR;
            double initialStopPoints = 0;

            if (orderTicket.Type == DAOrderType.Long)
            {
                initialStopPoints = currentBarClose - (atrMultiplier * atr[0]);
            }
            else if (orderTicket.Type == DAOrderType.Short)
            {
                initialStopPoints = currentBarClose + (atrMultiplier * atr[0]);
            }

            return initialStopPoints;
        }

        private bool BreakAndRetest(OrderTicket orderTicket)
        {
            TimeWindowPriceRange openingRange = IndicatorsORB.OpeningRange;
            double close     = Strategy.Closes[0][0];
            double rangeHigh = openingRange.RangeHigh;
            double rangeLow  = openingRange.RangeLow;

            // Reset state once per session
            DateTime tradingDate = Strategy.Time[0].Date;
            if (tradingDate != _breakRetestDate)
            {
                _breakoutLongOccurred  = false;
                _breakoutShortOccurred = false;
                _retestLongOccurred    = false;
                _retestShortOccurred   = false;
                _breakRetestDate       = tradingDate;
            }

            // Phase 1: initial close outside the range
            if (!_breakoutLongOccurred  && close > rangeHigh) _breakoutLongOccurred  = true;
            if (!_breakoutShortOccurred && close < rangeLow)  _breakoutShortOccurred = true;

            // Phase 2: price pulls back to or inside the range after the breakout
            if (_breakoutLongOccurred  && !_retestLongOccurred  && close <= rangeHigh) _retestLongOccurred  = true;
            if (_breakoutShortOccurred && !_retestShortOccurred && close >= rangeLow)  _retestShortOccurred = true;

            // Phase 3: re-close back outside the range after the retest → entry
            if (_retestLongOccurred && close > rangeHigh &&
                (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Long))
            {
                orderTicket.Type      = DAOrderType.Long;
                DataCollector.BreakAndRetestTriggerLongCount++; 
                _retestLongOccurred   = false;   // allow a subsequent retest to trigger again
                _breakoutLongOccurred = false;
            }
            else if (_retestShortOccurred && close < rangeLow &&
                (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Short))
            {
                orderTicket.Type       = DAOrderType.Short;
                DataCollector.BreakAndRetestTriggerShortCount++;
                _retestShortOccurred   = false;
                _breakoutShortOccurred = false;
            }
            else
            {
                orderTicket.Type = DAOrderType.None;
            }

            return orderTicket.Type != DAOrderType.None;
        }

        private bool ClosedOutsideOfOpeningRange(OrderTicket orderTicket)
        {
            double prevBarClose = Strategy.Close[1];
            double currentBarClose = Strategy.Close[0];

            // if ordertype then we want to know if it matches what the order
            // is set to. If still none then we will set it based on the breakout direction
            bool crossedAbove = prevBarClose <= IndicatorsORB.OpeningRange.RangeHigh && currentBarClose > IndicatorsORB.OpeningRange.RangeHigh;
            bool crossedBelow = prevBarClose >= IndicatorsORB.OpeningRange.RangeLow && currentBarClose < IndicatorsORB.OpeningRange.RangeLow;

            if (crossedAbove && (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Long))
            {
                orderTicket.Type = DAOrderType.Long;
                DataCollector.CloseOutsideOpeningRangeTriggerLongCount++;
            }
            else if (crossedBelow && (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Short))
            {
                orderTicket.Type = DAOrderType.Short;
                DataCollector.CloseOutsideOpeningRangeTriggerShortCount++;
            }
            else
            {
                // No valid cross happened, or it crossed in the opposite direction of the pre-set order type
                orderTicket.Type = DAOrderType.None;
            }
            return orderTicket.Type != DAOrderType.None;
        }

        private bool VWAPCheckPassed(OrderTicket orderTicket)
        {
            double candleClose = Strategy.Closes[0][0];

            if (OptParamsORB.EnableVWAP)
            {
                bool isValid = true;
                double VWAP = IndicatorsORB.NYSessionAnchoredVWAP.Value;

                if (orderTicket.Type == DAOrderType.Short && candleClose >= VWAP)
                {
                    isValid = false;
                    DataCollector.VWAPShortCheckFailedCount++;
                }
                else if (orderTicket.Type == DAOrderType.Long && candleClose <= VWAP)
                {
                    isValid = false;
                    DataCollector.VWAPLongCheckFailedCount++;
                }

                if (!isValid)
                {
                    orderTicket.Type = DAOrderType.None;
                }
            }

            if (OptParamsORB.EnableVWAPSlope)
            {
                bool isValid = true;
                double VWAP = IndicatorsORB.NYSessionAnchoredVWAP.Value;
                double VWAP5Back = IndicatorsORB.NYSessionAnchoredVWAP.GetFromMostRecent(5);

                if (orderTicket.Type == DAOrderType.Short && VWAP > VWAP5Back)
                {
                    isValid = false;
                    DataCollector.VWAPShortCheckSlopeFailedCount++;
                }
                else if (orderTicket.Type == DAOrderType.Long && VWAP < VWAP5Back)
                {
                    isValid = false;
                    DataCollector.VWAPLongCheckSlopeFailedCount++;
                }

                if (!isValid)
                {
                    orderTicket.Type = DAOrderType.None;
                }
            }
            return orderTicket.Type != DAOrderType.None;
        }

        // These checks have no directionality. They only check do we do further checks for
        // durectionality or not. For example, if the opening range is very small, we may
        // want to skip trading that day regardless of breakout direction
        public bool SkipTrade()
        {
            bool skip = !BreakoutCandleVolumeCheckPassed(); 

            if (!skip)
            {
                skip = !OpeningRangeWidthCheckPassed();
            }

            return skip;
        }

        public bool BreakoutCandleVolumeCheckPassed()
        {
            bool checkPassed = true;
            if (OptParamsORB.BreakoutBarVolumeMultiplier > 0) // a value of 0 disabled the check
            {
                VolumeSeries volume = Strategy.Volume;
                double currentBarVolume = volume[0];
                double barsAverageVolume = 0;
                int idx;

                checkPassed = false;

                for (idx = 1; idx <= OptParamsORB.VolumeAverageBarCount && idx < volume.Count; idx++)
                {
                    barsAverageVolume += volume[idx];
                }
                barsAverageVolume = barsAverageVolume / (idx - 1);

                //now that the average vol calculation is done do the check
                if (currentBarVolume >= (barsAverageVolume * OptParamsORB.BreakoutBarVolumeMultiplier))
                {
                    checkPassed = true;
                }
                else
                {
                    DataCollector.BreakoutBarVolumeCheckFailedCount++;
                }
            }
            return checkPassed;
        }

        private bool OpeningRangeWidthCheckPassed()
        {
            bool checkPassed = true;
            if (OptParamsORB.ORMinWidth > 0 && OptParamsORB.ORMaxWidth > 0)
            {
                checkPassed = false;
                TimeWindowPriceRange OpeningRange = IndicatorsORB.OpeningRange;
                ValueHistory OpeningRangeHistory = IndicatorsORB.OpeningRangeHistory;

                if (OpeningRangeHistory != null && OpeningRangeHistory.Count >= 30)
                {
                    double averageOR = OpeningRangeHistory.Average();
                    double percentage = (OpeningRange.Range * 100) / averageOR;

                    if (percentage >= OptParamsORB.ORMinWidth && percentage <= OptParamsORB.ORMaxWidth)
                    {
                        checkPassed = true;
                    }
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
                    double currentATR = IndicatorsORB.ATR[0];
                    double percentage = (OpeningRange.Range * 100) / currentATR;

                    if (percentage >= 125 && percentage <= 300)
                    {
                        checkPassed = true;
                    }
                }

                if (checkPassed == false)
                {
                    DataCollector.OpeningRangeWidthCheckFailedCount++;
                }
            }
            return checkPassed;
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

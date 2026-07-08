using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using NLog;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class TradeManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        public Strategy Strategy { get; private set; }
        public List<DAOrder> Orders { get; private set; }
        public bool EnableStopLossBreakEven { get; set; }
        public bool EnableTrailingStopLoss { get; set; }
        public int MaxTradeMinutes { get; set; }
        public ManualMarketPosition ManualMarketPosition { get; set; }
        #endregion

        #region constructors
        public TradeManager(
            Strategy strat,
            List<DAOrder> orderlist) 
        { 
            Strategy = strat;
            Orders = orderlist;
        }
        #endregion

        #region PublicMethods
        // called from OnBarUpdate when marketPosition is not flat.
        public void ManageOpenOrders()
        {
            foreach (DAOrder order in Orders)
            {
                if (order.IsActiveOrder())
                {
                    bool closed = false;
                    order.OnBarUpdate();
                    if (MaxTradeMinutes > 0)
                    {
                        closed = CheckMaxTradeLength(order);
                    }

                    if (!closed)
                    {
                        if (EnableStopLossBreakEven == true && order.StopLossMoveCount == 0)
                        {
                            MoveStopLossToBreakEven(order);
                        }
                        else if (EnableTrailingStopLoss)
                        {   // if we've moved another .5R move stop loss up                          
                            TrailStopLoss(order);
                        }
                    }
                }
            }
        }

        // triggered when order state changes
        // ie . . see if an order was accepted or cancelled
        public void OrderUpdated(DAOrder order)
        {

        }

        // triggers when an order is filled
        public void OrderExecuted(DAOrder order)
        {

        }
        #endregion

        #region PrivateMethods
        private bool CheckMaxTradeLength(DAOrder order)
        {
            bool closed = false;
            TimeSpan currentTradeTime = Strategy.Time[0] - order.EntryTime;

            if (currentTradeTime.TotalMinutes > MaxTradeMinutes)
            {
//                if (order.MaxUnrealizedR < .5)
//                {
                    if (ManualMarketPosition.MarketPosition == MarketPosition.Long)
                    {
                        Strategy.ExitLong("TimeBasedExit", order.OrderId);
                        closed = true;
                    }
                    else if (ManualMarketPosition.MarketPosition == MarketPosition.Short)
                    {
                        Strategy.ExitShort("TimeBasedExit", order.OrderId);
                        closed = true;
                    }
//                }
            } 
            return closed;
        }

        private void MoveStopLossToBreakEven(DAOrder order) 
        {
            // we want to move the stop loss to break even if we are
            // half way to our profit target
            if ((order.EntryOrder.OrderState == OrderState.Filled || order.EntryOrder.OrderState == OrderState.PartFilled) &&
                order.ProfitTargetOrder.OrderState == OrderState.Working)
            {
                double oneRValue = order.OpeningRange.Range;
                double fillPrice = order.EntryOrder.AverageFillPrice;
                double limitPrice = order.ProfitTargetPrice;
                double currentPrice = Strategy.Closes[0][0];
                if (limitPrice > fillPrice)
                {   // we're long
                    if (order.StopPrice < fillPrice)
                    {   // just verify we haven't already moved it to break even
                        double halfWayThere = fillPrice + ((limitPrice - fillPrice) / 2);
                        if (currentPrice > halfWayThere)
                        {   // move stop to break even
                            double stopLossPrice = fillPrice + (Strategy.TickSize * 2);

                            // reset current - will get populated by new in OnOrderUpdate
                            Strategy.ExitLong(1, DAOrder.TRIM_NAME, order.OrderId);
                            Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, stopLossPrice, false);
                            order.StopLossMoveCount++;
                            logger.Debug("moved stoploss to break even.  SignalName=" + order.OrderId);
                        }
                    }
                }
                else if (limitPrice < fillPrice)
                {   // we're short
                    if (order.StopPrice > fillPrice)
                    {
                        double halfWayThere = fillPrice - ((fillPrice - limitPrice) / 2);
                        if (currentPrice < halfWayThere)
                        {   // move stop to break even
                            double stopLossPrice = fillPrice - (Strategy.TickSize * 2);

                            // reset current - will get populated by new in OnOrderUpdate
                            Strategy.ExitShort(1, DAOrder.TRIM_NAME, order.OrderId);
                            Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, stopLossPrice, false);
                            order.StopLossMoveCount++;
                            logger.Debug("moved stoploss to break even.  SignalName=" + order.OrderId);
                        }
                    }
                }
            }
        }

        // we only do this when current price is aboce 1.5R
        private void NewTrailStopLoss(DAOrder order)
        {
            // we want to move the stop loss to break even if we are
            // half way to our profit target
            if ((order.EntryOrder.OrderState == OrderState.Filled || order.EntryOrder.OrderState == OrderState.PartFilled) &&
                order.ProfitTargetOrder.OrderState == OrderState.Working)
            {
                double oneHalfR = order.OpeningRange.Range / 2;
                double fillPrice = order.EntryOrder.AverageFillPrice;
                double targetPrice = order.ProfitTargetPrice;
                double currentPrice = Strategy.Closes[0][0];

                if (targetPrice > fillPrice)
                {   // we're long
                    if (currentPrice >= fillPrice + (oneHalfR * 3))
                    {
                        double newStopPrice = currentPrice - oneHalfR;
                        if (order.StopPrice < newStopPrice)
                        {
                            Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, newStopPrice, false);
                            order.StopLossMoveCount++;
                            logger.Debug("trailing stoploss.  SignalName=" + order.OrderId);
                        }
                    }
                }
                else if (targetPrice < fillPrice)
                {   // we're short
                    if (currentPrice <= fillPrice - (oneHalfR * 3))
                    {
                        double newStopPrice = currentPrice + oneHalfR;
                        if (order.StopPrice > newStopPrice)
                        {
                            Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, newStopPrice, false);
                            order.StopLossMoveCount++;
                            logger.Debug("trailing stoploss.  SignalName=" + order.OrderId);
                        }
                    }
                }
            }
        }

        private void TrailStopLoss(DAOrder order)
        {
            // we want to move the stop loss to break even if we are
            // half way to our profit target
            if ((order.EntryOrder.OrderState == OrderState.Filled || order.EntryOrder.OrderState == OrderState.PartFilled) &&
                order.ProfitTargetOrder.OrderState == OrderState.Working)
            {
                double oneHalfR = order.OpeningRange.Range / 2;
                double fillPrice = order.EntryOrder.AverageFillPrice;
                double targetPrice = order.ProfitTargetPrice;
                double currentPrice = Strategy.Closes[0][0];

                if (targetPrice > fillPrice)
                {   // we're long
                    if (currentPrice >= fillPrice + (oneHalfR * 3))
                    {
                        if (order.StopPrice + oneHalfR < currentPrice)
                        {
                            double newStopPrice = order.StopPrice;
                            while (newStopPrice < currentPrice)
                            {
                                newStopPrice += oneHalfR;
                            }
                            newStopPrice -= oneHalfR; // back off so we are belw current price

                            // is the new stop price higher than the current stop
                            if (newStopPrice > order.StopPrice)
                            {   // we can move the stop
                                // we should set the new stop to the lowest of the last 3 candles
                                newStopPrice = Strategy.MIN(Strategy.Low, 4)[0];
                                if ((newStopPrice > order.StopPrice) && newStopPrice < currentPrice)
                                {
                                    Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, newStopPrice, false);
                                    order.StopLossMoveCount++;
                                    logger.Debug("trailing stoploss.  SignalName=" + order.OrderId);
                                }
                            }
                        }
                    }
                }
                else if (targetPrice < fillPrice)
                {   // we're short
                    if (currentPrice <= fillPrice - (oneHalfR * 3))
                    {
                        if (order.StopPrice - oneHalfR > currentPrice)
                        {
                            double newStopPrice = order.StopPrice;
                            while (newStopPrice > currentPrice)
                            {
                                newStopPrice -= oneHalfR;
                            }
                            newStopPrice += oneHalfR; // back off so we are above current price

                            // is the new stop price higher than the current stop
                            if (newStopPrice < order.StopPrice)
                            {   // we can move the stop
                                // we should set the new stop to the highest of the last 3 candles
                                newStopPrice = Strategy.MAX(Strategy.High, 4)[0];
                                if ((newStopPrice < order.StopPrice) && newStopPrice < currentPrice)
                                {
                                    Strategy.SetStopLoss(order.OrderId, CalculationMode.Price, newStopPrice, false);
                                    order.StopLossMoveCount++;
                                    logger.Debug("trailing stoploss.  SignalName=" + order.OrderId);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
 
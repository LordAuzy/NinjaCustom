using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class DABaseOrderV2
    {
        #region Static
        private static int _orderIdIndex = 0;
        private static  string _orderPrefix = "DABaseV2-";
        #endregion

        #region Constants
        public const string STOP_LOSS_NAME = "Stop loss";
        public const string PROFIT_TARGET_NAME = "Profit target";
        public const string TRIM_NAME = "Trim";
        #endregion

        public string OrderIdPrefix { get; set; } = string.Empty;
        public DAOrderType OrderType { get; private set; }
        public int EntryQuantity { get; set; }
        public double EntryTP { get; set; }
        public double EntrySL { get; set; }
        public string OrderId { get; set; }
        public int QuantityHeld { get; private set; } = 0;
        public int StopLossMoveCount { get; set; } = 0;
        public Strategy Strategy { get; private set; }
        public Order EntryOrder { get; private set; } = null;
        public Order StopOrder { get; set; } = null;
        public double StopPrice { get; set; } = 0;
        public Order ProfitTargetOrder { get; private set; } = null;
        public double ProfitTargetPrice { get; private set; } = 0;
        public double TakeProfitMultiplier { get; set; } = 0;
        public DateTime EntryTime { get; set; } = DateTime.MinValue;
        public double MaxUnrealizedR { get; private set; } = 0;
        public double RiskPerContract { get; private set; } = 0;
        public OrderTicket OrderInputParams { get; set; } = null;

        #region constructors
        public DABaseOrderV2(Strategy strat, OrderTicket oip)
        {
            Strategy = strat;
            OrderInputParams = oip;
        }
        #endregion

        #region PublicMethods
        public void Submit()
        {
            if (OrderInputParams.Type == DAOrderType.Short)
            {
                Strategy.EnterShort(OrderInputParams.Contracts, OrderId);
                Strategy.SetProfitTarget(OrderId, CalculationMode.Price, OrderInputParams.TPPrice);
                Strategy.SetStopLoss(OrderId, CalculationMode.Price, OrderInputParams.SLPrice, false);
                RiskPerContract = Math.Abs(Strategy.Close[0] - OrderInputParams.SLPrice);
            }
            else if (OrderInputParams.Type == DAOrderType.Long)
            {
                Strategy.EnterLong(OrderInputParams.Contracts, OrderId);
                Strategy.SetProfitTarget(OrderId, CalculationMode.Price, OrderInputParams.TPPrice);
                Strategy.SetStopLoss(OrderId, CalculationMode.Price, OrderInputParams.SLPrice, false);
                RiskPerContract = Math.Abs(Strategy.Close[0] - OrderInputParams.SLPrice);
            }
        }

        public void OnBarUpdate()
        {
            if (QuantityHeld != 0)
            {
                double unrealizedPnL;
                double entryPrice = EntryOrder.AverageFillPrice;

                if (QuantityHeld > 0)
                {
                    unrealizedPnL = Strategy.Close[0] - entryPrice;
                }
                else
                {
                    unrealizedPnL = entryPrice - Strategy.Close[0];
                }

                double unrealizedR = unrealizedPnL / RiskPerContract;
                if (unrealizedR > MaxUnrealizedR)
                    MaxUnrealizedR = unrealizedR;
            }
        }

        public void OnUpdate(
            Order order,
            double limitPrice,
            double stopPrice,
            int quantity,
            int filled,
            double averageFillPrice,
            OrderState orderState,
            DateTime time,
            ErrorCode error,
            string nativeError)
        {
            if (order.Name == OrderId)
            {
                if (EntryOrder == null)
                {
                    EntryOrder = order;
                }
            }
            else if (order.FromEntrySignal == OrderId)
            {
                if (order.Name == STOP_LOSS_NAME)
                {
                    if (order.OrderState == OrderState.Working)
                    {   // once it reaches working state we'll save it
                        StopOrder = order;
                        StopPrice = stopPrice;
                    }
                }
                else if (order.Name == PROFIT_TARGET_NAME)
                {
                    if (order.OrderState == OrderState.Working)
                    {   // once it reaches working state we'll save it
                        ProfitTargetOrder = order;
                        ProfitTargetPrice = limitPrice;
                    }
                }
                else if (order.Name == TRIM_NAME)
                {


                }
            }
        }

        // we will use this to keep track of quantity held
        public void OnExecute(
            Execution execution,
            string executionId,
            double price,
            int quantity,
            // this should be the resultant market position of the order
            MarketPosition marketPosition,
            string orderId,
            DateTime time)
        {
            if (execution.Order.Name == OrderId)
            {   // record time main order was filled
                EntryTime = time;
            }

            if (execution.Order.OrderAction == OrderAction.Buy || execution.Order.OrderAction == OrderAction.BuyToCover)
            {
                QuantityHeld += quantity;
            }
            else if (execution.Order.OrderAction == OrderAction.Sell || execution.Order.OrderAction == OrderAction.SellShort)
            {
                QuantityHeld -= quantity;
            }
        }

        public bool EntryIsCancelled()
        {
            return EntryOrder?.OrderState == OrderState.Cancelled;
        }

        public bool IsActiveOrder()
        {
            bool isActive = true;

            if (EntryOrder?.OrderState == OrderState.Cancelled)
            {
                isActive = false;
            }
            else if (StopOrder?.OrderState == OrderState.Filled)
            {
                isActive = false;
            }
            else if (ProfitTargetOrder?.OrderState == OrderState.Filled)
            {
                isActive = false;
            }

            return isActive;
        }

        #endregion

        #region PrivateMethods
        public string GenerateOrderId()
        {
            string orderId = string.Empty;

            if (String.IsNullOrEmpty(OrderIdPrefix))
            {
                orderId = _orderPrefix + "-" + _orderIdIndex.ToString("D4");
            }
            else
            {
                orderId = OrderIdPrefix + "-" + _orderIdIndex.ToString("D4");
            }

            _orderIdIndex++;

            return orderId;
        }
        #endregion
    }
}

using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class EntryConditionsEvaluatorBase : IEntryConditionsEvaluator
    {
        #region Properties
        [Browsable(false)]
        public string OrderIdPrefix { get; set; } = "DAECE";
        [Browsable(false)]
        public StratBase Strategy { get; set; }
        [Browsable(false)]
        public IIndicators Indicators { get; set; }
        private TimeWindows _entryTimeWindows = null;
        public TradeContext TradeContext { get; set; }
        [Browsable(false)]
        public TimeWindows EntryTimeWindows
        {
            get 
            {
                if (_entryTimeWindows == null)
                {
                    // if it hasn't been set return one for the core trading session for US equities
                    _entryTimeWindows = new TimeWindows(Strategy, "9:30am", "Eastern Standard Time");
                    _entryTimeWindows.AddTimeBlock(      // 9:35am-3:30pm, which is the core session for most US equities and the most liquid time to trade SPY and QQQ
                        anchorOffsetStart: new TimeSpan(0, minutes: 5, 0),
                        anchorOffsetEnd: new TimeSpan(hours: 6, 0, 0));
                }
                return _entryTimeWindows; 
            }
            set { _entryTimeWindows = value; }
        }
        [Browsable(false)]
        public IOptimizationParameters OptParams { get; set; }
        [Browsable(false)]
        public int AllowedRiskPercentOfAccount { get; set; } = 1;  //defaulting to 1%
        #endregion

        public virtual OrderTicket Evaluate(TradeContext tradeContext)
        {
            return null;
        }

        public virtual void Reset()
        {
        }

        public virtual void SessionReset()
        {
        }

        public virtual OrderTicket CreateOrderTicket(
            DAOrderType orderType,
            double initialStop,
            double initialTP = 0,
            double entryPrice = 0,
            int orderExpiryBars = 0)
        {
            OrderTicket orderTicket = null;
            double currentPrice = Strategy.Close[0];

            if (orderType == DAOrderType.LongStopMarket)
            {
                // 🚫 Skip if already triggered (avoid chasing)
                if (currentPrice >= entryPrice)
                    return null;

                // Ensure valid stop placement
                double ask = Strategy.GetCurrentAsk();
                if (entryPrice <= ask)
                    entryPrice = ask + Strategy.TickSize;

                double risk = entryPrice - initialStop;
                if (risk <= 0)
                    return null;

                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                orderTicket.Type = DAOrderType.LongStopMarket;
                orderTicket.Price = entryPrice;
                orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);
                if (initialTP > 0)
                {
                    orderTicket.TPOffset = FlexibleValue.FromPoints(initialTP - entryPrice, Strategy);
                }

                if (orderExpiryBars > 0)
                {
                    orderTicket.StopExpiryBars = orderExpiryBars;
                }
            }
            else if (orderType == DAOrderType.Long)
            {
                // Optional: disable if you want pure stop-entry testing
                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                orderTicket.Type = DAOrderType.Long;
                orderTicket.Risk = FlexibleValue.FromPoints(currentPrice - initialStop, Strategy);
                if (initialTP > 0)
                {
                    orderTicket.TPOffset = FlexibleValue.FromPoints(initialTP - currentPrice, Strategy);
                }
            }
            else if (orderType == DAOrderType.ShortStopMarket)
            {
                // 🚫 Skip if already triggered
                if (currentPrice <= entryPrice)
                    return null;

                // Ensure valid stop placement
                double bid = Strategy.GetCurrentBid();
                if (entryPrice >= bid)
                    entryPrice = bid - Strategy.TickSize;

                double risk = initialStop - entryPrice;
                if (risk <= 0)
                    return null;

                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                orderTicket.Type = DAOrderType.ShortStopMarket;
                orderTicket.Price = entryPrice;
                orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);
                if (initialTP > 0)
                {
                    orderTicket.TPOffset = FlexibleValue.FromPoints(entryPrice - initialTP, Strategy);
                }

                if (orderExpiryBars > 0)
                {
                    orderTicket.StopExpiryBars = orderExpiryBars;
                }
            }
            else if (orderType == DAOrderType.Short)
            {
                orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                orderTicket.Type = DAOrderType.Short;
                orderTicket.Risk = FlexibleValue.FromPoints(initialStop - currentPrice, Strategy);
                if (initialTP > 0)
                {
                    orderTicket.TPOffset = FlexibleValue.FromPoints(currentPrice - initialTP, Strategy);
                }
            }
            return orderTicket;
        }
    }
}

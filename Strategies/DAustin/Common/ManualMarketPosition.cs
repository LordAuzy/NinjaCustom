using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class ManualMarketPosition
    {
        #region Properties
        public Strategy Strategy { get; private set; }
        public MarketPosition MarketPosition { get; private set; } = MarketPosition.Flat;
        public int Quantity { get; private set; } = 0;
        #endregion

        #region Constructors
        public ManualMarketPosition(Strategy strat)
        {
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public void Update(
            Order order,
            int quantity)
        {
            if (order == null)
                return;

            if (order.OrderState != OrderState.Filled &&
                order.OrderState != OrderState.PartFilled)
                return;

            // Determine signed fill quantity
            int signedQty = 0;

            if (order.OrderAction == OrderAction.Buy ||
                order.OrderAction == OrderAction.BuyToCover)
                signedQty = quantity;
            else if (order.OrderAction == OrderAction.Sell ||
                     order.OrderAction == OrderAction.SellShort)
                signedQty = -quantity;

            // Update running position
            Quantity += signedQty;

            if (Quantity > 0)
                MarketPosition = MarketPosition.Long;
            else if (Quantity < 0)
                MarketPosition = MarketPosition.Short;
            else
                MarketPosition = MarketPosition.Flat;
        }
        #endregion
    }
}

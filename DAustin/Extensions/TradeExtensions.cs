using System;
using NinjaTrader.Cbi;

namespace NinjaTrader.Custom.DAustin.Extensions
{
    public static class TradeExtensions
    {
        /// <summary>
        /// True if the completed trade made money.
        /// </summary>
        public static bool IsWinner(this Trade trade)
        {
            bool isWinner = false;

            if (trade != null && trade.Entry != null && trade.Exit != null)
            {
                if (trade.Entry.MarketPosition == MarketPosition.Long)
                {
                    isWinner = trade.Exit.Price > trade.Entry.Price;
                }
                else if (trade.Entry.MarketPosition == MarketPosition.Short)
                {
                    isWinner = trade.Exit.Price < trade.Entry.Price;
                }
            }
            return isWinner;
        }

        /// <summary>
        /// True if the completed trade lost money.
        /// </summary>
        public static bool IsLoser(this Trade trade)
        {
            bool isLoser = false;

            if (trade != null && trade.Entry != null && trade.Exit != null)
            {
                if (trade.Entry.MarketPosition == MarketPosition.Long)
                {
                    isLoser = trade.Exit.Price < trade.Entry.Price;
                }
                else if (trade.Entry.MarketPosition == MarketPosition.Short)
                {
                    isLoser = trade.Exit.Price > trade.Entry.Price;
                }
            }
            return isLoser;
        }
    }
}

using NinjaTrader.Custom.DAustin.Common.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class TradeSummaryData
    {
        #region Properties
        public Cbi.Trade Trade { get; set; } = null;
        public List<TradeEvent> TradeEvents { get; set; } = null;
        public double HighestHighSinceEntry { get; set; } = 0;
        public double LowestLowSinceEntry { get; set; } = 0;

        public string TradeId { get; private set; }
        public int TotalQty { get; private set; }
        public double EntryPx { get; private set; }
        public double ExitPx { get; private set; }
        public TimeSpan EntryLatency { get; private set; }
        public TimeSpan ExitLatency { get; private set; }
        public TimeSpan TradeDuration { get; private set; }
        public double GrossPnl { get; private set; } = 0;
        public double NetPnl { get; private set; } = 0;
        public double TotalCommissions { get; private set; } = 0;

        #endregion

        public static TradeSummaryData Create(
            Cbi.Trade trade,
            List<TradeEvent> tradeEvents,
            double highestHighSinceEntry,
            double lowestLowSinceEntry)
        { 
            TradeSummaryData TSD = new TradeSummaryData() { 
                Trade = trade,
                TradeEvents = tradeEvents,
                HighestHighSinceEntry = highestHighSinceEntry,
                LowestLowSinceEntry = lowestLowSinceEntry
            };

            TSD.CalculateSummaryData();

            return TSD; 
        }

        public void CalculateSummaryData()
        {
            if (Trade == null)
                return;
            if (TradeEvents == null || TradeEvents.Count == 0)
                return;

            // 1. Core Trade Data
            TradeId = Trade.TradeNumber.ToString();
            TotalQty = Trade.Quantity;
            EntryPx = Trade.Entry.Price;
            ExitPx = Trade.Exit.Price;

            // 3. Latency Metrics (Order Submission to Execution Fill)
            EntryLatency = Trade.Entry.Time - Trade.Entry.Order.Time;
            ExitLatency = Trade.Exit.Time - Trade.Exit.Order.Time;
            TradeDuration = Trade.Exit.Time - Trade.Entry.Time;

            NetPnl = Trade.ProfitCurrency;
            TotalCommissions = Trade.Commission;
            GrossPnl = NetPnl + TotalCommissions;
        }
    }
}

using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class ExecutionLeg
    {
        public DateTime DateTime { get; set; }
        public double SignalPrice { get; set; } = 0;
        public double FillPrice { get; set; } = 0;
        public int Quantity { get; set; } = 0;
        public double Commission { get; set; }
        public string Reason { get; set; }
    }

    public class TradePerformance
    {
        public double Commission { get; set; }
        public double EntrySlippage { get; set; } = 0;
        public double ExitSlippage { get; set; } = 0;
        public double GrossProfit { get; set; }
        public double GrossProfitR { get; set; }
        public double NetProfit { get; set; }
        public TimeSpan Duration { get; set; }
        public double MAE { get; set; } // Maximum Adverse Excursion
        public double MFE { get; set; } // Maximum Favorable Excursion
    }

    public class ClosedTrade
    {
        public string StrategyVersion { get; set; }
        public string TradeId { get; set; }
        public int SessionTradeNumber { get; set; }
        public Instrument Instrument { get; set; } = null;
        public Cbi.Account Account { get; set; } = null;
         public MarketPosition Direction { get; set; }

        public ExecutionLeg Entry { get; } = new ExecutionLeg();
        public ExecutionLeg Exit { get; } = new ExecutionLeg();
        public TradePerformance Metrics { get; } = new TradePerformance();

        public double InitialRisk { get; set; }
        public double HighestHighSinceEntry { get; set; }
        public double LowestLowSinceEntry { get; set; }
    }
}

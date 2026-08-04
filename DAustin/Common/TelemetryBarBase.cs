using ActiproSoftware.Text.Tagging.Implementation;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TelemetryBarBase : ITelemetryBar
    {
        #region static members
        private static List<string> s_columnNames { get; } = 
        [
            "StrategyVersion",
            "TradeId",
            "BarsSinceEntry",
            "Time",
            "Open",
            "High",
            "Low",
            "Close",
            "Volume",
            "MarketPosition",
            "EntryPrice",
            "Quantity",
            "CurrentStop",
            "InitialRisk",
            "CurrentR",
            "OpenPnL",
            "MFE",
            "MAE"
        ]; 
        #endregion

        #region Properties
        public StratBase Strategy { get; set; }
        public IIndicators Indicators { get; set; }
        public DAOrderType OrderType { get; set; } = DAOrderType.None;
        public double EntryPrice { get; set; } = 0;
        public int Quantity { get; set; } = 0;
        public double InitialRisk { get; set; }
        public double CurrentStop { get; set; } = 0;
        public string StrategyVersion { get; set; } = "1.0";
        public string TradeId { get; set; }
        public int BarsSinceEntry { get; set; } = 0;
        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public double HighestHighSinceEntry { get; set; } = 0;
        public double LowestLowSinceEntry { get; set; } = 0;
        public double MFE { get; set; } = 0; // Maximum Favorable Excursion
        public double MAE { get; set; } = 0; // Maximum Adverse Excursion
        #endregion

        #region constructors
        public TelemetryBarBase() 
        { 

        }
        public TelemetryBarBase(StratBase strat, IIndicators indicators)
        {
            Strategy = strat;
            Indicators = indicators;
        }

        public virtual List<string> GetColumnNames()
        {
            List<string> clonedColNames = new List<string>(s_columnNames);
            return clonedColNames;
        }

        public virtual List<string> GetRowData()
        {
            string doubleStringFormatter = "F2";
            List<string> rowData = new List<string>();
            double currentR = 0;
            double unrealizedPnL = 0;

            if (IsLong())
            {
                unrealizedPnL = Close - EntryPrice;
                currentR = (InitialRisk != 0) ? (Close - EntryPrice) / InitialRisk : 0;
            }
            else if (IsShort())
            {
                unrealizedPnL = EntryPrice - Close;
                currentR = (InitialRisk != 0) ? (EntryPrice - Close) / InitialRisk : 0;
            }

            rowData.Add(StrategyVersion);
            rowData.Add(TradeId);
            rowData.Add(BarsSinceEntry.ToString());
            rowData.Add(Time.ToString());
            rowData.Add(Open.ToString(doubleStringFormatter));
            rowData.Add(High.ToString(doubleStringFormatter));
            rowData.Add(Low.ToString(doubleStringFormatter));
            rowData.Add(Close.ToString(doubleStringFormatter));
            rowData.Add(Volume.ToString("F0"));
            rowData.Add(Direction());
            rowData.Add(EntryPrice.ToString(doubleStringFormatter));
            rowData.Add(Quantity.ToString("F0"));
            rowData.Add(CurrentStop.ToString(doubleStringFormatter));
            rowData.Add(InitialRisk.ToString(doubleStringFormatter));
            rowData.Add(currentR.ToString(doubleStringFormatter));
            rowData.Add(unrealizedPnL.ToString(doubleStringFormatter));
            rowData.Add(MFE.ToString(doubleStringFormatter));
            rowData.Add(MAE.ToString(doubleStringFormatter));

            return rowData;
        }
        #endregion

        #region PublicMethods
        public virtual void CollectData()
        {
            StrategyVersion = Strategy.StrategyVersion;
            Time = Strategy.Time[0];
            Open = Strategy.Open[0];
            High = Strategy.High[0];
            Low = Strategy.Low[0];
            Close = Strategy.Close[0];
            Volume = Strategy.Volume[0];

            if (IsLong())
            {
                MFE = HighestHighSinceEntry - EntryPrice;
                MAE = LowestLowSinceEntry - EntryPrice;
            }
            else if (IsShort())
            {
                MFE = EntryPrice - LowestLowSinceEntry;
                MAE = EntryPrice - HighestHighSinceEntry;
            }
        }

        public bool IsLong()
        {
            return (OrderType == DAOrderType.LongStopMarket || OrderType == DAOrderType.Long);
        }
        public bool IsShort()
        {
            return (OrderType == DAOrderType.ShortStopMarket || OrderType == DAOrderType.Short);
        }

        public string Direction()
        {
            if (OrderType == DAOrderType.LongStopMarket || OrderType == DAOrderType.Long)
                return "Long";
            else if (OrderType == DAOrderType.ShortStopMarket || OrderType == DAOrderType.Short)
                return "Short";
            else
                return "None";
        }
        #endregion
    }
}

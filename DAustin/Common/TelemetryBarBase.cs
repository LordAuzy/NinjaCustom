using ActiproSoftware.Text.Tagging.Implementation;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TelemetryBarBase : ITelemetryBar, ICSVDataSource
    {
        #region Properties
        [XmlIgnore]
        public StratBase Strategy { get; set; }
        [XmlIgnore]
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
            List<string> clonedColNames = new List<string>(_columnNames);
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

        #region ICSVDataSource implementation
        private static readonly List<string> _columnNames = new List<string>
        {
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
        };

        // we need to return a cloned list of column names
        // before the object has been instantiated, so we
        // can't always use the instance method GetColumnNames()
        public static List<string> ColumnNameList(List<string> columns)
        {
            List<string> columnNames = columns;

            if (columnNames == null)
            {   // if list wasn't passed in, create a new list to return
                columnNames = new List<string>();
            }

            columnNames.AddRange(_columnNames);
            return columnNames;
        }

        public int dataRowIndex = 0;

        public virtual void Rewind()
        {
            dataRowIndex = 0;
        }

        public virtual List<string> NextDataRow(List<string> data)
        {
            List<string> dataRow = null;

            dataRowIndex++;
            // this data source only has one row of data,
            // so return null after the first row is returned
            if (dataRowIndex == 1)
            {
                dataRow = data;

                if (dataRow == null)
                {   // if list wasn't passed in, create a new list to return
                    dataRow = new List<string>();
                }

                string doubleStringFormatter = "F2";
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

                dataRow.Add(StrategyVersion);
                dataRow.Add(TradeId);
                dataRow.Add(BarsSinceEntry.ToString());
                dataRow.Add(Time.ToString());
                dataRow.Add(Open.ToString(doubleStringFormatter));
                dataRow.Add(High.ToString(doubleStringFormatter));
                dataRow.Add(Low.ToString(doubleStringFormatter));
                dataRow.Add(Close.ToString(doubleStringFormatter));
                dataRow.Add(Volume.ToString("F0"));
                dataRow.Add(Direction());
                dataRow.Add(EntryPrice.ToString(doubleStringFormatter));
                dataRow.Add(Quantity.ToString("F0"));
                dataRow.Add(CurrentStop.ToString(doubleStringFormatter));
                dataRow.Add(InitialRisk.ToString(doubleStringFormatter));
                dataRow.Add(currentR.ToString(doubleStringFormatter));
                dataRow.Add(unrealizedPnL.ToString(doubleStringFormatter));
                dataRow.Add(MFE.ToString(doubleStringFormatter));
                dataRow.Add(MAE.ToString(doubleStringFormatter));
            }
            return dataRow;
        }

        public virtual List<string> GetColumnNames(List<string> columns)
        {
            List<string> columnNames = columns;

            if (columnNames == null)
            {   // if list wasn't passed in, create a new list to return
                columnNames = new List<string>();
            }

            columnNames.AddRange(_columnNames);
            return columnNames;
        }
        #endregion
    }
}

using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

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

    public class ClosedTrade : ICSVDataSource
    {
        #region Constants
        const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        #endregion

        #region Properties
        public StratBase Strategy { get; private set; }
        public string StrategyVersion { get; set; }
        public string TransactionId { get; set; }
        public string SignalName { get; set; }
        public int SessionTradeNumber { get; set; }
        public Instrument Instrument { get; set; } = null;
        public Cbi.Account Account { get; set; } = null;
        public MarketPosition Direction { get; set; }

        public ExecutionLeg Entry { get; } = new ExecutionLeg();
        public ExecutionLeg Exit { get; } = new ExecutionLeg();
        public TradePerformance Metrics { get; set; } = null;

        public double InitialRisk { get; set; }
        public double HighestHighSinceEntry { get; set; }
        public double LowestLowSinceEntry { get; set; }
        #endregion

        #region Constructors
        public ClosedTrade(StratBase strategy)
        {
            Strategy = strategy;
            StrategyVersion = Strategy.StrategyVersion;
        }
        #endregion

        #region ICSVDataSource Implementation
        int dataRowIndex = 0;
        private static readonly List<string> _columnNames = new List<string>
        {
            "StrategyVersion",
            "RunId",
            "TradeId",
            "SessionDate",
            "TradeNumInSession",
            "Instrument",
            "Account",
            "Direction",
            "Qty",
            "SignalEntryPrice",
            "FillEntryPrice",
            "SignalExitPrice",
            "FillExitPrice",
            "EntryTime",
            "ExitTime",
            "ExitQty",
            "ExitReason",
            "GrossProfit",
            "GrossProfitR",
            "Commission",
            "NetProfit",
            "DurationMinutes",
            "InitialRisk",
            "HighestHigh",
            "LowestLow",
            "MAE",
            "MFE",
            "EntrySlippage",
            "ExitSlippage"
        };

        public List<string> GetColumnNames(List<string> columns = null)
        {
            List<string> columnNames = columns;

            if (columnNames == null)
            {   // if list wasn't passed in, create a new list to return
                columnNames = new List<string>();
            }

            columnNames.AddRange(_columnNames);
            return columnNames;
        }

        public List<string> NextDataRow(List<string> data)
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

                if (Metrics == null)
                {
                    Metrics = BuildTradePerformance();
                }

                if (Metrics != null)
                {   // now we are ready to populate the data row with trade information
                    dataRow.Add(StrategyVersion);
                    dataRow.Add(Strategy.Logs.RunId.ToString());
                    dataRow.Add(SignalName);
                    dataRow.Add(Entry.DateTime.ToString("yyyy-MM-dd"));
                    dataRow.Add(SessionTradeNumber.ToString());
                    dataRow.Add(Instrument?.FullName ?? "");
                    dataRow.Add(Account?.Name ?? "");
                    dataRow.Add(Direction.ToString());
                    dataRow.Add(Entry.Quantity.ToString());
                    dataRow.Add(Entry.SignalPrice.ToString("F2"));
                    dataRow.Add(Entry.FillPrice.ToString("F2"));
                    dataRow.Add(Exit.SignalPrice.ToString("F2"));
                    dataRow.Add(Exit.FillPrice.ToString("F2"));
                    dataRow.Add(Entry.DateTime.ToString(DateTimeFormat));
                    dataRow.Add(Exit.DateTime.ToString(DateTimeFormat));
                    dataRow.Add(Exit.Quantity.ToString());
                    dataRow.Add(Exit.Reason ?? "");
                    dataRow.Add(Metrics.GrossProfit.ToString("F2"));
                    dataRow.Add(Metrics.GrossProfitR.ToString("F2"));
                    dataRow.Add(Metrics.Commission.ToString("F2"));
                    dataRow.Add(Metrics.NetProfit.ToString("F2"));
                    dataRow.Add(Metrics.Duration.TotalMinutes.ToString("F2"));
                    dataRow.Add(InitialRisk.ToString("F2"));
                    dataRow.Add(HighestHighSinceEntry.ToString("F2"));
                    dataRow.Add(LowestLowSinceEntry.ToString("F2"));
                    dataRow.Add(Metrics.MAE.ToString("F2"));
                    dataRow.Add(Metrics.MFE.ToString("F2"));
                    dataRow.Add(Metrics.EntrySlippage.ToString("F4"));
                    dataRow.Add(Metrics.ExitSlippage.ToString("F4"));
                }
            }
            return dataRow;
        }

        public void Rewind()
        {
            dataRowIndex = 0;
        }
        #endregion

        #region Private Methods
        private TradePerformance BuildTradePerformance()
        {
            if (Entry == null || Exit == null)
            {
                return null;
            }

            double entryPrice = Entry.FillPrice;
            double exitPrice = Exit.FillPrice;
            int quantity = Entry.Quantity;
            double pointValue = Instrument.MasterInstrument.PointValue;
            TradePerformance tp = new TradePerformance();

            // Calculate P&L
            if (Direction == MarketPosition.Long)
            {
                tp.GrossProfit = (exitPrice - entryPrice) * quantity * pointValue;
            }
            else
            {
                tp.GrossProfit = (entryPrice - exitPrice) * quantity * pointValue;
            }

            // Calculate R multiple
            if (InitialRisk > 0)
            {
                tp.GrossProfitR = Math.Abs(exitPrice - entryPrice) / InitialRisk;
                if (tp.GrossProfit < 0)
                    tp.GrossProfitR *= -1;
            }

            // Commission
            double totalCommission = Entry.Commission + Exit.Commission;
            tp.Commission = totalCommission;
            tp.NetProfit = tp.GrossProfit - totalCommission;

            // Duration
            tp.Duration = Exit.DateTime - Entry.DateTime;

            // MAE/MFE (Always positive values representing magnitude of excursion)
            if (Direction == MarketPosition.Long)
            {
                // MAE = Maximum Adverse Excursion (largest drawdown)
                // For longs: occurs when price drops below entry
                tp.MAE = Math.Max(0, (entryPrice - LowestLowSinceEntry) * pointValue * quantity);

                // MFE = Maximum Favorable Excursion (largest unrealized gain)
                // For longs: occurs when price rises above entry
                tp.MFE = Math.Max(0, (HighestHighSinceEntry - entryPrice) * pointValue * quantity);
            }
            else
            {
                // MAE = Maximum Adverse Excursion (largest drawdown)
                // For shorts: occurs when price rises above entry
                tp.MAE = Math.Max(0, (HighestHighSinceEntry - entryPrice) * pointValue * quantity);

                // MFE = Maximum Favorable Excursion (largest unrealized gain)
                // For shorts: occurs when price drops below entry
                tp.MFE = Math.Max(0, (entryPrice - LowestLowSinceEntry) * pointValue * quantity);
            }

            // Calculate slippage (in currency units to match MAE/MFE)
            tp.EntrySlippage = Math.Abs(Entry.FillPrice - Entry.SignalPrice) * pointValue * quantity;

            tp.ExitSlippage = 0;
            if (Exit.SignalPrice != 0)
            {   // Only calculate exit slippage if we have a valid signal price to compare against
                tp.ExitSlippage = Math.Abs(Exit.FillPrice - Exit.SignalPrice) * pointValue * quantity;
            }
            return tp;
        }
        #endregion
    }
}

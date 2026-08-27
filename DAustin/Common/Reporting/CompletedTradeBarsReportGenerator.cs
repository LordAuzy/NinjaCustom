using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.DAustin.Logging;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class CompletedTradeBarsReportGenerator
    {
        #region Properties
        private StrategyLogging _logs = null;
        public StrategyLogging Logs
        {
            get
            {
                if (_logs == null)
                {
                    if (Strategy != null && Strategy.Logs != null)
                    {
                        _logs = Strategy.Logs;
                    }
                }
                return _logs;
            }
        }
        public static string TradeCSVSchemaVersion => "1.0.0";
        public StratBase Strategy { get; private set; }
        #endregion

        #region Constructors
        public CompletedTradeBarsReportGenerator(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        public void LogCompletedTradeBars(List<ITelemetryBar> completedTradeBars)
        {
            if (completedTradeBars == null || completedTradeBars.Count == 0)
            {
                Logs.Warn("No completed trade bars to log.");
                return;
            }

            DateTime simTime = Strategy.GetDataTimeForLogger();
            EnsureCSVHeaderExists(completedTradeBars[0], simTime);

            foreach (ITelemetryBar bar in completedTradeBars)
            {
                Logs.WriteTelemetryCSV(simTime, ToCSV(bar));
            }
        }
        private void CalculateTradeMetrics(ClosedTrade td)
        {
            double entryPrice = td.Entry.FillPrice;
            double exitPrice = td.Exit.FillPrice;
            int quantity = td.Entry.Quantity;
            double pointValue = td.Instrument.MasterInstrument.PointValue;
            TradePerformance tp = td.Metrics;

            // Calculate P&L
            if (td.Direction == MarketPosition.Long)
            {
                tp.GrossProfit = (exitPrice - entryPrice) * quantity * pointValue;
            }
            else
            {
                tp.GrossProfit = (entryPrice - exitPrice) * quantity * pointValue;
            }

            // Calculate R multiple
            if (td.InitialRisk > 0)
            {
                tp.GrossProfitR = Math.Abs(exitPrice - entryPrice) / td.InitialRisk;
                if (tp.GrossProfit < 0)
                    tp.GrossProfitR *= -1;
            }

            // Commission
            double totalCommission = td.Entry.Commission + td.Exit.Commission;
            tp.Commission = totalCommission;
            tp.NetProfit = tp.GrossProfit - totalCommission;

            // Duration
            tp.Duration = td.Exit.DateTime - td.Entry.DateTime;

            // MAE/MFE (Always positive values representing magnitude of excursion)
            if (td.Direction == MarketPosition.Long)
            {
                // MAE = Maximum Adverse Excursion (largest drawdown)
                // For longs: occurs when price drops below entry
                tp.MAE = Math.Max(0, (entryPrice - td.LowestLowSinceEntry) * pointValue * quantity);

                // MFE = Maximum Favorable Excursion (largest unrealized gain)
                // For longs: occurs when price rises above entry
                tp.MFE = Math.Max(0, (td.HighestHighSinceEntry - entryPrice) * pointValue * quantity);
            }
            else
            {
                // MAE = Maximum Adverse Excursion (largest drawdown)
                // For shorts: occurs when price rises above entry
                tp.MAE = Math.Max(0, (td.HighestHighSinceEntry - entryPrice) * pointValue * quantity);

                // MFE = Maximum Favorable Excursion (largest unrealized gain)
                // For shorts: occurs when price drops below entry
                tp.MFE = Math.Max(0, (entryPrice - td.LowestLowSinceEntry) * pointValue * quantity);
            }

            // Calculate slippage (in currency units to match MAE/MFE)
            tp.EntrySlippage = Math.Abs(td.Entry.FillPrice - td.Entry.SignalPrice) * pointValue * quantity;

            tp.ExitSlippage = 0;
            if (td.Exit.SignalPrice != 0)
            {   // Only calculate exit slippage if we have a valid signal price to compare against
                tp.ExitSlippage = Math.Abs(td.Exit.FillPrice - td.Exit.SignalPrice) * pointValue * quantity;
            }
        }

        public string ToCSV(ITelemetryBar tb)
        {
            List<string> list = tb.GetRowData();
            string csv = string.Join(",", list);
            return csv;
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            
            return value;
        }

        private void EnsureCSVHeaderExists(ITelemetryBar tb, DateTime simTime)
        {
            List<string> columnList = tb.GetColumnNames();
            string headerString = string.Join(",", columnList);

            Logs.EnsureTelemetryCSVHeaderExists(simTime, headerString);
        }
    }
}



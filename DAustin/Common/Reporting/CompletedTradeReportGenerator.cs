using NinjaTrader.Cbi;
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
    public class CompletedTradeReportGenerator
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private Logger _loggerTP = null;
        private bool _fullyInitialized = false;
        private Logger LoggerTP
        {
            get
            {
                if (_loggerTP == null || _fullyInitialized == false)
                {
                    (_loggerTP, _fullyInitialized) = Strategy.CreateLoggerWithBaseProps(_logger);
                }
                return _loggerTP;
            }
        }

        public static NLog.Logger _tradeCSVLogger = LogManager.GetLogger("TradeExecutionCSVLogger");
        private Logger _csvloggerTP = null;
        private bool _csvfullyInitialized = false;
        private Logger CSVLoggerTP
        {
            get
            {
                if (_csvloggerTP == null || _csvfullyInitialized == false)
                {
                    (_csvloggerTP, _csvfullyInitialized) = Strategy.CreateLoggerWithBaseProps(_tradeCSVLogger);
                }
                return _csvloggerTP;
            }
        }

        #region Properties
        public static string TradeCSVSchemaVersion => "1.0.0";
        public StratBase Strategy { get; private set; }
        #endregion

        #region Constructors
        public CompletedTradeReportGenerator(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        public void LogCompletedTrade(ClosedTrade completedTradeData)
        {
            EnsureCSVHeaderExists();
            if (completedTradeData?.Entry == null || completedTradeData?.Exit == null)
            {
                // Log error or skip
                return;
            }

            CalculateTradeMetrics(completedTradeData);
            CSVLoggerTP.Info(ToCSV(completedTradeData));
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

        public string ToCSV(ClosedTrade td)
        {
            return $"{td.StrategyVersion}," +
                   $"{td.TradeId}," +
                   $"{td.Entry.DateTime:yyyy-MM-dd}," +
                   $"{td.SessionTradeNumber}," +
                   $"{td.Instrument.MasterInstrument.Name}," +
                   $"{td.Account?.Name ?? "Unknown"}," +
                   $"{td.Direction}," +
                   $"{td.Entry.Quantity}," +
                   $"{td.Entry.SignalPrice:F2}," +
                   $"{td.Entry.FillPrice:F2}," +
                   $"{td.Exit.SignalPrice:F2}," +
                   $"{td.Exit.FillPrice:F2}," +
                   $"{td.Entry.DateTime:HH:mm:ss}," +
                   $"{td.Exit.DateTime:HH:mm:ss}," +
                   $"{td.Exit.Quantity}," +
                   $"{EscapeCSV(td.Exit.Reason)}," +
                   $"{td.Metrics.GrossProfit:F2}," +
                   $"{td.Metrics.GrossProfitR:F2}," +
                   $"{td.Metrics.Commission:F2}," +
                   $"{td.Metrics.NetProfit:F2}," +
                   $"{td.Metrics.Duration.TotalMinutes:F2}," +
                   $"{td.InitialRisk:F2}," +
                   $"{td.HighestHighSinceEntry:F2}," +
                   $"{td.LowestLowSinceEntry:F2}," +
                   $"{td.Metrics.MAE:F2}," +
                   $"{td.Metrics.MFE:F2}," +
                   $"{td.Metrics.EntrySlippage:F4}," +
                   $"{td.Metrics.ExitSlippage:F4}";
        }

        public void WriteHeader()
        {
            CSVLoggerTP.Info(GetCSVHeader());
        }

        private string GetCSVHeader()
        {
            return
                "StrategyVersion," +
                "TradeId," +
                "SessionDate," +
                "TradeNumInSession," +
                "Instrument," +
                "Account," +
                "Direction," +
                "Qty," +
                "SignalEntryPrice," +
                "FillEntryPrice," +
                "SignalExitPrice," +
                "FillExitPrice," +
                "EntryTime," +
                "ExitTime," +
                "ExitQty," +
                "ExitReason," +
                "GrossProfit," +
                "GrossProfitR," +
                "Commission," +
                "NetProfit," +
                "DurationMinutes," +
                "InitialRisk," +
                "HighestHigh," +
                "LowestLow," +
                "MAE," +
                "MFE," +
                "EntrySlippage," +
                "ExitSlippage";
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            
            return value;
        }

        private void EnsureCSVHeaderExists()
        {
            Logger csvLogger = CSVLoggerTP;

            var logEvent = new LogEventInfo(NLog.LogLevel.Info, csvLogger.Name, string.Empty);

            foreach (var property in csvLogger.Properties)
            {
                logEvent.Properties[property.Key] = property.Value;
            }

            string logfilePath = LogManager.Configuration
                .FindTargetByName<NLog.Targets.FileTarget>("TradeLogCSVTarget")
                ?.FileName
                ?.Render(logEvent);

            if (String.IsNullOrEmpty(logfilePath))
            {
                LoggerTP.Error("LogfilePath is null. Not able to check if logfile needs CSV header.");
                return;
            }

            if (!File.Exists(logfilePath) || new FileInfo(logfilePath).Length == 0)
            {
                WriteHeader();
            }
        }
    }
}


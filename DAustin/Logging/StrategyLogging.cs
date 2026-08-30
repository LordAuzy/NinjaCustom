using NLog;
using System;
using System.IO;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.DAustin.Logging
{
    public sealed class StrategyLogging
    {
        #region Properties

        public StrategyLogIdentity Identity { get; }

        public StrategyLoggingOptions Options { get; }

        public string TradeCSVSchemaVersion { get; private set; }
        public string TelemetryCSVSchemaVersion { get; private set; }
        public string RunId { get; private set; }
        #endregion

        #region Private Fields
        private readonly Logger diagnosticLogger;
        private readonly Logger tradeLogger;
        private readonly Logger tradeCSVLogger;
        private readonly Logger telemetryCSVLogger;
        private readonly Logger runSummaryLogger;
        #endregion

        #region Constructors

        private StrategyLogging(
            StrategyLogIdentity identity,
            StrategyLoggingOptions options,
            string tradeCSVSchemaVersion,
            string telemetryCSVSchemaVersion)
        {
            Identity = identity;
            Options = options;
            TradeCSVSchemaVersion = tradeCSVSchemaVersion;
            TelemetryCSVSchemaVersion = telemetryCSVSchemaVersion;
            RunId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            //
            // NLog.config decides what files/targets receive
            // the various logger names and levels.
            //
            diagnosticLogger = CreateLogger( "Strategy", identity, RunId);
            tradeLogger = CreateLogger("TradeExecution", identity, RunId);
            tradeCSVLogger = CreateLogger("TradeExecutionCSV", identity, RunId);
            telemetryCSVLogger = CreateLogger("TelemetryTradeExecutionCSV", identity, RunId);
            runSummaryLogger = CreateLogger("RunSummary", identity, RunId);
        }

        #endregion

        #region Factory
        public static StrategyLogging Create(
            string strategyName,
            string instrumentName,
            string accountName,
            StrategyLoggingOptions options = null,
            string tradeCSVSchemaVersion = null,
            string telemetryCSVSchemaVersion = null)
        {
            var identity =
                new StrategyLogIdentity(
                    strategyName,
                    instrumentName,
                    accountName);

            options = options ?? new StrategyLoggingOptions();
            return new StrategyLogging(
                identity,
                options,
                tradeCSVSchemaVersion,
                telemetryCSVSchemaVersion);
        }

        #endregion

        #region Diagnostic Logging

        public void Trace(string message)
        {
            if (Options.Level < StrategyLogLevel.Trace)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Trace,
                message,
                null,
                null);
        }

        public void Trace(
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Trace)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Trace,
                simTime: null,
                exception: null,
                message,
                args);
        }

        public void Trace(
            DateTime simTime,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Trace)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Trace,
                message,
                simTime,
                null);
        }

        public void Trace(
            DateTime simTime,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Trace)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Trace,
                simTime: simTime,
                exception: null,
                message,
                args);
        }

        public void Debug(string message)
        {
            if (Options.Level < StrategyLogLevel.Debug)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Debug,
                message,
                null,
                null);
        }

        public void Debug(
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Debug)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Debug,
                simTime: null,
                exception: null,
                message,
                args);
        }

        public void Debug(
            DateTime simTime,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Debug)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Debug,
                message,
                simTime,
                null);
        }

        public void Debug(
            DateTime simTime,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Debug)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Debug,
                simTime: simTime,
                exception: null,
                message,
                args);
        }

        public void Info(string message)
        {
            if (Options.Level < StrategyLogLevel.Info)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Info,
                message,
                null,
                null);
        }

        public void Info(
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Info)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Info,
                simTime: null,
                exception: null,
                message,
                args);
        }

        public void Info(
            DateTime simTime,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Info)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Info,
                message,
                simTime,
                null);
        }

        public void Info(
            DateTime simTime,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Info)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Info,
                simTime: simTime,
                exception: null,
                message,
                args);
        }

        public void Warn(string message)
        {
            if (Options.Level < StrategyLogLevel.Warn)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Warn,
                message,
                null,
                null);
        }

        public void Warn(
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Warn)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Warn,
                simTime: null,
                exception: null,
                message,
                args);
        }

        public void Warn(
            DateTime simTime,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Warn)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Warn,
                message,
                simTime,
                null);
        }

        public void Warn(
            DateTime simTime,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Warn)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Warn,
                simTime: simTime,
                exception: null,
                message,
                args);
        }
 
        public void Error(string message)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Error,
                message,
                null,
                null);
        }

        public void Error(
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Error,
                simTime: null,
                exception: null,
                message,
                args);
        }

        public void Error(
            DateTime simTime,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Error,
                message,
                simTime,
                null);
        }

        public void Error(
            DateTime simTime,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Error,
                simTime: simTime,
                exception: null,
                message,
                args);
        }

        public void Error(
            Exception exception,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Error,
                message,
                null,
                exception);
        }

        public void Error(
            Exception exception,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Error,
                simTime: null,
                exception: exception,
                message,
                args);
        }

        public void Error(
            DateTime simTime,
            Exception exception,
            string message)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            Write(
                diagnosticLogger,
                LogLevel.Error,
                message,
                simTime,
                exception);
        }

        public void Error(
            DateTime simTime,
            Exception exception,
            string message,
            params object[] args)
        {
            if (Options.Level < StrategyLogLevel.Error)
                return;

            WriteFormatted(
                diagnosticLogger,
                LogLevel.Error,
                simTime: simTime,
                exception: exception,
                message,
                args);
        }
        #endregion

        #region SummaryLogging
        public void WriteRunSummary(string message)
        {
            if (!Options.EnableRunSummary)
                return;

            Write(
                runSummaryLogger,
                LogLevel.Info,
                message,
                null,
                null);
        }

        public void WriteRunSummary(DateTime simTime, string message)
        {
            if (!Options.EnableRunSummary)
                return;

            Write(
                runSummaryLogger,
                LogLevel.Info,
                message,
                simTime,
                null);
        }
        #endregion

        #region Trade Logging

        public void WriteTrade(string message)
        {
            if (!Options.EnableTradeLog)
                return;

            Write(
                tradeLogger,
                LogLevel.Info,
                message,
                null,
                null);
        }

        public void WriteTrade(
            DateTime simTime,
            string message)
        {
            if (!Options.EnableTradeLog)
                return;

            Write(
                tradeLogger,
                LogLevel.Info,
                message,
                simTime,
                null);
        }


        public void WriteTradeCSV(string csv)
        {
            if (!Options.EnableTradeCSV)
                return;

            Write(
                tradeCSVLogger,
                LogLevel.Info,
                csv,
                null,
                null);
        }

        public void WriteTradeCSV(
            DateTime simTime,
            string csv)
        {
            if (!Options.EnableTradeCSV)
                return;

            Write(
                tradeCSVLogger,
                LogLevel.Info,
                csv,
                simTime,
                null);
        }


        public void WriteTelemetryCSV(string csv)
        {
            if (!Options.EnableTelemetryCSV)
                return;

            Write(
                telemetryCSVLogger,
                LogLevel.Info,
                csv,
                null,
                null);
        }

        public void WriteTelemetryCSV(
            DateTime simTime,
            string csv)
        {
            if (!Options.EnableTelemetryCSV)
                return;

            Write(
                telemetryCSVLogger,
                LogLevel.Info,
                csv,
                simTime,
                null);
        }

        public void Flush()
        {
            LogManager.Flush();
        }
        #endregion

        #region Private Methods

        private static Logger CreateLogger(
            string loggerName,
            StrategyLogIdentity identity,
            string runId)
        {
            Logger baseLogger = LogManager.GetLogger(loggerName);

            //
            // This is now the ONLY contextual property needed
            // to determine the destination directory.
            //
            return baseLogger.
                WithProperty("LogDirectory", identity.LogDirectory)
                .WithProperty("RunId", runId);
        }


        private static void Write(
            Logger logger,
            LogLevel level,
            string message,
            DateTime? simTime,
            Exception exception)
        {
            if (!logger.IsEnabled(level))
                return;

            var logEvent =
                new LogEventInfo(
                    level,
                    logger.Name,
                    message);

            if (simTime.HasValue)
            {
                logEvent.Properties["SimTime"] =
                    simTime.Value;
            }

            if (exception != null)
            {
                logEvent.Exception =
                    exception;
            }

            logger.Log(logEvent);
        }

        private static void WriteFormatted(
            Logger logger,
            LogLevel level,
            DateTime? simTime,
            Exception exception,
            string message,
            object[] args)
        {
            if (!logger.IsEnabled(level))
                return;

            var logEvent =
                new LogEventInfo(
                    level,
                    logger.Name,
                    null,
                    message,
                    args,
                    exception);

            if (simTime.HasValue)
            {
                logEvent.Properties["SimTime"] =
                    simTime.Value;
            }

            logger.Log(logEvent);
        }
        #endregion

        #region CSVStuff
        public string GetTradeCSVFilePath(DateTime logTime)
        {
            string tradesDirectory =
                Path.Combine(
                    Identity.LogDirectory,
                    "Trades");

            string fileName =
                $"Trades_v{TradeCSVSchemaVersion}_{RunId}.csv";

            return Path.Combine(
                tradesDirectory,
                fileName);
        }

        public string GetTelemetryCSVFilePath(DateTime logTime)
        {
            string tradesDirectory =
                Path.Combine(
                    Identity.LogDirectory,
                    "Trades");

            string fileName =
                $"TradeBarTelemetry_v{TelemetryCSVSchemaVersion}_{RunId}.csv";

            return Path.Combine(
                tradesDirectory,
                fileName);
        }

        public void EnsureTradeCSVHeaderExists(
            DateTime logTime,
            string header)
        {
            if (!Options.EnableTradeCSV)
                return;

            string filePath = GetTradeCSVFilePath(logTime);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Error("Trade CSV logfile path is null. Unable to check CSV header.");
                return;
            }

            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                WriteTradeCSV(logTime, header);
            }
        }

        public void EnsureTelemetryCSVHeaderExists(
            DateTime logTime,
            string header)
        {
            if (!Options.EnableTelemetryCSV)
                return;

            string filePath = GetTelemetryCSVFilePath(logTime);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Error("Telemetry CSV logfile path is null. Unable to check CSV header.");
                return;
            }

            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                WriteTelemetryCSV(logTime, header);
            }
        }

        #endregion
    }
}
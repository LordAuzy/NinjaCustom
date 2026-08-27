using NinjaTrader.Custom.DAustin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Logging
{
    public sealed class StrategyLoggingOptions
    {
        public StrategyLogLevel Level { get; private set; }

        public bool EnableTradeLog { get; private set; }

        public bool EnableTradeCSV { get; private set; }

        public bool EnableTelemetryCSV { get; private set; }

        public StrategyLoggingOptions()
        {
        }

        public static StrategyLoggingOptions FromMode(
            LoggingMode mode)
        {
            switch (mode)
            {
                case LoggingMode.Off:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Off,
                        EnableTradeLog = false,
                        EnableTradeCSV = false,
                        EnableTelemetryCSV = false
                    };

                case LoggingMode.Error:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Error,
                        EnableTradeLog = true,
                        EnableTradeCSV = true,
                        EnableTelemetryCSV = true
                    };

                case LoggingMode.Normal:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Info,
                        EnableTradeLog = true,
                        EnableTradeCSV = true,
                        EnableTelemetryCSV = true
                    };

                case LoggingMode.Diagnostic:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Debug,
                        EnableTradeLog = true,
                        EnableTradeCSV = true,
                        EnableTelemetryCSV = true
                    };

                case LoggingMode.Trace:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Trace,
                        EnableTradeLog = true,
                        EnableTradeCSV = true,
                        EnableTelemetryCSV = true
                    };

                default:
                    return new StrategyLoggingOptions
                    {
                        Level = StrategyLogLevel.Error,
                        EnableTradeLog = true,
                        EnableTradeCSV = true,
                        EnableTelemetryCSV = true
                    };
            }
        }
    }
}
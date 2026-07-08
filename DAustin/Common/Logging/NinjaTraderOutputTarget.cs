using NLog;
using NLog.Targets;
using NinjaTrader.NinjaScript.Strategies;
using System;

namespace NinjaTrader.Custom.DAustin.Common.Logging
{
    [Target("NinjaTraderOutput")]
    public sealed class NinjaTraderOutputTarget : TargetWithLayout
    {
        protected override void Write(LogEventInfo logEvent)
        {
            string message = Layout.Render(logEvent);

            // Get Strategy from GDC (Global Diagnostics Context)
            object strategyObj = GlobalDiagnosticsContext.GetObject("Strategy");

            if (strategyObj is StratBase strategy)
            {
                // Write to NinjaTrader's output window
                strategy.Print($"[{logEvent.Level}] {message}");
            }
            else
            {
                // Fallback to system debug
                System.Diagnostics.Debug.WriteLine($"[NT-NoStrategy] [{logEvent.Level}] {message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    /// <summary>
    /// Constants for NinjaScript property GroupName values used across strategies.
    /// Using constants ensures consistency and prevents typos in Display attributes.
    /// </summary>
    public static class StratPropertyGroups
    {
        // Common groups used across multiple strategies
        public const string GeneralParameters = "GeneralParameters";
        public const string Parameters = "Parameters";
        public const string Indicators = "Indicators";
        public const string TimeParams = "Time Parameters";
        public const string TrendCHATGPT = "TrendChatGPT";
        public const string ScheduleBiasFilter = "Schedule Bias Filter";
        public const string ScheduleSizingFilter = "Schedule Sizing Filter";

        // Risk Management
        public const string BreakEven = "BreakEven";
        public const string StopLoss = "StopLoss Parameters";
        
        // Entry/Exit
        public const string Entry = "Entry";
        public const string Exit = "Exit";
        
        // Filters
        public const string ATRRegimeFilter = "ATR Regime Filter";

        // Trailing Stop Systems
        public const string TrendStructureTrail = "Trend Structure Trail";
        public const string ChandelierGuardStop = "Chandelier Guard Stop";
        public const string AdaptiveTrailingStop = "AdaptiveTrailingStop";
        public const string TrailingStop = "Trailing Stop";

        public const string Test = "Test";

    }

    public static class StratIdentifiers
    {
        public const string ORB         = "ORB";
        public const string VWAPPB      = "VWAPPB";
        public const string VWAPPB_V1   = "VWAPPB_V1";
        public const string VWAPMR      = "VWAPMR";
        public const string TRENCLAUD   = "TRENCLAUD";
        public const string TREND       = "TREND";
        public const string TRENDCL     = "TRENDCL";
    }
}

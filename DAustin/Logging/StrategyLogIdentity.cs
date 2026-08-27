using System;
using System.IO;

namespace NinjaTrader.Custom.DAustin.Logging
{
    public sealed class StrategyLogIdentity
    {
        #region Properties

        public string StrategyName { get; }
        public string InstrumentName { get; }
        public string AccountName { get; }

        public string LogDirectory { get; }

        #endregion

        #region Constructors

        public StrategyLogIdentity(
            string strategyName,
            string instrumentName,
            string accountName)
        {
            StrategyName = SanitizePathComponent(
                strategyName,
                "UnknownStrategy");

            InstrumentName = SanitizePathComponent(
                instrumentName,
                "UnknownInstrument");

            AccountName = SanitizePathComponent(
                accountName,
                "UnknownAccount");

            LogDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments),
                "NinjaTrader 8",
                "NLogLogs",
                StrategyName,
                InstrumentName,
                AccountName);
        }

        #endregion

        #region Private Methods

        private static string SanitizePathComponent(
            string value,
            string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            string result = value.Trim();

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c, '_');
            }
            return result;
        }
        #endregion
    }
}
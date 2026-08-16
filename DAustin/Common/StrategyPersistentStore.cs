using Newtonsoft.Json;
using NinjaTrader.Core;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class StrategyPersistentStore
    {
        private readonly string filePath;
        private readonly SemaphoreSlim fileSemaphore = new SemaphoreSlim(1, 1);
        private Dictionary<string, object> storeCollection;
        private bool isLoaded = false;
        private StratBase Strategy { get; }

        public StrategyPersistentStore(StratBase strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            Strategy = strategy;

            // Create a dedicated directory under Documents\NinjaTrader 8\CustomStorage
            string dir = Path.Combine(Globals.UserDataDir, "CustomStorage");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Construct unique filename using strategy and instrument context
            string tf = strategy.BarsPeriod != null ? $"{strategy.BarsPeriod.Value}{strategy.BarsPeriod.MarketDataType}" : "Default";
            string accountName = strategy.Account != null ? strategy.Account.Name : "Sim";
            string rawFileName = $"{strategy.Name}_{strategy.Instrument.FullName}_{tf}_{accountName}_Storage.json";

            // Sanitize file path against invalid OS characters
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                rawFileName = rawFileName.Replace(c, '_');
            }

            filePath = Path.Combine(dir, rawFileName);
        }

        #region First-Request Auto Load

        private void EnsureLoaded()
        {
            if (isLoaded) return;

            fileSemaphore.Wait();
            try
            {
                if (isLoaded) return;

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        // Newtonsoft handles Dictionary<string, object> natively
                        storeCollection = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                NinjaTrader.Code.Output.Process($"StrategyStorage Load Error [{filePath}]: {ex.Message}", PrintTo.OutputTab1);
            }
            finally
            {
                storeCollection ??= new Dictionary<string, object>();
                isLoaded = true;
                fileSemaphore.Release();
            }
        }
        #endregion

        #region Disk Write & Synchronization

        private void SaveToDisk()
        {
            fileSemaphore.Wait();
            try
            {
                string json = JsonConvert.SerializeObject(storeCollection, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                NinjaTrader.Code.Output.Process($"StrategyStorage Load Error [{filePath}]: {ex.Message}", PrintTo.OutputTab1);
            }
            finally
            {
                fileSemaphore.Release();
            }
        }

        #endregion

        #region Generic Get/Set API

        public T Get<T>(string key, T defaultValue = default)
        {
            EnsureLoaded();

            if (storeCollection.TryGetValue(key, out object value) && value != null)
            {
                try
                {
                    // Newtonsoft handles numeric type casting safely (e.g. Int64 to Int32)
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public void Set<T>(string key, T value)
        {
            EnsureLoaded();
            storeCollection[key] = value;
            SaveToDisk();
        }

        #endregion

        #region Convenience Properties

        public int NameIndex
        {
            get => Get<int>(nameof(NameIndex), 0);
            set => Set(nameof(NameIndex), value);
        }

        #endregion
    }
}

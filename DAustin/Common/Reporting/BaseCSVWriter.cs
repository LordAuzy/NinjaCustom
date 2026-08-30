using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class BaseCSVWriter
    {
        #region Properties
        private List<ICSVDataSource> _csvDataSources = new List<ICSVDataSource>();
        public int DataSourceCount { get { return _csvDataSources.Count(); } }

        public StratBase Strategy { get; private set; }
        #endregion

        #region Constructors
        public BaseCSVWriter(StratBase strategy)
        {
            Strategy = strategy;
        }
        #endregion

        #region Public Methods
        public void AddDataSource(ICSVDataSource dataSource)
        {
            if (dataSource != null && !_csvDataSources.Contains(dataSource))
            {
                _csvDataSources.Add(dataSource);
            }
        }

        public void RemoveDataSource(ICSVDataSource dataSource)
        {
            if (dataSource != null && _csvDataSources.Contains(dataSource))
            {
                _csvDataSources.Remove(dataSource);
            }
        }

        public void LogCSV()
        {
            if (_csvDataSources != null && _csvDataSources.Count > 0)
            {

                DateTime simTime = Strategy.GetDataTimeForLogger();
                List<string> colNames = new List<string>();
                List<string> dataRow = new List<string>();

                foreach (ICSVDataSource dataSource in _csvDataSources)
                {
                    dataSource.GetColumnNames(colNames);
                    dataSource.Rewind();
                }
                EnsureCSVHeaderExists(simTime, colNames);

                do
                {
                    dataRow.Clear();
                    foreach (ICSVDataSource dataSource in _csvDataSources)
                    {
                        if (dataSource != null && dataRow != null)
                        {
                            dataRow = dataSource.NextDataRow(dataRow);
                        }
                        else
                        {
                            Strategy.Logs.Warn("Encountered a null dataSource in the _csvDatasources collection");
                            dataRow = null;
                        }
                    }

                    if (dataRow != null && dataRow.Count > 0)
                    {
                        WriteCSV(simTime, dataRow);
                    }
                } while (dataRow != null);
            }
            else
            {
                Strategy.Logs.Warn("Attempting to log to CSV when _csvDatasources is empty");
            }
        }
        #endregion

        #region Virtuals
        protected virtual void EnsureCSVHeaderExists(
            DateTime simTime, 
            List<string> colNames)
        {
            // Implementation for ensuring CSV header exists. Overridden
            // in derived classes.
        }

        protected virtual void WriteCSV(
            DateTime simTime,
            List<string> dataRow)
        {
            // Implementation for writing CSV data. Overridden
            // in derived classes.
        }
        #endregion
    }
}

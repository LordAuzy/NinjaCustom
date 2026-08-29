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
            DateTime simTime = Strategy.GetDataTimeForLogger();
            List<string> colNames = new List<string>();
            List<string> dataRow = new List<string>();

            foreach (var dataSource in _csvDataSources)
            {
                dataSource.GetColumnNames(colNames);
            }
            EnsureCSVHeaderExists(simTime, colNames);

            foreach (var dataSource in _csvDataSources)
            {
                dataSource.FirstDataRow(dataRow);
            }

            if (dataRow.Count == colNames.Count)
            {   // only write the row if the number of data items
                // matches the number of columns
                WriteCSV(simTime, dataRow);
            }
            else
            {
                Strategy.Logs.Warn($"Data row count ({dataRow.Count}) does not match column count ({colNames.Count}). Row not logged.");
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

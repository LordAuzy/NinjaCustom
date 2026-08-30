using System;
using System.Collections.Generic;
using System.Linq;
 using System.Text;
using System.Threading.Tasks;
using NinjaTrader.Custom.DAustin.Interfaces;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TelemetryBarCollection : List<ITelemetryBar>, ICSVDataSource
    {
        #region Properties
        #endregion

        #region Constructors
        public TelemetryBarCollection() 
        { 
        }
        #endregion

        #region ICSVDataSource implementation
        int dataRowIndex = 0;

        public void Rewind()
        {
            dataRowIndex = -1;
        }

        public List<string> NextDataRow(List<string> data)
        {
            TelemetryBarBase tbb = null;
            List<string> dataRow = null;
            dataRowIndex++;

            if (dataRowIndex < this.Count)
            {
                tbb = this[dataRowIndex] as TelemetryBarBase;

                tbb.Rewind();   // each telemetry bar only has a single row
                dataRow = tbb.NextDataRow(data);
            }
            return dataRow;
        }

        public List<string> GetColumnNames(List<string> columns)
        {
            List<string> columnNames = null;

            // if we don't have any telemetry bars, we can't
            // get column names, so return null
            if (this.Count > 0)
            {
                TelemetryBarBase tbb = this[0] as TelemetryBarBase;
                columnNames = tbb.GetColumnNames(columns);
            }

            return columnNames;
        }
        #endregion

        #region Methods
        public TelemetryBarCollection ShallowClone()
        {
            TelemetryBarCollection newCollection = new TelemetryBarCollection();

            // Copy the references of the telemetry bars to the new collection
            foreach (var telemetryBar in this)
            {
                newCollection.Add(telemetryBar);
            }

            return newCollection;
        }
        #endregion
    }
}

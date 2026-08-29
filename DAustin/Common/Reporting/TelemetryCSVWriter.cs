using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class TelemetryCSVWriter : BaseCSVWriter
    {
        #region Constructors
        public TelemetryCSVWriter(StratBase strategy) : base(strategy)
        {
        }
        #endregion

        #region overrides
        protected override void WriteCSV(
            DateTime simTime,
            List<string> dataRow)
        {
            string data = string.Join(",", dataRow);
            Strategy.Logs.WriteTelemetryCSV(simTime, data);
        }

        protected override void EnsureCSVHeaderExists(
            DateTime simTime,
            List<string> colNames)
        {
            string header = string.Join(",", colNames);
            Strategy.Logs.EnsureTelemetryCSVHeaderExists(simTime, header);
        }
        #endregion
    }
}

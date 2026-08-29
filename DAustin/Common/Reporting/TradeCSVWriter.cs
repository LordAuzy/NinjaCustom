using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class TradeCSVWriter : BaseCSVWriter
    {
        #region Constructors
        public TradeCSVWriter(StratBase strategy) : base(strategy)
        {
        }
        #endregion

        #region overrides
        protected override void WriteCSV(
            DateTime simTime,
            List<string> dataRow)
        {
            string data = string.Join(",", dataRow);
            Strategy.Logs.WriteTradeCSV(simTime, data);
        }

        protected override void EnsureCSVHeaderExists(
            DateTime simTime, 
            List<string> colNames)
        {
            string header = string.Join(",", colNames);
            Strategy.Logs.EnsureTradeCSVHeaderExists(simTime, header);
        }
        #endregion
    }
}

using NinjaTrader.Custom.DAustin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class DataCollectorBase : IDataCollector
    {
        #region ClassDefinitions
        public class  OrderCounters
        {
            public int LongMarketOrderSubmittedCount { get; set; }
            public int ShortMarketOrderSubmittedCount { get; set; }
            public int LongStopMarketOrderSubmittedCount { get; set; }
            public int ShortStopMarketOrderSubmittedCount { get; set; }
            public int LongEntryOrderFilledCount { get; set; }
            public int LongEntryOrderExpiredCount { get; set; }
            public int LongEntryOrderCancelledCount { get; set; }
            public int LongEntryOrderRejectedCount { get; set; }

            public void EntryOrderSubmittedCount(DAOrderType ot, int incrementCount)
            {
                if (ot == DAOrderType.Short)
                {
                    ShortMarketOrderSubmittedCount += incrementCount;
                }
                else if (ot == DAOrderType.Long)
                {
                    LongMarketOrderSubmittedCount += incrementCount;
                }
                else if (ot == DAOrderType.ShortStopMarket)
                {
                    ShortStopMarketOrderSubmittedCount += incrementCount;
                }
                else if (ot == DAOrderType.LongStopMarket)
                {
                    LongStopMarketOrderSubmittedCount += incrementCount;
                }
            }
        }

        #endregion
        #region Properties
        public StratBase Strategy { get; set; } = null;
        public OrderCounters Orders { get; set; } = new OrderCounters();

        #endregion

        #region Constructors
        public DataCollectorBase(StratBase strat)
        {
            Strategy = strat;
        }

        public DataCollectorBase()
        {

        }
        #endregion

        public virtual void ToStringBuilder(StringBuilder sb)
        {

        }
    }
}

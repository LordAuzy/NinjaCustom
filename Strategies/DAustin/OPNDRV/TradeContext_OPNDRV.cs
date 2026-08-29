using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Reporting;
using NinjaTrader.Custom.DAustin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class TradeContext_OPNDRV : TradeContext
    {
        public DriveSetup DriveSetup { get; set; } = null;

        #region Constructors
        public TradeContext_OPNDRV() : base()
        { 
        
        }

        public TradeContext_OPNDRV(IEntryConditionsEvaluator ece) : base(ece)
        {

        }
        #endregion

        #region overrides
        public override void AddDataSources(TradeCSVWriter csvw)
        {
            // first add the base data sources (trade context)
            base.AddDataSources(csvw);

            if (DriveSetup != null) 
            {
                csvw.AddDataSource(DriveSetup);
            }
        }
        #endregion
    }
}

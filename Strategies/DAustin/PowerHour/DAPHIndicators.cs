using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.PowerHour
{
    public class DAPHIndicators : StratIndicatorsBase
    {
        #region Properties
        public SMA VolSMA20 { get; private set; }
        #endregion

        #region Constructors
        public DAPHIndicators(Strategy strat) : base(strat)
        { 

        }
        #endregion

        #region PublicMethods
        public override void Initialize()
        {
            base.Initialize();

            VolSMA20 = Strategy.SMA(Strategy.Volume, 20);
        }

        public override void Update()
        {
            base.Update();
        }
        #endregion
    }
}

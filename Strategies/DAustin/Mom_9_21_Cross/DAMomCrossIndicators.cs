using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross
{
    public class DAMomCrossIndicators : StratIndicatorsBase
    {
        #region Properties
        #endregion

        #region Constructors
        public DAMomCrossIndicators(Strategy strat) : base(strat)
        { 

        }
        #endregion

        #region PublicMethods
        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update()
        {
            base.Update();
        }
        #endregion
    }
}

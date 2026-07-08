using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class StratIndicatorsBase
    {
        #region Properties
        public DAVWAP NYSessionAnchoredVWAP { get; private set; }
        public ATR ATR { get; private set; }
        public int ATRPeriod { get; private set; } = 14;
        public Strategy Strategy { get; private set; }
        public SMA VolSMA20 { get; private set; }
        public EMA FastEMA { get; private set; }
        public EMA SlowEMA { get; private set; }
        public int SlowPeriod { get; private set; } = 21;
        public int FastPeriod { get; private set; } = 9;
        // 14 is the sweet spot when trading the 9-21 crossover
        public int DMPeriod { get; private set; } = 14;
        public DM DM { get; private set; } = null;
        #endregion

        #region Constructors
        public StratIndicatorsBase(Strategy strat) 
        { 
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public virtual void Initialize()
        {
            NYSessionAnchoredVWAP = new DAVWAP(Strategy, "9:30am", "Eastern Standard Time");
            ATR = Strategy.ATR(ATRPeriod);
            VolSMA20 = Strategy.SMA(Strategy.Volume, 20);
            FastEMA = Strategy.EMA(FastPeriod);
            SlowEMA = Strategy.EMA(SlowPeriod);
            DM = Strategy.DM(DMPeriod);
        }

        public virtual void Update()
        {
            NYSessionAnchoredVWAP.Update();
        }
        #endregion
    }
}

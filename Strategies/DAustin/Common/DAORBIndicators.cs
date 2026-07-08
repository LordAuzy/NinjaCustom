using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class DAORBIndicators
    {
        #region Properties
        public DAVWAP NYSessionAnchoredVWAP { get; private set; }
        public Strategy Strategy { get; private set; }
        public int FastEMAPeriod { get; set; } = 9;
        public EMA FastEMA { get; private set; }
        public int SlowEMAPeriod { get; set; } = 21;
        public EMA SlowEMA { get; private set; }
        public int RSIPeriod { get; set; } = 7;
        public RSI RSI { get; private set; }
        public int ATRPeriod { get; set; } = 14;
        public ATR ATR { get; private set; }
        #endregion

        #region Constructors
        public DAORBIndicators(Strategy strat) 
        { 
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public void Initialize()
        {
            FastEMA = Strategy.EMA(FastEMAPeriod);
            SlowEMA = Strategy.EMA(SlowEMAPeriod);
            RSI = Strategy.RSI(RSIPeriod, 3);
            ATR = Strategy.ATR(ATRPeriod);
            NYSessionAnchoredVWAP = new DAVWAP(Strategy, "9:30am", "Eastern Standard Time");
        }

        public void Update()
        {
            NYSessionAnchoredVWAP.Update();
        }
        #endregion
    }
}

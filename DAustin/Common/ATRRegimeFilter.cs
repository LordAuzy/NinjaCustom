using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class ATRRegimeFilter
    {
        #region Properties
        public StratBase Strategy { get; set; }

        public ATRRegimeParameters RP { get; private set; }

        public ATR AtrPeriod { get; set; }
        public EMA EMAAtrFast { get; set; }
        public EMA EMAAtrSlow { get; set; }

        #endregion

        #region Constructors
        public ATRRegimeFilter(
            StratBase strat,
            ATRRegimeParameters rp)
        {
            Strategy = strat;
            RP = rp;
            InitializeIndicators();
        }
        #endregion

        #region PublicMethods
        public bool Passed(double close)
        {
            bool passed = false;

            if (close > 0)
            {
                // Check 1: Absolute ATR floor
                double atrPct = AtrPeriod[0] / close;
                if (RP.MinAtrPercent > 0 && atrPct < RP.MinAtrPercent)
                {
                    passed = false;  // Block: ATR too low (dead market)
                }
                else
                {
                    // Check 2: Regime expansion/compression
                    double atrPctFast = EMAAtrFast[0] / close;
                    double atrPctSlow = EMAAtrSlow[0] / close;

                    if (atrPctSlow <= 0)
                    {
                        passed = true;  // Can't compute ratio safely, allow by default
                    }
                    else
                    {
                        double regimeRatio = atrPctFast / atrPctSlow;
                        if (RP.MinAtrRegimeRatio > 0 && regimeRatio < RP.MinAtrRegimeRatio)
                        {
                            passed = false;  // Block: volatility compressing
                        }
                        else
                        {
                            passed = true;  // Passed both checks
                        }
                    }
                }
            }

            return passed;
        }
        #endregion

        #region PrivateMethods
        private void InitializeIndicators()
        {
            if (RP.AtrRegimeAtrPeriod > 0 && RP.AtrRegimeFastPeriod > 0 && RP.AtrRegimeSlowPeriod > 0)
            {
                AtrPeriod = Strategy.ATR(RP.AtrRegimeAtrPeriod);
                EMAAtrFast = Strategy.EMA(AtrPeriod, RP.AtrRegimeFastPeriod);
                EMAAtrSlow = Strategy.EMA(AtrPeriod, RP.AtrRegimeSlowPeriod);
            }
        }
        #endregion
    }
}

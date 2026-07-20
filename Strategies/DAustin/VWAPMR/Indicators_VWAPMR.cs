using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPMR
{
    [StrategyComponentId("IDC-VWAPMR")]
    public class Indicators_VWAPMR : IndicatorsBase
    {
        #region classDefinitions
        public class EntryIndicators
        {
            public ATR ATR { get; set; }
        }

        public EMA FastEMA { get; set; }
        public EMA SlowEMA { get; set; }
        public EMA TrendEMA { get; set; }
        public RSI RSI { get; set; }
        public ADX ADX { get; set; }


        #endregion

        #region Properties
        private OptimizationParameters_VWAPMR OptParamsVWAPMR { get { return OptParams as OptimizationParameters_VWAPMR; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        #endregion

        public Indicators_VWAPMR(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            Entry.ATR = Strategy.ATR(OptParamsVWAPMR.Entry.ATRPeriod);

            FastEMA = Strategy.EMA(OptParamsVWAPMR.FastEMAPeriod);
            SlowEMA = Strategy.EMA(OptParamsVWAPMR.SlowEMAPeriod);
            TrendEMA = Strategy.EMA(OptParamsVWAPMR.TrendEMAPeriod);
            RSI = Strategy.RSI(OptParamsVWAPMR.RSIPeriod, 3);
            ADX = Strategy.ADX(OptParamsVWAPMR.ADXPeriod);
        }

        public override void Update()
        {
            base.Update();

        }
        #endregion
    }
}
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Indicators
{
    [StrategyComponentId("IDC-MEANREVERSION")]
    public class Indicators_MeanReversion : IndicatorsBase
    {
        #region Properties
        private OptimizationParameters_MeanReversion OptParamsMR { get { return OptParams as OptimizationParameters_MeanReversion; } }
        public ADX ADX { get; private set; }
        public Bollinger Bollinger { get; private set; }
        public RSI RSI { get; private set; }
        public EMA EMA50 { get; private set; }
        public EMA EMA200Daily { get; private set; }
        public ATR ATR { get; private set; }
        #endregion

        public Indicators_MeanReversion(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize() 
        { 
            base.Initialize();

            Bollinger = Strategy.Bollinger(OptParamsMR.BBDev, OptParamsMR.BBPeriod);
            RSI = Strategy.RSI(OptParamsMR.RSIPeriod, 3);
            EMA50 = Strategy.EMA(OptParamsMR.EMAPeriod);
            ADX = Strategy.ADX(OptParamsMR.ADXPeriod);
            EMA200Daily = Strategy.EMA(Strategy.BarsArray[1], 200);      // daily 200 EMA
            ATR = Strategy.ATR(OptParamsMR.ATRPeriod);
        }

        public override void Update() 
        { 
            base.Update();
        }
        #endregion
    }
}
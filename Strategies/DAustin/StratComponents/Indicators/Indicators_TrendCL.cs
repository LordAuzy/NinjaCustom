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
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.Strategies.DAustin.Indicators
{
    [StrategyComponentId("IDC-TRENDCL")]
    public class Indicators_TRENDCL : IndicatorsBase
    {
        #region classDefinitions
        public class EntryIndicators
        {
            public EMA FastEMA { get; set; }
            public EMA MidEMA { get; set; }
            public EMA SlowEMA { get; set; }
            public ATR ATR { get; set; }
            public ADX ADXFilter { get; set; }
        }
        #endregion

        #region Properties
        private OptimizationParameters_TRENDCL OptParamsTrendCL { get { return OptParams as OptimizationParameters_TRENDCL; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        public TrendStructuralTrailingIndicators TrendStructuralIndicators { get; set; } = new TrendStructuralTrailingIndicators();
        #endregion

        public Indicators_TRENDCL(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override TrendStructuralTrailingIndicators GetTrendStructuralTrailingIndicators() { return TrendStructuralIndicators; }


        public override void Initialize()
        {
            base.Initialize();

            Entry.FastEMA = Strategy.EMA(OptParamsTrendCL.Entry.FastEMAPeriod);
            Entry.MidEMA = Strategy.EMA(OptParamsTrendCL.Entry.MidEMAPeriod);
            Entry.SlowEMA = Strategy.EMA(OptParamsTrendCL.Entry.SlowEMAPeriod);
            Entry.ATR = Strategy.ATR(OptParamsTrendCL.Entry.ATRPeriod);
            Entry.ADXFilter = Strategy.ADX(OptParamsTrendCL.Entry.ADXPeriod);

            TrendStructuralIndicators.EMA = Strategy.EMA(OptParamsTrendCL.TrendStructuralTrailingStop.EMAPeriod);
            TrendStructuralIndicators.ATR = Strategy.ATR(OptParamsTrendCL.TrendStructuralTrailingStop.ATRPeriod);
        }

        public override void Update()
        {
            base.Update();
        }
        #endregion
    }
}
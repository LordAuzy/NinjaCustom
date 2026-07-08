using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.Strategies.DAustin.Indicators
{
    [StrategyComponentId("IDC-TREND")]
    public class Indicators_TREND : IndicatorsBase
    {
        #region classDefinitions
        public class EntryIndicators
        {
            public ATR ATR { get; set; }
            public EMA FastEMA { get; set; }
            public EMA SlowEMA { get; set; }
        }
        #endregion

        #region Properties
        private OptimizationParameters_TREND OptParamsTREND { get { return OptParams as OptimizationParameters_TREND; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        public BreakEvenIndicators BreakEven { get; set; } = new BreakEvenIndicators();

        public ChandelierGuardIndicators ChandelierGuard { get; set; } = new ChandelierGuardIndicators();
        public AdaptiveTrailingStopIndicators AdaptiveTrailingStopIndicators { get; set; } = new AdaptiveTrailingStopIndicators();
        public TrendStructuralTrailingIndicators TrendStructuralIndicators { get; set; } = new TrendStructuralTrailingIndicators();

        //ChatGPT
        public TimeWindowPriceRange OpeningRange { get; set; }
        public EMA emaFast { get; set; }
        public EMA  emaSlow { get; set; }
        public ATR atr { get; set; }
        public ADX adx { get; set; }
        #endregion

        public Indicators_TREND(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            BreakEven.ATR = Strategy.ATR(OptParamsTREND.BreakEven.ATRPeriod);

            OpeningRange = new TimeWindowPriceRange(    Strategy, 
                                                        OptParamsTREND.GPT_TI_ORBStartTime, 
                                                        OptParamsTREND.GPT_TI_ORBMinutes,
                                                        OptParamsTREND.GPT_TI_TimeZone.GetDisplayName());

            // keep 60 days if opening range width history for reference in optimization and live trading
            OpeningRange.HistoryBuffer = new ValueHistory(60);

            if (OptParamsTREND.GPT_TI_ORBTradingWindow > 0)
            {
                OpeningRange.SetTradeWindowDurationMinutes(OptParamsTREND.GPT_TI_ORBTradingWindow);
            }

            emaFast = Strategy.EMA(OptParamsTREND.GPT_EMAFastPeriod);
            emaSlow = Strategy.EMA(OptParamsTREND.GPT_EMASlowPeriod);
            atr = Strategy.ATR(OptParamsTREND.GPT_ATRPeriod);
            adx = Strategy.ADX(OptParamsTREND.GPT_ADXPeriod);

            ChandelierGuard.ATR = Strategy.ATR(OptParamsTREND.ChandelierGuardStop.ATRPeriod);

            AdaptiveTrailingStopIndicators.FastEMA = Strategy.EMA(OptParamsTREND.AdaptiveTrailingStop.FastEMAPeriod);
            AdaptiveTrailingStopIndicators.SlowEMA = Strategy.EMA(OptParamsTREND.AdaptiveTrailingStop.SlowEMAPeriod);
            AdaptiveTrailingStopIndicators.ATR = Strategy.ATR(OptParamsTREND.AdaptiveTrailingStop.ATRPeriod);

            TrendStructuralIndicators.EMA = Strategy.EMA(OptParamsTREND.TrendStructuralTrailingStop.EMAPeriod);
            TrendStructuralIndicators.ATR = Strategy.ATR(OptParamsTREND.TrendStructuralTrailingStop.ATRPeriod);
        }

        public override ChandelierGuardIndicators GetChandelierGuardIndicators() { return ChandelierGuard; }
        public override BreakEvenIndicators GetBreakEvenIndicators() { return BreakEven; }

        public override AdaptiveTrailingStopIndicators GetAdaptiveTrailingStopIndicators() { return AdaptiveTrailingStopIndicators; }
        public override TrendStructuralTrailingIndicators GetTrendStructuralTrailingIndicators() { return TrendStructuralIndicators; }


        public override void Update()
        {
            base.Update();
            OpeningRange.Update();
        }
        #endregion
    }
}
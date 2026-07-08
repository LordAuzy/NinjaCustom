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
    [StrategyComponentId("IDC-ORB")]
    public class Indicators_ORB : IndicatorsBase
    {
        #region Properties
        private StopLossIndicators _stopLossIndicators = new StopLossIndicators();
        public AdaptiveTrailingStopIndicators AdaptiveTrailingStopIndicators { get; set; } = new AdaptiveTrailingStopIndicators();
        private OptimizationParameters_ORB OptParamsORB { get { return OptParams as OptimizationParameters_ORB; } }
        public TimeWindowPriceRange OpeningRange { get; set; }
        public TimeWindowPriceRange PreMarketBig { get; set; }
        public TimeWindowPriceRange PreMarketSmall { get; set; }
        public ValueHistory OpeningRangeHistory { get; set; } = new ValueHistory(60);
        public ATR ATR { get; set; }
        public RSI RSI { get; set; }
        public EMA FastEMA { get; set; }
        public EMA SlowEMA { get; set; }
        public ATR TrailingATR { get; set; }
        #endregion

        public Indicators_ORB(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            OpeningRange = new TimeWindowPriceRange(Strategy, "9:30am", OptParamsORB.OpeningRangeMinutes, "Eastern Standard Time");
            OpeningRange.SetTradeWindowDurationMinutes(OptParamsORB.SessionTradingMinutes);
            OpeningRange.HistoryBuffer = OpeningRangeHistory;
            PreMarketBig = new TimeWindowPriceRange(Strategy, "4:30am", 178, "Eastern Standard Time");
            PreMarketSmall = new TimeWindowPriceRange(Strategy, "7:00am", 178, "Eastern Standard Time");
            RSI = Strategy.RSI(OptParamsORB.RSIPeriod, 3);
            ATR = Strategy.ATR(OptParamsORB.ATRPeriod);
            SlowEMA = Strategy.EMA(OptParamsORB.SlowEMAPeriod);
            FastEMA = Strategy.EMA(OptParamsORB.FastEMAPeriod);

            AdaptiveTrailingStopIndicators.SlowEMA = Strategy.EMA(OptParamsORB.AdaptiveTrailingStop.SlowEMAPeriod);
            AdaptiveTrailingStopIndicators.FastEMA = Strategy.EMA(OptParamsORB.AdaptiveTrailingStop.FastEMAPeriod);
            AdaptiveTrailingStopIndicators.ATR = Strategy.ATR(OptParamsORB.AdaptiveTrailingStop.ATRPeriod);

            _stopLossIndicators.InitialPlacementATR = Strategy.ATR(OptParamsORB.StopLoss.InitialPlacement_ATRPeriod);
        }

        public override void Update()
        {
            base.Update();

            OpeningRange.Update();
            PreMarketBig.Update();
            PreMarketSmall.Update();
        }

        public override EMA GetFastEMA { get { return FastEMA; } }
        public override EMA GetSlowEMA { get { return SlowEMA; } }

        public override AdaptiveTrailingStopIndicators GetAdaptiveTrailingStopIndicators() { return AdaptiveTrailingStopIndicators; }

        public override ATR GetTrailingATR() 
        { 
            return TrailingATR; 
        }

        public override StopLossIndicators GetStopLossIndicators()
        {
            return _stopLossIndicators;
        }
        #endregion
    }
}
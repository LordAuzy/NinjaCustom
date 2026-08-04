using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.ScheduleFilter;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB_V1
{
    [StrategyComponentId("IDC-VWAPPB_V1")]
    public class Indicators_VWAPPB_V1 : IndicatorsBase
    {
        #region classDefinitions
        public class EntryIndicators
        {
            public ATR ATR { get; set; }
            public EMA FastEMA { get; set; }
            public EMA SlowEMA { get; set; }
            public DAVWAPIndicator AnchoredVWAP { get; set; }
            public DM DM { get; set; } = null;
        }
        #endregion

        #region Properties
        private OptimizationParameters_VWAPPB_V1 OptParamsVWAPPB { get { return OptParams as OptimizationParameters_VWAPPB_V1; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        public ChandelierGuardIndicators ChandelierGuard { get; set; } = new ChandelierGuardIndicators();
        public BreakEvenIndicators BreakEven { get; set; } = new BreakEvenIndicators();
        public AdaptiveTrailingStopIndicators AdaptiveTrailingStopIndicators { get; set; } = new AdaptiveTrailingStopIndicators();
        public TrendStructuralTrailingIndicators TrendStructuralIndicators { get; set; } = new TrendStructuralTrailingIndicators();
        public BiasFilter BiasFilter { get; set; }
        public SizingFilter SizingFilter { get; set; }
        #endregion

        public Indicators_VWAPPB_V1(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            BreakEven.ATR = Strategy.ATR(OptParamsVWAPPB.BreakEven.ATRPeriod);

            Entry.ATR = Strategy.ATR(OptParamsVWAPPB.Entry.ATRPeriod);
            Entry.FastEMA = Strategy.EMA(OptParamsVWAPPB.Entry.FastEMAPeriod);
            Entry.SlowEMA = Strategy.EMA(OptParamsVWAPPB.Entry.SlowEMAPeriod);
            Entry.DM = Strategy.DM(OptParamsVWAPPB.Entry.DMPeriod);
            Entry.AnchoredVWAP = Strategy.DAVWAPIndicator("9:30am", "Eastern Standard Time");
            Entry.AnchoredVWAP.StdDevBandCount = OptParamsVWAPPB.Entry.VWAPStdDevBandCount;
            Entry.AnchoredVWAP.BandMode = VwapBandMode.Cumulative;
            Entry.AnchoredVWAP.Initialize();

            ChandelierGuard.ATR = Strategy.ATR(OptParamsVWAPPB.ChandelierGuardStop.ATRPeriod);

            AdaptiveTrailingStopIndicators.FastEMA = Strategy.EMA(OptParamsVWAPPB.AdaptiveTrailingStop.FastEMAPeriod);
            AdaptiveTrailingStopIndicators.SlowEMA = Strategy.EMA(OptParamsVWAPPB.AdaptiveTrailingStop.SlowEMAPeriod);
            AdaptiveTrailingStopIndicators.ATR = Strategy.ATR(OptParamsVWAPPB.AdaptiveTrailingStop.ATRPeriod);

            TrendStructuralIndicators.EMA = Strategy.EMA(OptParamsVWAPPB.TrendStructuralTrailingStop.EMAPeriod);
            TrendStructuralIndicators.ATR = Strategy.ATR(OptParamsVWAPPB.TrendStructuralTrailingStop.ATRPeriod);

            BiasFilter = new BiasFilter(
                OptParamsVWAPPB.General.TimeWindowTimeZone,
                OptParamsVWAPPB.General.TWAnchorTime,
                OptParamsVWAPPB.ScheduleBiasFilters
            );

            SizingFilter = new SizingFilter(
                OptParamsVWAPPB.General.TimeWindowTimeZone,
                OptParamsVWAPPB.General.TWAnchorTime,
                OptParamsVWAPPB.ScheduleSizingFilters
            );
        }

        public override ChandelierGuardIndicators GetChandelierGuardIndicators() { return ChandelierGuard; }
        public override AdaptiveTrailingStopIndicators GetAdaptiveTrailingStopIndicators() { return AdaptiveTrailingStopIndicators; }
        public override BreakEvenIndicators GetBreakEvenIndicators() { return BreakEven; }
        public override TrendStructuralTrailingIndicators GetTrendStructuralTrailingIndicators() { return TrendStructuralIndicators; }

        public override void Update()
        {
            base.Update();
        }
        #endregion
    }
}
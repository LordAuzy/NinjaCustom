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

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    [StrategyComponentId("IDC-OPNDRV")]
    public class Indicators_OPNDRV : IndicatorsBase
    {
        #region classDefinitions
        public class EntryIndicators
        {
            public ATR ATR { get; set; }
            public EMA FastEMA { get; set; }
            public EMA SlowEMA { get; set; }
            public DAVWAPIndicator AnchoredVWAP { get; set; }
            public DM DM { get; set; } = null;
            public TimeWindowPriceRange OpeningDrive { get; set; } = null;
        }
        #endregion

        #region Properties
        private OptimizationParameters_OPNDRV OptParamsOPNDRV { get { return OptParams as OptimizationParameters_OPNDRV; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        public ChandelierGuardIndicators ChandelierGuard { get; set; } = new ChandelierGuardIndicators();
        public BreakEvenIndicators BreakEven { get; set; } = new BreakEvenIndicators();
        public AdaptiveTrailingStopIndicators AdaptiveTrailingStopIndicators { get; set; } = new AdaptiveTrailingStopIndicators();
        public TrendStructuralTrailingIndicators TrendStructuralIndicators { get; set; } = new TrendStructuralTrailingIndicators();
        public BiasFilter BiasFilter { get; set; }
        public SizingFilter SizingFilter { get; set; }
        #endregion

        public Indicators_OPNDRV(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            BreakEven.ATR = Strategy.ATR(OptParamsOPNDRV.BreakEven.ATRPeriod);

            Entry.ATR = Strategy.ATR(OptParamsOPNDRV.Entry.ATRPeriod);
            Entry.FastEMA = Strategy.EMA(OptParamsOPNDRV.Entry.FastEMAPeriod);
            Entry.SlowEMA = Strategy.EMA(OptParamsOPNDRV.Entry.SlowEMAPeriod);
            Entry.DM = Strategy.DM(14);
            Entry.AnchoredVWAP = Strategy.DAVWAPIndicator("9:30am", "Eastern Standard Time");
            Entry.AnchoredVWAP.StdDevBandCount = 0;
            Entry.AnchoredVWAP.BandMode = VwapBandMode.Cumulative;
            Entry.AnchoredVWAP.Initialize();
            Entry.OpeningDrive = new TimeWindowPriceRange(Strategy, "9:30am", OptParamsOPNDRV.Entry.DriveDuration, "Eastern Standard Time");

            ChandelierGuard.ATR = Strategy.ATR(OptParamsOPNDRV.ChandelierGuardStop.ATRPeriod);

            AdaptiveTrailingStopIndicators.FastEMA = Strategy.EMA(OptParamsOPNDRV.AdaptiveTrailingStop.FastEMAPeriod);
            AdaptiveTrailingStopIndicators.SlowEMA = Strategy.EMA(OptParamsOPNDRV.AdaptiveTrailingStop.SlowEMAPeriod);
            AdaptiveTrailingStopIndicators.ATR = Strategy.ATR(OptParamsOPNDRV.AdaptiveTrailingStop.ATRPeriod);

            TrendStructuralIndicators.EMA = Strategy.EMA(OptParamsOPNDRV.TrendStructuralTrailingStop.EMAPeriod);
            TrendStructuralIndicators.ATR = Strategy.ATR(OptParamsOPNDRV.TrendStructuralTrailingStop.ATRPeriod);

            BiasFilter = new BiasFilter(
                OptParamsOPNDRV.General.TimeWindowTimeZone,
                OptParamsOPNDRV.General.TWAnchorTime,
                OptParamsOPNDRV.ScheduleBiasFilters
            );

            SizingFilter = new SizingFilter(
                OptParamsOPNDRV.General.TimeWindowTimeZone,
                OptParamsOPNDRV.General.TWAnchorTime,
                OptParamsOPNDRV.ScheduleSizingFilters
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
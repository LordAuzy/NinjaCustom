using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.ScheduleFilter;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.Strategies.DAustin.OFVALUEREV_V1
{
    [StrategyComponentId("IDC-OFVALUEREV_V1")]
    public class Indicators_OFVALUEREV_V1 : IndicatorsBase
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
        private OptimizationParameters_OFVALUEREV_V1 OptParamsOFVALUEREV { get { return OptParams as OptimizationParameters_OFVALUEREV_V1; } }
        public EntryIndicators Entry { get; set; } = new EntryIndicators();
        public ChandelierGuardIndicators ChandelierGuard { get; set; } = new ChandelierGuardIndicators();
        public BreakEvenIndicators BreakEven { get; set; } = new BreakEvenIndicators();
        public AdaptiveTrailingStopIndicators AdaptiveTrailingStopIndicators { get; set; } = new AdaptiveTrailingStopIndicators();
        public TrendStructuralTrailingIndicators TrendStructuralIndicators { get; set; } = new TrendStructuralTrailingIndicators();
        public BiasFilter BiasFilter { get; set; }
        public SizingFilter SizingFilter { get; set; }
        public OrderFlowVWAP SessionVWAP { get; set; }
        public OrderFlowVolumeProfile SessionVolumeProfile { get; set; }
        public DAMNQRegimeFilter RegimeFilter { get; set; }
        public DAPriorSessionValueArea PriorValue { get; set; }
        public DataSeries_OFVALUEREV_V1 DataSeriesIndicies { get; set; }
        #endregion

        public Indicators_OFVALUEREV_V1(StratBase strat) : base(strat)
        {

        }

        #region Overrides
        public override void Initialize()
        {
            base.Initialize();

            BreakEven.ATR = Strategy.ATR(OptParamsOFVALUEREV.BreakEven.ATRPeriod);

            Entry.ATR = Strategy.ATR(OptParamsOFVALUEREV.Entry.ATRPeriod);
            Entry.FastEMA = Strategy.EMA(OptParamsOFVALUEREV.Entry.FastEMAPeriod);
            Entry.SlowEMA = Strategy.EMA(OptParamsOFVALUEREV.Entry.SlowEMAPeriod);
            Entry.DM = Strategy.DM(OptParamsOFVALUEREV.Entry.DMPeriod);
            Entry.AnchoredVWAP = Strategy.DAVWAPIndicator("9:30am", "Eastern Standard Time");
            Entry.AnchoredVWAP.StdDevBandCount = OptParamsOFVALUEREV.Entry.VWAPStdDevBandCount;
            Entry.AnchoredVWAP.BandMode = VwapBandMode.Cumulative;
            Entry.AnchoredVWAP.Initialize();

            ChandelierGuard.ATR = Strategy.ATR(OptParamsOFVALUEREV.ChandelierGuardStop.ATRPeriod);

            AdaptiveTrailingStopIndicators.FastEMA = Strategy.EMA(OptParamsOFVALUEREV.AdaptiveTrailingStop.FastEMAPeriod);
            AdaptiveTrailingStopIndicators.SlowEMA = Strategy.EMA(OptParamsOFVALUEREV.AdaptiveTrailingStop.SlowEMAPeriod);
            AdaptiveTrailingStopIndicators.ATR = Strategy.ATR(OptParamsOFVALUEREV.AdaptiveTrailingStop.ATRPeriod);

            TrendStructuralIndicators.EMA = Strategy.EMA(OptParamsOFVALUEREV.TrendStructuralTrailingStop.EMAPeriod);
            TrendStructuralIndicators.ATR = Strategy.ATR(OptParamsOFVALUEREV.TrendStructuralTrailingStop.ATRPeriod);

            BiasFilter = new BiasFilter(
                OptParamsOFVALUEREV.General.TimeWindowTimeZone,
                OptParamsOFVALUEREV.General.TWAnchorTime,
                OptParamsOFVALUEREV.ScheduleBiasFilters
            );

            SizingFilter = new SizingFilter(
                OptParamsOFVALUEREV.General.TimeWindowTimeZone,
                OptParamsOFVALUEREV.General.TWAnchorTime,
                OptParamsOFVALUEREV.ScheduleSizingFilters
            );


            SessionVWAP = Strategy.OrderFlowVWAP(
                VWAPResolution.Standard,
                TradingHours.String2TradingHours("CME US Index Futures RTH"),
                VWAPStandardDeviations.Three, 1.0, 2.0, 3.0);
            RegimeFilter = Strategy.DAMNQRegimeFilter(
                OptParamsOFVALUEREV.OFRF_LookbackDays, 
                OptParamsOFVALUEREV.OFRF_MaxSlopeTicks,
                OptParamsOFVALUEREV.OFRF_TrendEntryConfirmationBars,
                OptParamsOFVALUEREV.OFRF_InRangeTrendSlopeMultiplier);
            RegimeFilter.SessionVWAP = SessionVWAP;

            TradingHours rth = TradingHours.String2TradingHours("CME US Index Futures RTH");
            int rthPrimaryIndex = DataSeriesIndicies.GetSeriesIndex(DataSeries_OFVALUEREV_V1.Name.RthPrimary);
            SessionVolumeProfile =
                Strategy.OrderFlowVolumeProfile(
                    Strategy.Closes[rthPrimaryIndex],
                    MarketProfileType.Volume,
                    MarketProfilePeriod.Sessions,
                    1,
                    rth,
                    MarketProfileResolution.Minute,
                    68,
                    0);
            SessionVolumeProfile.TicksPerLevel = 1;

            PriorValue = Strategy.DAPriorSessionValueArea(Strategy.Closes[rthPrimaryIndex]);
            PriorValue.SessionVolumeProfile = SessionVolumeProfile; 
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
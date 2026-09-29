#region Namespaces
using DA.NinjaTrader.Types;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
#endregion

namespace DA.NinjaTrader.Types
{
    public enum MarketRegime
    {
        BullishTrend,
        BearishTrend,
        RotationalChop,
        Transitioning
    }
}

namespace NinjaTrader.NinjaScript.Indicators
{
    public class DAMNQRegimeFilter : Indicator
    {
        #region DAProps
        public OrderFlowVWAP SessionVWAP {  get; set; }
        private Series<int> regimeHistory;

        // Historical regime ribbon colors. The ribbon is rendered in screen coordinates
        // at the bottom of the price panel, so it does not interfere with VWAP bands.
        private Brush bullishRibbonBrush = Brushes.LimeGreen;
        private Brush bearishRibbonBrush = Brushes.OrangeRed;
        private Brush chopRibbonBrush = Brushes.DodgerBlue;
        private Brush transitioningRibbonBrush = Brushes.Goldenrod;

        private const float RegimeRibbonHeight = 8.0f;

        // Initial-cut regime hysteresis.
        //
        // A trend regime must remain valid for several consecutive 1-minute bars
        // before trend trading is enabled. Non-trend states disable trend trading
        // immediately. This is intentionally asymmetric: it is easy to turn the
        // strategy OFF in uncertain conditions and harder to turn it back ON.
        //
        // Keep these as constants for the first validation pass so NinjaTrader's
        // generated indicator factory signature does not change. If the idea proves
        // useful, promote them to optimization parameters later.
//        private const int TrendEntryConfirmationBars = 5;
        private const int TrendEntryConfirmationBars = 3;
        private const double InRangeTrendSlopeMultiplier = 1.50;

        private DA.NinjaTrader.Types.MarketRegime _currentRegime;
        private DA.NinjaTrader.Types.MarketRegime _rawRegime;
        private DA.NinjaTrader.Types.MarketRegime _pendingTrendRegime;
        private int _pendingTrendBars;

        public DA.NinjaTrader.Types.MarketRegime CurrentRegime
        {
            get
            {
                Update();
                return _currentRegime;
            }
        }

        // Raw one-bar classification before hysteresis is applied.
        // Useful for logging/telemetry while validating the filter.
        [Browsable(false)]
        public DA.NinjaTrader.Types.MarketRegime RawRegime
        {
            get
            {
                Update();
                return _rawRegime;
            }
        }

        // This is the value the strategy should ultimately care about.
        // Direction can continue to come from the existing VWAP pullback logic.
        [Browsable(false)]
        public bool TrendTradingAllowed
        {
            get
            {
                Update();
                return IsTrendRegime(_currentRegime);
            }
        }

        [Browsable(false)]
        public int PendingTrendBars
        {
            get
            {
                Update();
                return _pendingTrendBars;
            }
        }
        #endregion

        #region Ninjascript Properties
        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Multi-Day Lookback (Days)", GroupName = "Parameters", Order = 1)]
        public int LookbackDays { get; set; }

        [NinjaScriptProperty]
        [Range(1.0, 20.0)]
        [Display(Name = "Max Slope Ticks (Chop Threshold)", GroupName = "Parameters", Order = 2)]
        public double MaxSlopeTicks { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Multi-Day Regime Filter for MNQ Intraday Switchboard.";
                Name = "MNQ_RegimeFilter";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;

                LookbackDays = 3;   // 3-day rolling window
                MaxSlopeTicks = 8.0; // VWAP slope within 8 ticks = Chop

                _currentRegime = DA.NinjaTrader.Types.MarketRegime.Transitioning;
                _rawRegime = DA.NinjaTrader.Types.MarketRegime.Transitioning;
                _pendingTrendRegime = DA.NinjaTrader.Types.MarketRegime.Transitioning;
                _pendingTrendBars = 0;
            }
            else if (State == State.Configure)
            {
                // Add Daily Data Series (BarsInProgress = 1) for Multi-Day Context
                AddDataSeries(BarsPeriodType.Day, 1);
            }
            else if (State == State.DataLoaded)
            {
                regimeHistory = new Series<int>(this, MaximumBarsLookBack.Infinite);
            }
        }

        protected override void OnBarUpdate()
        {
            // Do not evaluate on the Daily BarsInProgress series directly
            if (BarsInProgress != 0) return;

            if (CurrentBar < 20 || CurrentBars[1] < LookbackDays)
            {
                _rawRegime = MarketRegime.Transitioning;
                _currentRegime = MarketRegime.Transitioning;
                ResetPendingTrend();
                regimeHistory[0] = (int)_currentRegime;
                return;
            }

            // -------------------------------------------------------------
            // STEP 1: CALCULATE MULTI-DAY STRUCTURAL BOUNDARIES
            // -------------------------------------------------------------
            double multiDayHigh = double.MinValue;
            double multiDayLow = double.MaxValue;

            // Scan the last N completed daily sessions (BarsInProgress 1)
            for (int i = 1; i <= LookbackDays; i++)
            {
                multiDayHigh = Math.Max(multiDayHigh, Highs[1][i]);
                multiDayLow = Math.Min(multiDayLow, Lows[1][i]);
            }

            // -------------------------------------------------------------
            // STEP 2: INTRADAY VWAP & SLOPE METRICS
            // -------------------------------------------------------------
            double currentVWAP = SessionVWAP.VWAP[0];
            double vwapSlopeTicks = Math.Abs(currentVWAP - SessionVWAP.VWAP[10]) / TickSize;

            // Check if price is inside or outside the multi-day bracket
            // Use the current DAILY bar open, not the current 1-minute bar open.
            // The multi-day boundaries above intentionally use completed daily bars [1..LookbackDays],
            // while [0] is the current daily bar.
            double currentDayOpen = Opens[1][0];
            bool openInsideMultiDayRange = currentDayOpen < multiDayHigh && currentDayOpen > multiDayLow;
            bool priceInsideMultiDayRange = Close[0] < multiDayHigh && Close[0] > multiDayLow;
            bool priceAboveVWAP = Close[0] > currentVWAP;
            bool priceBelowVWAP = Close[0] < currentVWAP;

            // When price is still inside the prior multi-day bracket, require a
            // materially steeper VWAP slope before calling the environment a trend.
            // This creates a dead-band:
            //     <= MaxSlopeTicks                      -> chop candidate
            //     MaxSlopeTicks .. strict threshold    -> transitioning
            //     > strict threshold                   -> trend candidate
            //
            // Once price actually leaves the bracket, return to the normal threshold.
            double trendSlopeThresholdTicks =
                priceInsideMultiDayRange
                    ? MaxSlopeTicks * InRangeTrendSlopeMultiplier
                    : MaxSlopeTicks;

            // -------------------------------------------------------------
            // STEP 3: RAW REGIME CLASSIFICATION
            // -------------------------------------------------------------

            MarketRegime rawRegime;

            // Preserve the original structural idea: an open inside the prior
            // multi-day bracket plus a flat VWAP is rotational/chop.
            if (openInsideMultiDayRange &&
                priceInsideMultiDayRange &&
                vwapSlopeTicks <= MaxSlopeTicks)
            {
                rawRegime = MarketRegime.RotationalChop;
            }
            // Trend candidates must clear the stricter threshold while price
            // remains inside the multi-day bracket.
            else if (priceAboveVWAP &&
                     currentVWAP > SessionVWAP.VWAP[10] &&
                     vwapSlopeTicks > trendSlopeThresholdTicks)
            {
                rawRegime = MarketRegime.BullishTrend;
            }
            else if (priceBelowVWAP &&
                     currentVWAP < SessionVWAP.VWAP[10] &&
                     vwapSlopeTicks > trendSlopeThresholdTicks)
            {
                rawRegime = MarketRegime.BearishTrend;
            }
            else
            {
                rawRegime = MarketRegime.Transitioning;
            }

            // -------------------------------------------------------------
            // STEP 4: APPLY ASYMMETRIC HYSTERESIS / PERSISTENCE
            // -------------------------------------------------------------
            //
            // Non-trend states disable trend trading immediately.
            // A trend must persist for TrendEntryConfirmationBars consecutive
            // 1-minute bars before CurrentRegime becomes Bullish/Bearish.
            //
            // This directly addresses the failure mode where a short-lived VWAP
            // slope burst inside a rotational day temporarily enabled VWAPPB.
            ApplyRegimeHysteresis(rawRegime);

            // Store the stable regime for this 1-minute bar so OnRender() can draw
            // exactly the regime that the strategy was allowed to act on.
            regimeHistory[0] = (int)_currentRegime;
        }


        private void ApplyRegimeHysteresis(MarketRegime rawRegime)
        {
            _rawRegime = rawRegime;

            // Any loss of a trend condition shuts trend trading off immediately.
            // Re-entry into a trend state must earn its way back in.
            if (!IsTrendRegime(rawRegime))
            {
                _currentRegime = rawRegime;
                ResetPendingTrend();
                return;
            }

            // Already in this confirmed trend -- nothing more to prove.
            if (_currentRegime == rawRegime)
            {
                ResetPendingTrend();
                return;
            }

            // If a confirmed trend attempts to reverse direction, stop trading
            // immediately while the opposite direction earns confirmation.
            if (IsTrendRegime(_currentRegime) && _currentRegime != rawRegime)
                _currentRegime = MarketRegime.Transitioning;

            if (_pendingTrendRegime == rawRegime)
            {
                _pendingTrendBars++;
            }
            else
            {
                _pendingTrendRegime = rawRegime;
                _pendingTrendBars = 1;
            }

            if (_pendingTrendBars >= TrendEntryConfirmationBars)
            {
                _currentRegime = rawRegime;
                ResetPendingTrend();
            }
        }


        private void ResetPendingTrend()
        {
            _pendingTrendRegime = MarketRegime.Transitioning;
            _pendingTrendBars = 0;
        }


        private bool IsTrendRegime(MarketRegime regime)
        {
            return regime == MarketRegime.BullishTrend ||
                   regime == MarketRegime.BearishTrend;
        }


        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            if (ChartBars == null || regimeHistory == null || RenderTarget == null)
                return;

            int fromIndex = Math.Max(ChartBars.FromIndex, 0);
            int toIndex = Math.Min(ChartBars.ToIndex, Bars.Count - 1);

            if (fromIndex > toIndex)
                return;

            DrawHistoricalRibbon(chartControl, chartScale, fromIndex, toIndex);
            DrawRegimeHUD();
        }

        private void DrawHistoricalRibbon(
            ChartControl chartControl, 
            ChartScale chartScale,
            int fromIndex = -1, 
            int toIndex = -1)
        {
            float ribbonTop = (float)(ChartPanel.Y + ChartPanel.H - RegimeRibbonHeight);

            using (SharpDX.Direct2D1.Brush bullishDx = bullishRibbonBrush.ToDxBrush(RenderTarget))
            using (SharpDX.Direct2D1.Brush bearishDx = bearishRibbonBrush.ToDxBrush(RenderTarget))
            using (SharpDX.Direct2D1.Brush chopDx = chopRibbonBrush.ToDxBrush(RenderTarget))
            using (SharpDX.Direct2D1.Brush transitioningDx = transitioningRibbonBrush.ToDxBrush(RenderTarget))
            {
                for (int barIndex = fromIndex; barIndex <= toIndex; barIndex++)
                {
                    if (!regimeHistory.IsValidDataPointAt(barIndex))
                        continue;

                    DA.NinjaTrader.Types.MarketRegime regime =
                        (DA.NinjaTrader.Types.MarketRegime)
                            regimeHistory.GetValueAt(barIndex);

                    SharpDX.Direct2D1.Brush dxBrush;

                    switch (regime)
                    {
                        case DA.NinjaTrader.Types.MarketRegime.BullishTrend:
                            dxBrush = bullishDx;
                            break;

                        case DA.NinjaTrader.Types.MarketRegime.BearishTrend:
                            dxBrush = bearishDx;
                            break;

                        case DA.NinjaTrader.Types.MarketRegime.RotationalChop:
                            dxBrush = chopDx;
                            break;

                        default:
                            dxBrush = transitioningDx;
                            break;
                    }

                    float xCenter =
                        chartControl.GetXByBarIndex(ChartBars, barIndex);

                    float left;
                    float right;

                    if (barIndex > fromIndex)
                    {
                        float previousX =
                            chartControl.GetXByBarIndex(ChartBars, barIndex - 1);

                        left = (previousX + xCenter) * 0.5f;
                    }
                    else
                    {
                        left = xCenter - (float)chartControl.BarWidth;
                    }

                    if (barIndex < toIndex)
                    {
                        float nextX =
                            chartControl.GetXByBarIndex(ChartBars, barIndex + 1);

                        right = (xCenter + nextX) * 0.5f;
                    }
                    else
                    {
                        right = xCenter + (float)chartControl.BarWidth;
                    }

                    if (right <= left)
                        continue;

                    RenderTarget.FillRectangle(
                        new SharpDX.RectangleF(
                            left,
                            ribbonTop,
                            right - left,
                            RegimeRibbonHeight),
                        dxBrush);
                }
            }
        }

        private void DrawRegimeHUD()
        {
            string labelText = $"REGIME: {_currentRegime.ToString().ToUpper()}";

            Brush bgBrush;

            switch (_currentRegime)
            {
                case DA.NinjaTrader.Types.MarketRegime.BullishTrend:
                    bgBrush = Brushes.DarkGreen;
                    break;

                case DA.NinjaTrader.Types.MarketRegime.BearishTrend:
                    bgBrush = Brushes.DarkRed;
                    break;

                case DA.NinjaTrader.Types.MarketRegime.RotationalChop:
                    bgBrush = Brushes.DarkGoldenrod;
                    break;

                default:
                    bgBrush = Brushes.SlateGray;
                    break;
            }

            const float width = 205.0f;
            const float height = 24.0f;
            const float margin = 8.0f;

            // Bottom-right, immediately above the regime ribbon.
            float x =
                (float)(ChartPanel.X + ChartPanel.W)
                - width
                - margin;

            float y =
                (float)(ChartPanel.Y + ChartPanel.H)
                - RegimeRibbonHeight
                - height
                - margin;

            using (SharpDX.Direct2D1.Brush backgroundDx =
                bgBrush.ToDxBrush(RenderTarget))
            using (SharpDX.Direct2D1.Brush textDx =
                Brushes.White.ToDxBrush(RenderTarget))
            using (SharpDX.DirectWrite.TextFormat textFormat =
                new SharpDX.DirectWrite.TextFormat(
                    Core.Globals.DirectWriteFactory,
                    "Consolas",
                    12.0f))
            {
                RenderTarget.FillRectangle(
                    new SharpDX.RectangleF(
                        x,
                        y,
                        width,
                        height),
                    backgroundDx);

                RenderTarget.DrawText(
                    labelText,
                    textFormat,
                    new SharpDX.RectangleF(
                        x + 6,
                        y + 3,
                        width - 12,
                        height - 6),
                    textDx);
            }
        }

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DAMNQRegimeFilter[] cacheDAMNQRegimeFilter;
		public DAMNQRegimeFilter DAMNQRegimeFilter(int lookbackDays, double maxSlopeTicks)
		{
			return DAMNQRegimeFilter(Input, lookbackDays, maxSlopeTicks);
		}

		public DAMNQRegimeFilter DAMNQRegimeFilter(ISeries<double> input, int lookbackDays, double maxSlopeTicks)
		{
			if (cacheDAMNQRegimeFilter != null)
				for (int idx = 0; idx < cacheDAMNQRegimeFilter.Length; idx++)
					if (cacheDAMNQRegimeFilter[idx] != null && cacheDAMNQRegimeFilter[idx].LookbackDays == lookbackDays && cacheDAMNQRegimeFilter[idx].MaxSlopeTicks == maxSlopeTicks && cacheDAMNQRegimeFilter[idx].EqualsInput(input))
						return cacheDAMNQRegimeFilter[idx];
			return CacheIndicator<DAMNQRegimeFilter>(new DAMNQRegimeFilter(){ LookbackDays = lookbackDays, MaxSlopeTicks = maxSlopeTicks }, input, ref cacheDAMNQRegimeFilter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DAMNQRegimeFilter DAMNQRegimeFilter(int lookbackDays, double maxSlopeTicks)
		{
			return indicator.DAMNQRegimeFilter(Input, lookbackDays, maxSlopeTicks);
		}

		public Indicators.DAMNQRegimeFilter DAMNQRegimeFilter(ISeries<double> input , int lookbackDays, double maxSlopeTicks)
		{
			return indicator.DAMNQRegimeFilter(input, lookbackDays, maxSlopeTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DAMNQRegimeFilter DAMNQRegimeFilter(int lookbackDays, double maxSlopeTicks)
		{
			return indicator.DAMNQRegimeFilter(Input, lookbackDays, maxSlopeTicks);
		}

		public Indicators.DAMNQRegimeFilter DAMNQRegimeFilter(ISeries<double> input , int lookbackDays, double maxSlopeTicks)
		{
			return indicator.DAMNQRegimeFilter(input, lookbackDays, maxSlopeTicks);
		}
	}
}

#endregion

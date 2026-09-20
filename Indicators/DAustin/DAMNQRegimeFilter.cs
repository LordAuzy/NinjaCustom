#region Namespaces
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
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
        private OrderFlowVWAP sessionVWAP;

        private DA.NinjaTrader.Types.MarketRegime _currentRegime;
        public DA.NinjaTrader.Types.MarketRegime CurrentRegime 
        { 
            get
            {
                Update();
                return _currentRegime;
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
            }
            else if (State == State.Configure)
            {
                // Add Daily Data Series (BarsInProgress = 1) for Multi-Day Context
                AddDataSeries(BarsPeriodType.Day, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Instantiate Order Flow VWAP on Primary 1-Min Series
                sessionVWAP = OrderFlowVWAP(VWAPResolution.Standard, Bars.TradingHours, VWAPStandardDeviations.Three, 1.0, 2.0, 3.0);
            }
        }

        protected override void OnBarUpdate()
        {
            // Do not evaluate on the Daily BarsInProgress series directly
            if (BarsInProgress != 0) return;
            if (CurrentBar < 20 || CurrentBars[1] < LookbackDays) return;

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
            double currentVWAP = sessionVWAP.VWAP[0];
            double vwapSlopeTicks = Math.Abs(currentVWAP - sessionVWAP.VWAP[10]) / TickSize;

            // Check if price is inside or outside the multi-day bracket
            // Use the current DAILY bar open, not the current 1-minute bar open.
            // The multi-day boundaries above intentionally use completed daily bars [1..LookbackDays],
            // while [0] is the current daily bar.
            double currentDayOpen = Opens[1][0];
            bool openInsideMultiDayRange = currentDayOpen < multiDayHigh && currentDayOpen > multiDayLow;
            bool priceAboveVWAP = Close[0] > currentVWAP;
            bool priceBelowVWAP = Close[0] < currentVWAP;

            // -------------------------------------------------------------
            // STEP 3: REGIME EVALUATION LOGIC
            // -------------------------------------------------------------

            // CHOP CONDITION: Opened inside prior value AND VWAP is flat
            if (openInsideMultiDayRange && vwapSlopeTicks <= MaxSlopeTicks)
            {
                _currentRegime = DA.NinjaTrader.Types.MarketRegime.RotationalChop;
            }
            // BULLISH TREND CONDITION: Price trading above VWAP with steep positive slope
            else if (priceAboveVWAP && (currentVWAP > sessionVWAP.VWAP[10]) && vwapSlopeTicks > MaxSlopeTicks)
            {
                _currentRegime = DA.NinjaTrader.Types.MarketRegime.BullishTrend;
            }
            // BEARISH TREND CONDITION: Price trading below VWAP with steep negative slope
            else if (priceBelowVWAP && (currentVWAP < sessionVWAP.VWAP[10]) && vwapSlopeTicks > MaxSlopeTicks)
            {
                _currentRegime = DA.NinjaTrader.Types.MarketRegime.BearishTrend;
            }
            else
            {
                _currentRegime = DA.NinjaTrader.Types.MarketRegime.Transitioning;
            }

            // -------------------------------------------------------------
            // STEP 4: VISUAL HUD DASHBOARD DISPLAY
            // -------------------------------------------------------------
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            string labelText = string.Format("REGIME: {0}", _currentRegime.ToString().ToUpper());
            Brush bgBrush = Brushes.DimGray;

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
                case DA.NinjaTrader.Types.MarketRegime.Transitioning:
                    bgBrush = Brushes.SlateGray;
                    break;
            }

            Draw.TextFixed(
                owner: this,
                tag: "RegimeHUD",
                text: labelText,
                textPosition: TextPosition.TopRight,
                textBrush: Brushes.White,
                font: new Gui.Tools.SimpleFont("Consolas", 12),
                outlineBrush: Brushes.Transparent,
                areaBrush: bgBrush,
                areaOpacity: 85
            );
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

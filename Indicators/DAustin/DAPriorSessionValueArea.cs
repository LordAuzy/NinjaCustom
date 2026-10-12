#region Using declarations
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.ChartStyles;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{	
    public class DAPriorSessionValueArea : Indicator
    {
        private class ProfileSnapshot
        {
            private bool _completed = false;
            public bool Completed { get { return _completed; } }
            public DateTime SessionDate { get; set; }
            public double VAH { get; set; }
            public double VAL { get; set; }
            public double POC { get; set; }
            public double High { get; set; }
            public double Low { get; set; }

            #region Constructors
            public ProfileSnapshot() 
            {
                Reset();                   
            }
            #endregion

            public void Reset()
            {
                _completed = false;
                SessionDate = DateTime.MinValue;
                VAH = double.NaN;
                VAL = double.NaN;
                POC = double.NaN;
                High = double.NaN;
                Low = double.NaN;
            }

            public bool Complete(
                double vah,
                double val,
                double poc)
            {
                if (!IsFinite(vah) ||
                    !IsFinite(val) ||
                    !IsFinite(poc) ||
                    !IsFinite(High) ||
                    !IsFinite(Low))
                {
                    return false;
                }

                if (vah < val ||
                    poc < val ||
                    poc > vah ||
                    High < vah ||
                    Low > val)
                {
                    return false;
                }

                VAH = vah;
                VAL = val;
                POC = poc;

                _completed = true;
                return _completed;
            }

            public void UpdateHighLow(double high, double low)
            {
                if (double.IsNaN(High) || high > High)
                {
                    High = high;
                }

                if (double.IsNaN(Low) || low < Low)
                {
                    Low = low;
                }
            }

            public double ValueAreaWidth
            {
                get
                {
                    double width = double.NaN;
                    if (Completed)
                    {
                        width = VAH - VAL;
                    }
                    return width;
                }
            }

            public double ValueAreaMidpoint
            {
                get
                {
                    double midpoint = double.NaN;
                    if (Completed)
                    { 
                        midpoint = (VAH + VAL) / 2.0;
                    }
                    return midpoint;
                }
            }

            public double Range
            {
                get
                {
                    double range = double.NaN;
                    if (Completed)
                    {
                        range = High - Low;
                    }
                    return range;
                }
            }

            public double POCPositionPct
            {
                get
                {
                    double pct = double.NaN;
                    if (Completed)
                    {
                        double width = VAH - VAL;

                        if (width > 0)
                        {
                            pct = (POC - VAL) / width;
                        }
                    }
                    return pct;
                }
            }
        }

        private ProfileSnapshot SessionSnapshot = new ProfileSnapshot();
        private ProfileSnapshot PriorSessionSnapshot = null;
        private SessionIterator rthSessionIterator = null;
        private SimpleFont labelFont = new SimpleFont("Arial", 12);

        // Exposed user property for session template selection
        [NinjaScriptProperty]
        [Display(Name = "RTH Trading Hours Template", Description = "Select the RTH session template used to identify session boundaries.", Order = 1, GroupName = "Parameters")]
        [TypeConverter(typeof(TradingHoursDataConverter))]
        public string RTHTradingHoursName { get; set; }

        // ------------------------------------------------------------
        // Runtime dependency
        // ------------------------------------------------------------
        private OrderFlowVolumeProfile _sessionVolumeProfile;
        [Browsable(false)]
        [XmlIgnore]
        public OrderFlowVolumeProfile SessionVolumeProfile
        {
            get { return _sessionVolumeProfile; }
            set { _sessionVolumeProfile = value; }
        }

        // ------------------------------------------------------------
        // Current RTH session state
        // ------------------------------------------------------------

        private bool haveActiveSession;
        private DateTime activeSessionDate;

        // ------------------------------------------------------------
        // Completed prior-session snapshot
        // ------------------------------------------------------------

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description =
                    "Maintains a stable snapshot of the completed prior " +
                    "RTH session Volume Profile value area.";

                Name = "DAPriorSessionValueArea";

                Calculate = Calculate.OnBarClose;

                IsOverlay = true;
                DrawOnPricePanel = true;
                DisplayInDataBox = true;
                PaintPriceMarkers = true;
                IsSuspendedWhileInactive = true;
                BarsRequiredToPlot = 1;

                RTHTradingHoursName = "CME US Index Futures RTH";

                // Stable prior-session levels.
                AddPlot(
                    new Stroke(Brushes.LimeGreen, 1),
                    PlotStyle.Line,
                    "PriorVAHPlot");

                AddPlot(
                    new Stroke(Brushes.LimeGreen, 1),
                    PlotStyle.Line,
                    "PriorPOCPlot");

                AddPlot(
                    new Stroke(Brushes.LimeGreen, 1),
                    PlotStyle.Line,
                    "PriorVALPlot");


                // Full prior-session RTH range.

                AddPlot(
                    new Stroke(Brushes.LimeGreen, 2),
                    PlotStyle.Dot,
                    "PriorProfileHighPlot");

                AddPlot(
                    new Stroke(Brushes.LimeGreen, 2),
                    PlotStyle.Dot,
                    "PriorProfileLowPlot");
            }
            else if (State == State.DataLoaded)
            {
                if (!string.IsNullOrEmpty(RTHTradingHoursName))
                {
                    TradingHours rthHours = TradingHours.Get(RTHTradingHoursName);
                    if (rthHours != null)
                        rthSessionIterator = new SessionIterator(rthHours);
                }
                SessionSnapshot.Reset();
            }
        }
        protected override void OnBarUpdate()
        {
            if (CurrentBar < 0 || BarsInProgress != 0)
                return;

            bool inRTH = IsInRTHSession(Time[0]);
            if (!haveActiveSession)
            {
                if (inRTH)
                    BeginSession();
            }
            else
            {
                if (inRTH)
                {
                    // Safety: never allow two RTH dates to merge.
                    if (Time[0].Date != SessionSnapshot.SessionDate)
                    {
                        // Previous session failed to complete.
                        // Drop it rather than contaminate the next one.
                        BeginSession();
                    }
                    else
                    {
                        SessionSnapshot.UpdateHighLow(High[0], Low[0]);
                    }
                }
                else
                {
                    CompleteSession();
                }
            }

            PublishPlots(inRTH);
        }

        // ============================================================
        // Session handling
        // ============================================================

        private void BeginSession()
        {
            haveActiveSession = true;
            SessionSnapshot = new ProfileSnapshot();
            SessionSnapshot.SessionDate = Time[0].Date;
            SessionSnapshot.UpdateHighLow(High[0], Low[0]);
        }

        private void CompleteSession()
        {
            if (_sessionVolumeProfile == null)
                return;

            double vah = _sessionVolumeProfile.DevelopingValueAreaHigh[0];
            double val = _sessionVolumeProfile.DevelopingValueAreaLow[0];
            double poc = _sessionVolumeProfile.DevelopingPoc[0];

            if (!SessionSnapshot.Complete(vah, val, poc))
            {
                return;
            }

            PriorSessionSnapshot = SessionSnapshot;
            Print("Completed prior session snapshot for " + PriorSessionSnapshot.SessionDate.ToShortDateString() +
                ": VAH=" + PriorSessionSnapshot.VAH.ToString("F2") +
                ", VAL=" + PriorSessionSnapshot.VAL.ToString("F2") +
                ", POC=" + PriorSessionSnapshot.POC.ToString("F2") +
                ", High=" + PriorSessionSnapshot.High.ToString("F2") +
                ", Low=" + PriorSessionSnapshot.Low.ToString("F2"));

            SessionSnapshot = new ProfileSnapshot();
            haveActiveSession = false;
        }

        private (bool inSession, bool isLastBar) GetRTHSessionState(DateTime barTime)
        {
            if (rthSessionIterator == null)
                return (false, false);

            // Advance iterator to the session matching or following barTime
            rthSessionIterator.GetNextSession(barTime, true);

            // Check if barTime falls within the active session boundaries
            bool inSession = rthSessionIterator.IsInSession(barTime, true, true);

            // Bar time equals ActualSessionEnd on the final bar of the session (e.g., 16:00:00 EST for 15:59-16:00 bar)
            bool isLastBar = inSession && (barTime == rthSessionIterator.ActualSessionEnd);

            return (inSession, isLastBar);
        }

        private bool IsInRTHSession(DateTime barTime)
        {
            var (inRTH, isLastRTHBar) = GetRTHSessionState(barTime);

            return inRTH;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        // ============================================================
        // Plot publishing
        // ============================================================
        private void PublishPlots(bool inRTH)
        {
            if (PriorSessionSnapshot == null || !PriorSessionSnapshot.Completed || !inRTH)
            {
                ClearPlots();
                RemovePriorValueLabels();
                return;
            }

            PriorVAHPlot[0] = PriorSessionSnapshot.VAH;
            PriorPOCPlot[0] = PriorSessionSnapshot.POC;
            PriorVALPlot[0] = PriorSessionSnapshot.VAL;
            PriorProfileHighPlot[0] = PriorSessionSnapshot.High;
            PriorProfileLowPlot[0] = PriorSessionSnapshot.Low;
            DrawPriorValueLabels();
        }

        private void DrawPriorValueLabels()
        {
            if (PriorSessionSnapshot == null || !PriorSessionSnapshot.Completed)
            {
                RemovePriorValueLabels();
            }

            DrawPriorLabel( "PriorVAHLabel",
                $"Prior VAH {PriorSessionSnapshot.VAH:F2}", 
                PriorSessionSnapshot.VAH);

            DrawPriorLabel( "PriorPOCLabel",
                $"Prior POC {PriorSessionSnapshot.POC:F2}", 
                PriorSessionSnapshot.POC);

            DrawPriorLabel( "PriorVALLabel",
                $"Prior VAL {PriorSessionSnapshot.VAL:F2}", 
                PriorSessionSnapshot.VAL);

            DrawPriorLabel( "PriorProfileHighLabel",
                $"Prior High {PriorSessionSnapshot.High:F2}", 
                PriorSessionSnapshot.High);

            DrawPriorLabel( "PriorProfileLowLabel", 
                            $"Prior Low {PriorSessionSnapshot.Low:F2}", 
                            PriorSessionSnapshot.Low);
        }

        private void DrawPriorLabel(
            string tag, 
            string text,
            double yValue)
        {
            Draw.Text(this, tag, false,
                text, 0,
                yValue, 0,
                Brushes.LimeGreen, labelFont, TextAlignment.Left,
                Brushes.Transparent, Brushes.Transparent, 0);
        }

        private void ClearPlots()
        {
            PriorVAHPlot[0] = double.NaN;
            PriorPOCPlot[0] = double.NaN;
            PriorVALPlot[0] = double.NaN;
            PriorProfileHighPlot[0] = double.NaN;
            PriorProfileLowPlot[0] = double.NaN;
        }

        private void RemovePriorValueLabels()
        {
            RemoveDrawObject("PriorVAHLabel");
            RemoveDrawObject("PriorPOCLabel");
            RemoveDrawObject("PriorVALLabel");
            RemoveDrawObject("PriorProfileHighLabel");
            RemoveDrawObject("PriorProfileLowLabel");
        }

        // ============================================================
        // Stable prior-session values
        // ============================================================

        [Browsable(false)]
        public bool IsReady
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.Completed : false;
            }
        }


        [Browsable(false)]
        public DateTime ProfileSessionDate
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.SessionDate : DateTime.MinValue;
            }
        }


        [Browsable(false)]
        public double PriorVAH
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.VAH : double.NaN;
            }
        }


        [Browsable(false)]
        public double PriorVAL
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.VAL : double.NaN;
            }
        }


        [Browsable(false)]
        public double PriorPOC
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.POC : double.NaN;
            }
        }


        [Browsable(false)]
        public double PriorProfileHigh
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.High : double.NaN;
            }
        }


        [Browsable(false)]
        public double PriorProfileLow
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.Low : double.NaN;
            }
        }


        // ============================================================
        // Derived profile structure
        // ============================================================

        [Browsable(false)]
        public double ValueAreaWidth
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.ValueAreaWidth : double.NaN;
            }
        }


        [Browsable(false)]
        public double ValueAreaMidpoint
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.ValueAreaMidpoint : double.NaN;
            }
        }


        [Browsable(false)]
        public double ProfileRange
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.Range : double.NaN;
            }
        }


        [Browsable(false)]
        public double POCPositionPct
        {
            get
            {
                Update();
                return PriorSessionSnapshot != null ? PriorSessionSnapshot.POCPositionPct : double.NaN;
            }
        }


        // ============================================================
        // Plot series
        // ============================================================
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorVAHPlot { get { return Values[0]; } }    

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorPOCPlot { get { return Values[1]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorVALPlot { get { return Values[2]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorProfileHighPlot { get { return Values[3]; } }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> PriorProfileLowPlot { get { return Values[4]; } }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DAPriorSessionValueArea[] cacheDAPriorSessionValueArea;
		public DAPriorSessionValueArea DAPriorSessionValueArea()
		{
			return DAPriorSessionValueArea(Input);
		}

		public DAPriorSessionValueArea DAPriorSessionValueArea(ISeries<double> input)
		{
			if (cacheDAPriorSessionValueArea != null)
				for (int idx = 0; idx < cacheDAPriorSessionValueArea.Length; idx++)
					if (cacheDAPriorSessionValueArea[idx] != null &&  cacheDAPriorSessionValueArea[idx].EqualsInput(input))
						return cacheDAPriorSessionValueArea[idx];
			return CacheIndicator<DAPriorSessionValueArea>(new DAPriorSessionValueArea(), input, ref cacheDAPriorSessionValueArea);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DAPriorSessionValueArea DAPriorSessionValueArea()
		{
			return indicator.DAPriorSessionValueArea(Input);
		}

		public Indicators.DAPriorSessionValueArea DAPriorSessionValueArea(ISeries<double> input )
		{
			return indicator.DAPriorSessionValueArea(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DAPriorSessionValueArea DAPriorSessionValueArea()
		{
			return indicator.DAPriorSessionValueArea(Input);
		}

		public Indicators.DAPriorSessionValueArea DAPriorSessionValueArea(ISeries<double> input )
		{
			return indicator.DAPriorSessionValueArea(input);
		}
	}
}

#endregion

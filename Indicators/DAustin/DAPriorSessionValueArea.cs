#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.ChartStyles;
using NinjaTrader.Gui;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class DAPriorSessionValueArea : Indicator
    {
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

        private double currentSessionHigh;
        private double currentSessionLow;


        // ------------------------------------------------------------
        // Completed prior-session snapshot
        // ------------------------------------------------------------

        private bool isReady;
        private DateTime profileSessionDate;

        private double priorVAH;
        private double priorVAL;
        private double priorPOC;

        private double priorProfileHigh;
        private double priorProfileLow;


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


                // Stable prior-session levels.
                AddPlot(
                    new Stroke(Brushes.DodgerBlue, 1),
                    PlotStyle.Line,
                    "PriorVAHPlot");

                AddPlot(
                    new Stroke(Brushes.Goldenrod, 1),
                    PlotStyle.Line,
                    "PriorPOCPlot");

                AddPlot(
                    new Stroke(Brushes.DodgerBlue, 1),
                    PlotStyle.Line,
                    "PriorVALPlot");


                // Full prior-session RTH range.

                AddPlot(
                    new Stroke(Brushes.Gray, 1),
                    PlotStyle.Line,
                    "PriorProfileHighPlot");

                AddPlot(
                    new Stroke(Brushes.Gray, 1),
                    PlotStyle.Line,
                    "PriorProfileLowPlot");
            }
            else if (State == State.DataLoaded)
            {
                ResetState();
            }
        }


        protected override void OnBarUpdate()
        {
            // This indicator should be instantiated using the same
            // RTH bars series used as the input for SessionVolumeProfile.

            if (CurrentBar < 0)
                return;


            // First RTH session encountered.

            if (!haveActiveSession)
            {
                BeginSession();
                PublishPlots();
                return;
            }


            // First bar of a new RTH session means the previous
            // RTH profile is now complete.

            if (Bars.IsFirstBarOfSession)
            {
                if (CurrentBar > 0)
                    CaptureCompletedSession();

                BeginSession();
            }
            else
            {
                UpdateSessionRange();
            }


            PublishPlots();
        }


        // ============================================================
        // Session handling
        // ============================================================

        private void BeginSession()
        {
            haveActiveSession = true;

            activeSessionDate = Time[0].Date;

            currentSessionHigh = High[0];
            currentSessionLow = Low[0];
        }


        private void UpdateSessionRange()
        {
            if (High[0] > currentSessionHigh)
                currentSessionHigh = High[0];

            if (Low[0] < currentSessionLow)
                currentSessionLow = Low[0];
        }


        private void CaptureCompletedSession()
        {
            // Never allow yesterday's stale profile to remain valid
            // if today's rollover fails for some reason.

            isReady = false;

            if (_sessionVolumeProfile == null)
                return;


            // [1] is intentional.
            //
            // We are currently on the first bar of the NEW RTH session.
            // Therefore [1] represents the final bar of the completed
            // prior RTH session.

            double vah =
                _sessionVolumeProfile.DevelopingValueAreaHigh[1];

            double val =
                _sessionVolumeProfile.DevelopingValueAreaLow[1];

            double poc =
                _sessionVolumeProfile.DevelopingPoc[1];


            if (!IsFinite(vah) ||
                !IsFinite(val) ||
                !IsFinite(poc))
            {
                return;
            }


            // Basic sanity checks.

            if (vah < val)
                return;

            if (poc < val || poc > vah)
                return;


            priorVAH = vah;
            priorVAL = val;
            priorPOC = poc;

            priorProfileHigh = currentSessionHigh;
            priorProfileLow = currentSessionLow;

            profileSessionDate = activeSessionDate;

            isReady = true;
        }


        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }


        private void ResetState()
        {
            haveActiveSession = false;
            isReady = false;

            activeSessionDate = DateTime.MinValue;
            profileSessionDate = DateTime.MinValue;

            currentSessionHigh = double.NaN;
            currentSessionLow = double.NaN;

            priorVAH = double.NaN;
            priorVAL = double.NaN;
            priorPOC = double.NaN;

            priorProfileHigh = double.NaN;
            priorProfileLow = double.NaN;
        }


        // ============================================================
        // Plot publishing
        // ============================================================

        private void PublishPlots()
        {
            if (!isReady)
            {
                PriorVAHPlot[0] = double.NaN;
                PriorPOCPlot[0] = double.NaN;
                PriorVALPlot[0] = double.NaN;
                PriorProfileHighPlot[0] = double.NaN;
                PriorProfileLowPlot[0] = double.NaN;

                return;
            }


            PriorVAHPlot[0] = priorVAH;
            PriorPOCPlot[0] = priorPOC;
            PriorVALPlot[0] = priorVAL;

            PriorProfileHighPlot[0] = priorProfileHigh;
            PriorProfileLowPlot[0] = priorProfileLow;
        }


        // ============================================================
        // Injected runtime object
        // ============================================================



        // ============================================================
        // Stable prior-session values
        // ============================================================

        [Browsable(false)]
        public bool IsReady
        {
            get
            {
                Update();
                return isReady;
            }
        }


        [Browsable(false)]
        public DateTime ProfileSessionDate
        {
            get
            {
                Update();
                return profileSessionDate;
            }
        }


        [Browsable(false)]
        public double PriorVAH
        {
            get
            {
                Update();
                return priorVAH;
            }
        }


        [Browsable(false)]
        public double PriorVAL
        {
            get
            {
                Update();
                return priorVAL;
            }
        }


        [Browsable(false)]
        public double PriorPOC
        {
            get
            {
                Update();
                return priorPOC;
            }
        }


        [Browsable(false)]
        public double PriorProfileHigh
        {
            get
            {
                Update();
                return priorProfileHigh;
            }
        }


        [Browsable(false)]
        public double PriorProfileLow
        {
            get
            {
                Update();
                return priorProfileLow;
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

                return isReady
                    ? priorVAH - priorVAL
                    : double.NaN;
            }
        }


        [Browsable(false)]
        public double ValueAreaMidpoint
        {
            get
            {
                Update();

                return isReady
                    ? (priorVAH + priorVAL) / 2.0
                    : double.NaN;
            }
        }


        [Browsable(false)]
        public double ProfileRange
        {
            get
            {
                Update();

                return isReady
                    ? priorProfileHigh - priorProfileLow
                    : double.NaN;
            }
        }


        [Browsable(false)]
        public double POCPositionPct
        {
            get
            {
                Update();

                if (!isReady)
                    return double.NaN;

                double width = priorVAH - priorVAL;

                if (width <= 0)
                    return double.NaN;

                return (priorPOC - priorVAL) / width;
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

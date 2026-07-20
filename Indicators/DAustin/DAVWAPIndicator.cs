#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

// actual indicators and strategies need to be in the standard namespaces
// NinjaTrader.NinjaScript.Indicators or NinjaTrader.NinjaScript.Strategies
// So they are recognized by the platform and can be used in the UI.
namespace NinjaTrader.NinjaScript.Indicators
{
    class InternalProps
    {
        public string AnchorTime { get; set; }
        public string AnchorTimeZone { get; set; }
        public VwapBandMode BandMode { get; set; }
        public int BandRollingPeriod { get; set; }
        public int StdDevBandCount { get; set; } = 0;
        public double StdDev_1_Multiplier { get; set; } = 1.0;
        public double StdDev_2_Multiplier { get; set; } = 2.0;
        public double StdDev_3_Multiplier { get; set; } = 3.0;
        public int ZScorePeriods { get; set; } = 30;
        public bool EnableZScore { get; set; } = true;
    }

    public class DAVWAPIndicator : Indicator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private InternalProps ip = new InternalProps();
        private double cumulativePriceSquaredVolume;
        private double cumulativePriceVolume;
        private double cumulativeVolume;
        private StdDev rollingStdDev;
        private Series<double> _distance;
        private Series<double> _zscore;
        private SolidColorBrush Grey1Sigma;
        private SolidColorBrush Blue2Sigma;
        private SolidColorBrush Magenta3Sigma;
        private SolidColorBrush UpperShadeBrush;
        private SolidColorBrush LowerShadeBrush;

        private SMA smaDistance;
        private StdDev stdDistance;

        private TimeZoneInfo dataTimeZone;
        private TimeWindow TimeWindow {  get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                 = @"Volume-Weighted Average Price (VWAP) anchored to a specific time and timezone";
                Name                                        = "DAVWAP";
                Calculate                                   = Calculate.OnBarClose;
                IsOverlay                                   = true;
                DisplayInDataBox                            = true;
                DrawOnPricePanel                            = true;
                DrawHorizontalGridLines                     = true;
                DrawVerticalGridLines                       = true;
                PaintPriceMarkers                           = true;
                ScaleJustification                          = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive                    = true;

                // Default parameters
                AnchorTime                                  = "9:30am";
                AnchorTimeZone                              = "Eastern Standard Time";
                ip.AnchorTime = AnchorTime;
                ip.AnchorTimeZone = AnchorTimeZone;

                BandMode = VwapBandMode.Cumulative;
                BandRollingPeriod = 20;
                ip.BandMode = BandMode;
                ip.BandRollingPeriod = BandRollingPeriod;

                // Initialize stddev line brushes
                Grey1Sigma = new SolidColorBrush(Color.FromArgb(150, 112, 128, 144));
                Grey1Sigma.Freeze();
                Blue2Sigma = new SolidColorBrush(Color.FromArgb(95, 70, 130, 180));
                Blue2Sigma.Freeze();
                Magenta3Sigma = new SolidColorBrush(Color.FromArgb(140, 255, 0, 128));
                Magenta3Sigma.Freeze();

                // Region Brushes
                // Red haze for the overextended upper sell extreme zone
                UpperShadeBrush = new SolidColorBrush(Color.FromArgb(45, 255, 0, 0));
                UpperShadeBrush.Freeze();
                // Green haze for the overextended lower buy extreme zone
                LowerShadeBrush = new SolidColorBrush(Color.FromArgb(45, 0, 255, 0));
                LowerShadeBrush.Freeze();

                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "VWAP");

                AddPlot(new Stroke(Grey1Sigma, DashStyleHelper.Dash, 2), PlotStyle.Line, "Upper1");
                AddPlot(new Stroke(Grey1Sigma, DashStyleHelper.Dash, 2), PlotStyle.Line, "Lower1");

                AddPlot(new Stroke(Blue2Sigma, 1), PlotStyle.Line, "Upper2");
                AddPlot(new Stroke(Blue2Sigma, 1), PlotStyle.Line, "Lower2");

                AddPlot(new Stroke(Magenta3Sigma, 1), PlotStyle.Line, "Upper3");
                AddPlot(new Stroke(Magenta3Sigma, 1), PlotStyle.Line, "Lower3");

                // initialize to whatever the StdDevBandCount is currently set to
                // this will set the brushes for the bands to either the default
                // colors or transparent
                ip.StdDevBandCount = -1; // setting this to -1 forces the initialization
                InitializeStdDevBands();
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                TimeWindow = new TimeWindow(ip.AnchorTime, 0, ip.AnchorTimeZone);
                // Get data timezone     
                dataTimeZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;

                // Initialize cumulative values
                cumulativePriceSquaredVolume = 0;
                cumulativePriceVolume = 0;
                cumulativeVolume = 0;

                _distance = new Series<double>(this);
                _zscore = new Series<double>(this);
                smaDistance = SMA(_distance, ZScorePeriods);
                stdDistance = StdDev(_distance, ZScorePeriods);
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (NeedsInitialization() == true)
                {
                    Initialize();
                }

                TimeSpan currentTime = Times[0][0].TimeOfDay;
                // get the anchor point in the timezone the data is in
                TimeSpan anchorStart = TimeWindow.StartTimeIn(dataTimeZone.Id);

                //compare times down to the minute to determine if it's time to reAnchor
                // Cast TotalMinutes to an integer to ignore fractions of a minute
                int currentTimeMinutes = (int)currentTime.TotalMinutes;
                int anchorPointMinutes = (int)anchorStart.TotalMinutes;

                if (anchorPointMinutes == currentTimeMinutes)
                {
                    logger.Info("Resetting Anchored VWAP");
                    cumulativePriceSquaredVolume = 0;
                    cumulativePriceVolume = 0;
                    cumulativeVolume = 0;
                }

                // Calculate typical price and update cumulative values
                double high = Highs[0][0];
                double low = Lows[0][0];
                double close = Closes[0][0];
                double volume = Volumes[0][0];

                double typicalPrice = (high + low + close) / 3;

                cumulativePriceSquaredVolume += typicalPrice * typicalPrice * volume;
                cumulativePriceVolume += typicalPrice * volume;
                cumulativeVolume += volume;

                // Calculate VWAP
                if (cumulativeVolume > 0)
                {
                    double vwap = cumulativePriceVolume / cumulativeVolume;
                    double stdDev = 0;

                    if (BandMode == VwapBandMode.Cumulative)
                    {
                        double meanSquare = cumulativePriceSquaredVolume / cumulativeVolume;
                        double variance = Math.Max(0, meanSquare - vwap * vwap);
                        stdDev = Math.Sqrt(variance);
                    }
                    else if (BandMode == VwapBandMode.Rolling)
                    {
                        // Use the rolling standard deviation series
                        stdDev = rollingStdDev[0];
                    }

                    VWAP[0] = vwap;
                    Upper1[0] = vwap + ip.StdDev_1_Multiplier * stdDev;
                    Lower1[0] = vwap - ip.StdDev_1_Multiplier * stdDev;
                    Upper2[0] = vwap + ip.StdDev_2_Multiplier * stdDev;
                    Lower2[0] = vwap - ip.StdDev_2_Multiplier * stdDev;
                    Upper3[0] = vwap + ip.StdDev_3_Multiplier * stdDev;
                    Lower3[0] = vwap - ip.StdDev_3_Multiplier * stdDev;

                    Distance[0] = typicalPrice - vwap;

                    if (ip.EnableZScore && CurrentBar >= ip.ZScorePeriods)
                    {
                        double rollingMean = smaDistance[0];
                        double rollingStdDev = stdDistance[0];
                        ZScore[0] = rollingStdDev > 0 ? (Distance[0] - rollingMean) / rollingStdDev : 0;
                    }
                    else
                    {
                        ZScore[0] = 0;
                    }
                    DoRegionShading();
                }
                else
                {
                    // Fallback to close if no volume yet
                    VWAP[0] = close;
                    Upper1[0] = close;
                    Lower1[0] = close;
                    Upper2[0] = close;
                    Lower2[0] = close;
                    Upper3[0] = close;
                    Lower3[0] = close;
                    Distance[0] = 0;
                    ZScore[0] = 0;
                }
            }

            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        protected bool NeedsInitialization()
        {
            bool needsInitialization = false;

            if (
                (ip.AnchorTime != AnchorTime) ||
                (ip.AnchorTimeZone != AnchorTimeZone) ||
                (ip.EnableZScore != EnableZScore) ||
                (ip.ZScorePeriods != ZScorePeriods) ||
                (ip.StdDevBandCount != StdDevBandCount) ||
                (ip.StdDev_1_Multiplier != StdDev_1_Multiplier) ||
                (ip.StdDev_2_Multiplier != StdDev_2_Multiplier) ||
                (ip.StdDev_3_Multiplier != StdDev_3_Multiplier) ||
                (ip.BandMode != BandMode) ||
                (ip.BandRollingPeriod != BandRollingPeriod)
                )
            {
                needsInitialization = true;
            }
            return needsInitialization;
        }


        protected void DoRegionShading()
        {
            if (ip.StdDevBandCount < 2)
            {
                return;
            }

            // 2. Shade the Upper Zone (Between 2-Sigma and 3-Sigma)
            // Unique ID layout: "vwapUpperRegion" + Bar number string
            Draw.Region(this, "vwapUpperRegionCloud",
                CurrentBar, 0,      // Start at beginning of chart, end at current bar 
                Upper2, Upper3,    // The two historical series lines to bind the fill between
                null,              // No boundary outline stroke needed
                UpperShadeBrush,   // Pass the custom opacity fill brush
                100);              // Opacity level (0-255) for the fill color

            // 3. Shade the Lower Zone (Between -2-Sigma and -3-Sigma)
            Draw.Region(this, "vwapLowerRegionCloud",
                CurrentBar, 0,
                Lower3, Lower2,
                null,
                LowerShadeBrush,
                100);
        }

        private void InitializeAnchor()
        {
            if (ip.AnchorTime != AnchorTime || ip.AnchorTimeZone != AnchorTimeZone)
            {
                ip.AnchorTime = AnchorTime;
                ip.AnchorTimeZone = AnchorTimeZone;

                TimeWindow = new TimeWindow(ip.AnchorTime, 0, ip.AnchorTimeZone);
            }
        }

        private void InitializeBands()
        {
            if (ip.BandMode != BandMode || ip.BandRollingPeriod != BandRollingPeriod)
            {
                ip.BandMode = BandMode;
                ip.BandRollingPeriod = BandRollingPeriod;

                if (ip.BandMode == VwapBandMode.Rolling)
                {
                    rollingStdDev = StdDev(BandRollingPeriod);
                }
            }
        }

        private void InitializeZScore()
        {
            if (ip.ZScorePeriods != ZScorePeriods || ip.EnableZScore != EnableZScore)
            {
                ip.ZScorePeriods = ZScorePeriods;
                ip.EnableZScore = EnableZScore;

            }
        }

        private void InitializeStdDevMultipliers()
        {
            if (    ip.StdDev_1_Multiplier != StdDev_1_Multiplier ||
                    ip.StdDev_2_Multiplier != StdDev_1_Multiplier ||
                    ip.StdDev_3_Multiplier != StdDev_3_Multiplier
               )
            {
                ip.StdDev_1_Multiplier = StdDev_1_Multiplier;
                ip.StdDev_2_Multiplier = StdDev_2_Multiplier;
                ip.StdDev_3_Multiplier = StdDev_3_Multiplier;
            }
        }

        private void InitializeStdDevBands()
        {
            if (ip.StdDevBandCount != StdDevBandCount)
            {
                ip.StdDevBandCount = StdDevBandCount;

                if (ip.StdDevBandCount < 3)
                {
                    Plots[5].Brush = Brushes.Transparent;
                    Plots[6].Brush = Brushes.Transparent;
                }
                else
                {
                    Plots[5].Brush = Magenta3Sigma;
                    Plots[6].Brush = Magenta3Sigma;
                }

                if (ip.StdDevBandCount < 2)
                {
                    Plots[3].Brush = Brushes.Transparent;
                    Plots[4].Brush = Brushes.Transparent;
                }
                else
                {
                    Plots[3].Brush = Blue2Sigma;
                    Plots[4].Brush = Blue2Sigma;
                }

                if (ip.StdDevBandCount < 1)
                {
                    Plots[1].Brush = Brushes.Transparent;
                    Plots[2].Brush = Brushes.Transparent;
                }
                else
                {
                    Plots[1].Brush = Grey1Sigma;
                    Plots[2].Brush = Grey1Sigma;
                }
            }
        }

        public void Initialize()
        {
            if (NeedsInitialization() == false)
            {
                return;
            }

            // do initialization here.
            // we run off the properties in the InternalProps class. When InternalProps
            // differs from props visible from the outside it means we need to
            // update ip with the props that have changed and re-initialize.
            //
            InitializeAnchor();
            InitializeBands();
            InitializeZScore();
            InitializeStdDevMultipliers();
            InitializeStdDevBands();

            return;
        }

        #region GeneralParameters[NinjaScriptProperty]
        [NinjaScriptProperty]
        [Display(   Name = "Anchor Time", 
                    Description = "Time to anchor VWAP (e.g., 9:30am)", 
                    Order = 1, 
                    GroupName = "Parameters")]
        public string AnchorTime { get; set; }

        [NinjaScriptProperty]
        [Display(   Name = "Anchor TimeZone", 
                    Description = "TimeZone for anchor time", 
                    Order = 2, 
                    GroupName = "Parameters")]
        public string AnchorTimeZone { get; set; }

        [NinjaScriptProperty]
        [Display(   Name = "Band Calculation Mode",
                    Description = "VWAP band calculation mode",
                    Order = 3,
                    GroupName = "Parameters")]
        public VwapBandMode BandMode { get; set; }

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(   Name = "Band Rolling Period", 
                    Description = "Number of periods for the rolling calculation", 
                    Order = 4, 
                    GroupName = "Parameters")]
        public int BandRollingPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0, 3)]
        [Display(   Name = "StdDevBandCount", 
                    Description = "Number of standard deviation bands to display on the chart", 
                    Order = 5, 
                    GroupName = "Parameters")]
        public int  StdDevBandCount { get; set; } = 0;

        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(   Name = "StdDev_1_Multiplier",
                    Description = "Multiplier for the first standard deviation band",
                    Order = 6,
                    GroupName = "Parameters")]
        public double StdDev_1_Multiplier { get; set; } = 1.0;

        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(   Name = "StdDev_2_Multiplier",
                    Description = "Multiplier for the second standard deviation band",
                    Order = 7,
                    GroupName = "Parameters")]
        public double StdDev_2_Multiplier { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Range(0.1, 10)]
        [Display(   Name = "StdDev_3_Multiplier",
                    Description = "Multiplier for the third standard deviation band",
                    Order = 8,
                    GroupName = "Parameters")]
        public double StdDev_3_Multiplier { get; set; } = 3.0;

        [NinjaScriptProperty]
        [Range(2, 500)]
        [Display(   Name = "ZScore Periods", 
                    Description = "How many periods the ZScore calculation should use", 
                    Order = 9, 
                    GroupName = "Parameters")]
        public int ZScorePeriods { get; set; } = 30;

        [NinjaScriptProperty]
        [Display(   Name = "Calculate ZScore", 
                    Description = "Toggle to enable or disable ZScore calculation", 
                    Order = 10, 
                    GroupName = "Parameters")]
        public bool EnableZScore { get; set; } = true;
        #endregion

        #region Series
        // expose the  series for the VWAP and the standard deviation bands
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP => Values[0];        

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Upper1 => Values[1];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Lower1 => Values[2];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Upper2 => Values[3];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Lower2 => Values[4];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Upper3 => Values[5];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Lower3 => Values[6];

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ZScore => _zscore;

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Distance => _distance;
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private DAVWAPIndicator[] cacheDAVWAPIndicator;
        public DAVWAPIndicator DAVWAPIndicator(string anchorTime, string anchorTimeZone)
        {
            return DAVWAPIndicator(Input, anchorTime, anchorTimeZone);
        }

        public DAVWAPIndicator DAVWAPIndicator(ISeries<double> input, string anchorTime, string anchorTimeZone)
        {
            if (cacheDAVWAPIndicator != null)
                for (int idx = 0; idx < cacheDAVWAPIndicator.Length; idx++)
                    if (cacheDAVWAPIndicator[idx] != null && cacheDAVWAPIndicator[idx].AnchorTime == anchorTime && cacheDAVWAPIndicator[idx].AnchorTimeZone == anchorTimeZone && cacheDAVWAPIndicator[idx].EqualsInput(input))
                        return cacheDAVWAPIndicator[idx];
            return CacheIndicator<DAVWAPIndicator>(new DAVWAPIndicator(){ AnchorTime = anchorTime, AnchorTimeZone = anchorTimeZone }, input, ref cacheDAVWAPIndicator);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.DAVWAPIndicator DAVWAPIndicator(string anchorTime, string anchorTimeZone)
        {
            return null;
        }

        public Indicators.DAVWAPIndicator DAVWAPIndicator(ISeries<double> input , string anchorTime, string anchorTimeZone)
        {
            return null;
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.DAVWAPIndicator DAVWAPIndicator(string anchorTime, string anchorTimeZone)
        {
            return indicator.DAVWAPIndicator(Input, anchorTime, anchorTimeZone);
        }

        public Indicators.DAVWAPIndicator DAVWAPIndicator(ISeries<double> input , string anchorTime, string anchorTimeZone)
        {
            return indicator.DAVWAPIndicator(input, anchorTime, anchorTimeZone);
        }
    }
}

#endregion

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
    public class DAVWAPIndicator : Indicator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private double cumulativePriceVolume;
        private double cumulativeVolume;
        private TimeZoneInfo dataTimeZone;
        private TimeWindow timeWindow {  get; set; }

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

                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "VWAP");
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                timeWindow = new TimeWindow(AnchorTime, 0, AnchorTimeZone);
                // Get data timezone     
                dataTimeZone = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;

                // Initialize cumulative values
                cumulativePriceVolume = 0;
                cumulativeVolume = 0;
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                TimeSpan currentTime = Times[0][0].TimeOfDay;
                // get the anchor point in the timezone the data is in
                TimeSpan anchorStart = timeWindow.StartTimeIn(dataTimeZone.Id);

                //compare times down to the minute to determine if it's time to reAnchor
                // Cast TotalMinutes to an integer to ignore fractions of a minute
                int currentTimeMinutes = (int)currentTime.TotalMinutes;
                int anchorPointMinutes = (int)anchorStart.TotalMinutes;

                if (anchorPointMinutes == currentTimeMinutes)
                {
                    logger.Info("Resetting NYSession Anchored VWAP");
                    cumulativePriceVolume = 0;
                    cumulativeVolume = 0;
                }

                // Calculate typical price and update cumulative values
                double high = Highs[0][0];
                double low = Lows[0][0];
                double close = Closes[0][0];
                double volume = Volumes[0][0];

                double typicalPrice = (high + low + close) / 3;
                double candlePriceVolume = typicalPrice * volume;

                cumulativePriceVolume += candlePriceVolume;
                cumulativeVolume += volume;

                // Calculate VWAP
                if (cumulativeVolume > 0)
                {
                    Value[0] = cumulativePriceVolume / cumulativeVolume;
                }
                else
                {
                    Value[0] = close; // Fallback to close if no volume yet
                }
            }

            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Anchor Time", Description = "Time to anchor VWAP (e.g., 9:30am)", Order = 1, GroupName = "Parameters")]
        public string AnchorTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Anchor TimeZone", Description = "TimeZone for anchor time", Order = 2, GroupName = "Parameters")]
        public string AnchorTimeZone { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP
        {
            get { return Values[0]; }
        }
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

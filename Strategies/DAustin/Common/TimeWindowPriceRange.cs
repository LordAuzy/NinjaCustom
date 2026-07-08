using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using NLog;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class TimeWindowPriceRange : TimeWindow
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        public Strategy Strategy { get; set; }
        public ValueHistory HistoryBuffer { get; set; }
        public double RangeHigh { get; private set; }
        public TimeSpan RangeHighTOD { get; private set; } = TimeSpan.Zero;
        public double RangeLow { get; private set; }
        public TimeSpan RangeLowTOD { get; private set; } = TimeSpan.Zero;
        public double Range { get { return RangeHigh - RangeLow; } }

        public bool RangeSet { get; private set; }
        public bool IsInRange
        {
            get
            {
                // the RangeSet flag is te same as move out of range
                bool inRange = MovedInRange == true && RangeSet == false;

                return inRange;
            }
        }

        public bool MovedPastRange
        {
            get
            {
                // the RangeSet flag is te same as move out of range
                bool movedPastRange = MovedInRange == true && RangeSet == true;

                return movedPastRange;
            }
        }
        public TimeSpan TradeWindow { get; set; }

        /*
        Default Behavior: By default, NinjaTrader uses the time zone set on 
        your local Windows machine.
        Strategy / Backtest Context: When running a strategy, the times and 
        data series(Highs, Lows, Time[0]) are converted to the timezone 
        specified in your platform options.
        */
        public TimeZoneInfo DataTimeZone { get; } = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;

        // we need to know if the current time has been in range yet.
        // this has to have been set in order to set the RangeSet flag
        private bool MovedInRange { get; set; }
        public TimeSpan RangeStartTOD { get; private set; } = TimeSpan.Zero;
        public TimeSpan RangeEndTOD { get; private set; } = TimeSpan.Zero;
        #endregion

        #region Constructors
        public TimeWindowPriceRange(
            Strategy strategy,
            string start, 
            int minutesDuration, 
            string timeZoneId) : base(start, minutesDuration, timeZoneId)
        {
            Reset();

            Strategy = strategy;
            // these are in the current series timezone
            RangeStartTOD = StartTimeIn(DataTimeZone.Id);
            RangeEndTOD = EndTimeIn(DataTimeZone.Id);
        }

        public TimeWindowPriceRange(
            TimeWindowPriceRange toCopy)
        {
            Start = toCopy.Start;
            End = toCopy.End;
            TimeZoneId = toCopy.TimeZoneId;
            RangeSet = toCopy.RangeSet;
            RangeHigh = toCopy.RangeHigh;
            RangeLow = toCopy.RangeLow;
            RangeHighTOD = toCopy.RangeHighTOD;
            RangeLowTOD = toCopy.RangeLowTOD;
            MovedInRange = toCopy.MovedInRange;
            Strategy = toCopy.Strategy;
            HistoryBuffer = toCopy.HistoryBuffer;
        }

        #endregion

        #region PublicMethods
        public void Reset()
        {
            RangeSet = false;
            RangeHigh = double.MinValue;
            RangeLow = double.MaxValue;
            RangeHighTOD = TimeSpan.Zero;
            RangeLowTOD = TimeSpan.Zero;
            MovedInRange = false;
        }

        public void Update()
        {
            if (Strategy.Bars.IsFirstBarOfSession == true)
            {   // reset if we are starting a new session
                logger.Info(String.Format("{0}  Resetting.", Strategy.Times[0][0]));
                Reset();
                return;
            }

            if (RangeSet)
            {   // no more updating until RangeSet is reset
                return;
            }

            if (Strategy == null)
            {
                logger.Warn("TimeWindowPriceRange: Strategy is null. Returning.");
                return;
            }

            if (Strategy?.Bars?.TradingHours?.TimeZone == null ||
                String.IsNullOrWhiteSpace(Strategy.Bars.TradingHours.TimeZone))
            {
                logger.Warn("Strategy.Bars.TradingHours.TimeZone is empty. Returning.");
                return;
            }

            DateTime currentSeriesDateTime = Strategy.Time[0];
            TimeSpan currentSeriesTimeOfDay = currentSeriesDateTime.TimeOfDay;
            // get the range in the timezone time the series is in
            double currentBarHigh = Strategy.High[0];
            double currentBarLow = Strategy.Low[0];

            if (RangeStartTOD <= currentSeriesTimeOfDay && currentSeriesTimeOfDay <= RangeEndTOD)
            {
                MovedInRange = true;
                // I use the >= because even if they are the same I want the
                // time updated.
                if (currentBarHigh >= RangeHigh)
                {
                    RangeHighTOD = currentSeriesTimeOfDay;
                    RangeHigh = currentBarHigh;
                }
                if (currentBarLow <= RangeLow)
                {
                    RangeLowTOD = currentSeriesTimeOfDay;
                    RangeLow = currentBarLow;
                }
            }

            if (MovedInRange == true && currentSeriesTimeOfDay >= RangeEndTOD)
            {   // we've made the last update for the range
                RangeSet = true;
                logger.Info(String.Format("{0}  OpeningRange Set:  High:{1}  Low:{2}", Strategy.Times[0][0], RangeHigh, RangeLow));
                if (HistoryBuffer != null)
                {
                    HistoryBuffer.Add(RangeHigh - RangeLow);
                }
            }
        }

        public TimeSpan TradingEndTime()
        {
            // this method is for display purposes only
            // TODO: convert to data timezone
            TimeSpan tradeRangeStart = StartTimeIn(DataTimeZone.Id);
            TimeSpan tradeRangeEnd = tradeRangeStart + TradeWindow;

            return tradeRangeEnd;
        }

        public DateTime TradingEndDateTime()
        {
            TimeSpan tradeRangeEnd = TradingEndTime();
            DateTime tradingTimeEndDateTime = DateTime.Today.Add(tradeRangeEnd);

            return tradingTimeEndDateTime;
        }

        public bool IsInTradeTimeWindow()
        {
            bool inTradeWindow = false;
            TimeSpan currentTime = Strategy.Times[0][0].TimeOfDay;
            // trade window starts same time Opening range starts
            TimeSpan TradeRangeStart = StartTimeIn(DataTimeZone.Id);
            TimeSpan TradeRangeEnd = TradeRangeStart + TradeWindow;

            if (TradeRangeStart <= currentTime && currentTime <= TradeRangeEnd)
            {
                inTradeWindow = true;
            }
            return inTradeWindow;
        }

        public void SetTradeWindowDurationMinutes(int minutes)
        {
            // trade window start at NY session open
            TradeWindow = TimeSpan.FromMinutes(minutes);
        }

        #endregion
    }
}

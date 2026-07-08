using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using NLog;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class DAVWAP : TimeWindow
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        Strategy Strategy { get; set; }
        TimeZoneInfo DataTimeZone { get; } = NinjaTrader.Core.Globals.GeneralOptions.TimeZoneInfo;
        public double CumulativePriceVolume { get; private set; }
        public double CumulativeVolume { get; private set; }
        public double Value 
        { 
            get 
            {
                double value = 0;
                if (CumulativeVolume > 0) 
                {
                    value = CumulativePriceVolume / CumulativeVolume;
                }

                return value;

            } 
        }
        public ValueHistory History { get; private set; }
        #endregion

        public DAVWAP(Strategy strat, string AnchorTime, string anchorTimeZone) : 
            base(AnchorTime, 0, anchorTimeZone)
        {
            Strategy = strat;
            History = new ValueHistory(60);
        }

        #region PublicMethods
        public void Update()
        {
            TimeSpan currentTime = Strategy.Times[0][0].TimeOfDay;
            // get the anchor point in the timezone the data is in
            TimeSpan anchorStart = StartTimeIn(DataTimeZone.Id);

            //compare times down to the minute to determine if it's time to reAnchor
            // Cast TotalMinutes to an integer to ignore fractions of a minute
            int currentTimeMinutes = (int)currentTime.TotalMinutes;
            int anchorPointMinutes = (int)anchorStart.TotalMinutes;

            if (anchorPointMinutes == currentTimeMinutes)
            {
                logger.Info("Resetting NYSession Anchored VWAP");
                CumulativePriceVolume = 0;
                CumulativeVolume = 0;
                History.Clear();
            }

            double high = Strategy.Highs[0][0];
            double low = Strategy.Lows[0][0];
            double close = Strategy.Closes[0][0];
            double volume = Strategy.Volumes[0][0];

            double typicalPrice = (high + low + close) / 3;
            double candlePriceVolume = typicalPrice * volume;

            CumulativePriceVolume += candlePriceVolume;
            CumulativeVolume += volume;

            History.Add(Value);
        }

        public double GetFromMostRecent(int index)
        {
            return History.GetFromMostRecent(index);
        }
        #endregion
    }
}

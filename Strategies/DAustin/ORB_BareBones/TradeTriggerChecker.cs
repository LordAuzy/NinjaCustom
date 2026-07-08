using ActiproSoftware.Text.Utility;
using NinjaTrader.Cbi;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using NLog;
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.Strategies.DAustin.ORB_BareBones
{
    public class TradeTriggerChecker
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        public ValueHistory OpeningRangeHistory { get; set; }
        public Strategy Strategy { get; private set; }
        public TimeWindowPriceRange OpeningRange { get; private set; }
        public DAORBIndicators ORBIndicators { get; private set; }
        public DAOrderType OrderType { get; private set; } = DAOrderType.None;
        public int VolumeCheckAverageBarCount { get; set; } = 5;
        public double VolumeCheckHowFarAboveAverage { get; set; } = 1.5;
        public bool BreakoutCandleVolumeCheckEnabled { get; set; } = false;
        public bool VWAPCheckEnabled { get; set; } = false;
        public bool OpeningRangeWidthCheckEnabled { get; set; } = false;
        public int OpeningRangeMaxWidth { get; set; } = 80;
        public int OpeningRangeMinWidth { get; set; } = 20;
        public bool ExtermeInbalanceCheckEnabled { get; set; } = false;
        public static int Reject_VOL { get; set; }
        public static int Reject_Width { get; set; }
        public static int Reject_VWAP { get; set; }
        public TimeWindowPriceRange PreMarketBig { get; set; }
        public TimeWindowPriceRange PreMarketSmall { get; set; }
        public bool PreMarketFilteringEnabled { get; set; } = false;

        #endregion

        #region Constructors
        public TradeTriggerChecker(
            Strategy strat, 
            TimeWindowPriceRange or,
            DAORBIndicators oRBIndicators)
        {
            Strategy = strat;
            OpeningRange = or;
            ORBIndicators = oRBIndicators;
        }
        #endregion

        #region PublicMethods
        public DAOrderType Triggered()
        {
            OrderType = ClosedOutsideOfOpeningRange();

            if (OrderType != DAOrderType.None)
            {
                if (BreakoutCandleVolumeCheckEnabled && !BreakoutCandleVolumeCheckOK())
                {
                    OrderType = DAOrderType.None;
                }
                else if (VWAPCheckEnabled && !VWAPCheckOK(OrderType))
                {
                    OrderType = DAOrderType.None;
                }
                else if (PreMarketFilteringEnabled && !PreMarketFilteringCheckOK(OrderType))
                {
                    OrderType = DAOrderType.None;
                }
                else if (OpeningRangeWidthCheckEnabled && !OpeningRangeWidthCheckOK())
                {
                    OrderType = DAOrderType.None;
                }
                else if (ExtermeInbalanceCheckEnabled && !ExtermeInbalanceCheckOK())
                {
                    OrderType = DAOrderType.None;
                }
            }
            return OrderType;
        }
        #endregion

        #region PrivateMethods
        private DAOrderType ClosedOutsideOfOpeningRange()
        {
            if (Strategy.CrossAbove(Strategy.Closes[0], OpeningRange.RangeHigh, 1))
            {
                OrderType = DAOrderType.Long;
            }
            else if (Strategy.CrossBelow(Strategy.Closes[0], OpeningRange.RangeLow, 1))
            {
                OrderType = DAOrderType.Short;
            }

            return OrderType;
        }

        private bool PreMarketFilteringCheckOK(DAOrderType ot)
        {
            bool ok = true;

            if (OpeningRange.RangeLow > PreMarketSmall.RangeHigh ||
                OpeningRange.RangeHigh < PreMarketSmall.RangeLow)
            {   // high probability trade if no overlap

            }
            else if (Math.Abs(OpeningRange.RangeHigh - PreMarketSmall.RangeHigh) <= Strategy.TickSize * 7)
            {   // dump longs
                if (ot == DAOrderType.Long)
                {   // if the  premarket high within 37min of premarket close allow
                    TimeSpan highToEndTS = PreMarketSmall.RangeEndTOD - PreMarketSmall.RangeHighTOD;
                    if (highToEndTS > TimeSpan.FromMinutes(37))
                    {
                        ok = false;
                    }
                }
            }
            else if (Math.Abs(OpeningRange.RangeLow - PreMarketSmall.RangeLow) <= Strategy.TickSize * 7)
            {   // dump shorts
                if (ot == DAOrderType.Short)
                {   // if the  premarket low within 37min of premarket close
                    TimeSpan lowToEndTS = PreMarketSmall.RangeEndTOD - PreMarketSmall.RangeLowTOD;
                    if (lowToEndTS > TimeSpan.FromMinutes(37))
                    {
                        ok = false;
                    }
                }
            }

            return ok;
        }

        private bool BreakoutCandleVolumeCheckOK()
        {
            bool ok = false;
            VolumeSeries volume = Strategy.Volume;
            double currentBarVolume = volume[0];
            double barsAverageVolume = 0;
            int idx;

            for (idx = 1; idx <= VolumeCheckAverageBarCount && idx < volume.Count; idx++)
            {
                barsAverageVolume += volume[idx];
            }
            barsAverageVolume = barsAverageVolume / (idx - 1);

            //now that the average vol calculation is done do the check
            //initially do a 1.4 * average volumme
            if (currentBarVolume >= (barsAverageVolume * VolumeCheckHowFarAboveAverage))
            {
                ok = true;
            }

            string logString = String.Format("BreakoutCandleVolumeCheckOK returning:{0}.  CandleVol={1}  AverageVol={2}",
                    ok, currentBarVolume, barsAverageVolume);
            logger.Info(logString);
            if (!ok)
            {
                Reject_VOL += 1;
            }

            return ok;
        }

        private bool VWAPCheckOK(DAOrderType ot)
        {
            bool isValid = true;
            double VWAP = ORBIndicators.NYSessionAnchoredVWAP.Value;
            double candleClose = Strategy.Closes[0][0];
            string logStr = String.Format("VWAPCheckValidated. IsValid={0}", isValid);

            if (ot == DAOrderType.Short && candleClose >= VWAP)
            {
                logStr = String.Format("Short order invalidated. CandleClose >= VWAP.  VWAP={0}  CandleClose={1}", VWAP, candleClose);
                isValid = false;
            }
            else if (ot == DAOrderType.Long && candleClose <= VWAP)
            {
                logStr = String.Format("Long order invalidated. CandleClose <= VWAP.  VWAP={0}  CandleClose={1}", VWAP, candleClose);
                isValid = false;
            }

            logger.Info(logStr);

            if (!isValid)
            {
                Reject_VWAP += 1;
            }
            return isValid;
        }

        private bool OpeningRangeWidthCheckOK()
        {
            bool ok = false;
            string logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.", ok);

            if (OpeningRangeHistory != null && OpeningRangeHistory.Count >= 30)
            {
                double averageOR = OpeningRangeHistory.Average();
                double percentage = (OpeningRange.Range * 100) / averageOR;

                if (percentage >= OpeningRangeMinWidth && percentage <= OpeningRangeMaxWidth)
                {
                    ok = true;
                }
                logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.  Range at {1} percent of average", ok, percentage);
            }
            else
            {   // when opening range history is not ready we'll check against the ATR
                // Minimum Threshold: The opening range should be greater than about 1.25x
                // the ATR.Ranges smaller than this often indicate low volatility, leading
                // to higher risk of whipsaws, false breakouts, or stagnant price action
                // with poor follow - through.
                //Maximum Threshold: The opening range should be no larger than 3x the ATR.Wider
                //ranges suggest excessive volatility, which can result in overly wide stop - losses,
                //poor risk-reward ratios, and increased chance of reversals or exhaustion.
                //
                double currentATR = ORBIndicators.ATR[0];
                double percentage = (OpeningRange.Range * 100) / currentATR;

                if (percentage >= 125 && percentage <= 300)
                {
                    ok = true;
                }
                logString = String.Format("OpeningRangeWidthCheckOK returning:{0}.  Range at {1} percent of ATR", ok, percentage);
            }

            if (!String.IsNullOrEmpty(logString))
            {
                logger.Info(logString);
            }

            if (!ok)
            {
                Reject_Width += 1;
            }
            return ok;
        }

        private bool ExtermeInbalanceCheckOK()
        {
            bool ok = true;


            return ok;
        }
        #endregion
    }
}

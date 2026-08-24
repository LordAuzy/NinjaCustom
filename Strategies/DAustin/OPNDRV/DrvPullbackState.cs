using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class DrvPullbackState
    {
        #region Properties
        public DriveSetup DriveSetup { get; set; }
        public double PullbackHigh { get; set; } = double.MinValue;
        public double PullbackLow { get; set; } = double.MaxValue;
        // Maximum amount price has penetrated VWAP during the pullback.
        // Long: penetration = VWAP - Low
        // Short: penetration = High - VWAP
        //
        // Stored as a positive point value.
        // Zero means no penetration through VWAP occurred.
        public double MaxVWAPPenetration { get; private set; } = 0.0;
        public double RetracementPct
        {
            get
            {
                double retPct = 0;

                if (DriveSetup != null && DriveSetup.Drive.Range > 0)
                {
                    if (DriveSetup.Direction == MarketPosition.Long)
                    {
                        retPct = (DriveSetup.Drive.High - PullbackLow) / DriveSetup.Drive.Range;
                    } 
                    else if (DriveSetup.Direction != MarketPosition.Long)
                    {
                        retPct = (PullbackHigh - DriveSetup.Drive.Low) / DriveSetup.Drive.Range;
                    }
                }
                return retPct;
            }
        }
        #endregion

        #region Constructors
        public DrvPullbackState() 
        { 
        }

        public DrvPullbackState(DriveSetup drv)
        {
            DriveSetup = drv;
        }

        #endregion

        #region PublicMethods
        public void UpdateHigh(double high)
        {
            PullbackHigh = Math.Max(PullbackHigh, high);
        }

        public void UpdateLow(double low)
        {
            PullbackLow = Math.Min(PullbackLow, low);
        }

        public void UpdateVWAPPenetration(double penetration)
        {
            if (penetration <= 0)
            {
                return;
            }

            MaxVWAPPenetration = Math.Max(MaxVWAPPenetration, penetration);
        }

        public void Reset()
        {
            PullbackHigh = double.MinValue;
            PullbackLow = double.MaxValue;
            MaxVWAPPenetration = 0.0;
        }
        #endregion
    }
}

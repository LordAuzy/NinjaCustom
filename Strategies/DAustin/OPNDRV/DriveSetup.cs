using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class DriveSetup
    {
        public MarketPosition Direction { get; set; }
        public DriveState Drive { get; set; }
        public int DriveCompletedBar { get; set; }
        public double VWAP { get; set; }
        public double VWAPSlopeATR { get; set; }
        public double VWAPDistanceATR { get; set; }
        public double EMASpreadATR { get; set; }
        public double CloseLocation { get; set; }

        public DriveSetup()
        {
        }
    }
}

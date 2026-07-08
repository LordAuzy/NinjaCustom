using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class StratInputParams
    {
        public int OpeningRangeMinutes { get; set; } = 30;
        public int TradingMinutes { get; set; } = 180;
        public bool BreakoutCandleVolumeCheckEnabled { get; set; }
        public double VolumeCheckHowFarAboveAverage { get; set; }
        public bool OpeningRangeWidthCheckEnabled { get; set; }
        public int OpeningRangeMaxWidth { get; set; }
        public int OpeningRangeMinWidth { get; set; }
        public bool VWAPCheckEnabled { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.DataCollectors
{
    public class ECE_ORB_DataCollector
    {
        public int BreakAndRetestTriggerShortCount { get; set; } = 0;
        public int BreakAndRetestTriggerLongCount { get; set; } = 0;
        public int CloseOutsideOpeningRangeTriggerShortCount { get; set; } = 0;
        public int CloseOutsideOpeningRangeTriggerLongCount { get; set; } = 0;
        public int BreakoutBarVolumeCheckFailedCount { get; set; } = 0;
        public int OpeningRangeWidthCheckFailedCount { get; set; } = 0;
        public int VWAPShortCheckFailedCount { get; set; } = 0;
        public int VWAPLongCheckFailedCount { get; set; } = 0;
        public int VWAPShortCheckSlopeFailedCount { get; set; } = 0;
        public int VWAPLongCheckSlopeFailedCount { get; set; } = 0;
        public int SIPLongMoonshotCount { get; set; } = 0;
        public int SIPShortMoonshotCount { get; set; } = 0;
        public int SIPLongMidpointCount { get; set; } = 0;
        public int SIPShortMidpointCount { get; set; } = 0;
        public int SIPLongOppositeBreakCount { get; set; } = 0;
        public int SIPShortOppositeBreakCount { get; set; } = 0;
    }
}

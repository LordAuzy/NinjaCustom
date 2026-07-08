using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class TimeWindows
    {
        private class TimeBlock
        {
            #region Properties
            public TimeSpan StartTOD { get; set; } = TimeSpan.Zero;
            public TimeSpan EndTOD { get; set; } = TimeSpan.Zero;
            #endregion

            #region constructors
            public TimeBlock(
                TimeSpan startTOD,
                TimeSpan endTOD)
            {
                StartTOD = startTOD;
                EndTOD = endTOD;
            }

            public TimeBlock() 
            { 
            
            }
            #endregion
        }

        #region Fields
        private List<TimeBlock> _timeBlocks = new List<TimeBlock>();
        #endregion

        #region Properties
        public TimeConverter TimeConverter { get; } = new TimeConverter();
        public Strategy Strategy { get; set; }
        public string AnchorTime { get; private set; }
        public string AnchorTimeZone { get; private set; }
        public TimeSpan AnchorTOD { get; private set; }
        #endregion

        #region Constructors
        public TimeWindows(
            Strategy strat,
            string anchorTime,
            string anchorTimeTimezone)
        {
            Strategy = strat;
            AnchorTime = anchorTime;
            AnchorTimeZone = anchorTimeTimezone;

            AnchorTOD = TimeConverter.ToDataTimeOfDay(AnchorTime, AnchorTimeZone);
        }

        public TimeWindows()
        {

        }
        #endregion

        #region PublicMethods
        public void AddTimeBlock(
            int anchorOffsetStartMinutes,
            int anchorOffsetEndMinutes)
        {
            AddTimeBlock(TimeSpan.FromMinutes(anchorOffsetStartMinutes), TimeSpan.FromMinutes(anchorOffsetEndMinutes));
        }

        public void AddTimeBlock(
            TimeSpan anchorOffsetStart,
            TimeSpan anchorOffsetEnd)
        {
            // startminutes, endminutes need to be added to the anchor TOD
            TimeBlock timeBlock = new TimeBlock();

            timeBlock.StartTOD = AnchorTOD.Add(anchorOffsetStart);
            timeBlock.EndTOD = AnchorTOD.Add(anchorOffsetEnd);

            _timeBlocks.Add(timeBlock);
        }

        public bool IsInDefinedTimeBlock()
        {
            bool isIn = true; //if no timeblocks are defined we assume everything goes

            if (_timeBlocks.Count > 0)
            {   // if we  have timeblocks we default to not allowed and check each timeblock for in-ness
                isIn = false;
                DateTime currentSeriesDateTime = Strategy.Time[0];
                TimeSpan currentSeriesTOD = currentSeriesDateTime.TimeOfDay;

                foreach ( TimeBlock timeBlock in _timeBlocks )
                {
                    // Handle case where EndTOD < StartTOD (window spans midnight)
                    bool inRange;
                    if (timeBlock.EndTOD < timeBlock.StartTOD)
                    {
                        // Window spans midnight: current time must be >= Start OR < End
                        inRange = currentSeriesTOD >= timeBlock.StartTOD || currentSeriesTOD < timeBlock.EndTOD;
                    }
                    else
                    {
                        // Normal window: current time must be between Start and End
                        inRange = currentSeriesTOD >= timeBlock.StartTOD && currentSeriesTOD <= timeBlock.EndTOD;
                    }

                    if (inRange)
                    {
                        isIn = true; 
                        break;
                    }
                }
            }
            return isIn;
        }

        public bool IsInTimeWindow() 
        { 
            return IsInDefinedTimeBlock();
        }
        #endregion
    }
}

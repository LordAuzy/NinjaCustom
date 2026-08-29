using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class DriveState
    {
        #region Properties
        public double High { get; set; } = 0;
        public double Low { get; set; } = 0;
        public double Open { get; set; } = 0;
        public double Close { get; set; } = 0;
        public double ATR { get; set; } = 0;

        public double Range { get {  return High - Low; } }
        public double RangeATR
        {
            get { 
                if (ATR <= 0)
                    return 0;
                return Range / ATR;
            }
        }

        public double NetMove
        {
            get { return Close - Open; }
        }

        // Signed:
        // + = long displacement
        // - = short displacement
        public double NetMoveAtr
        {
            get
            {
                if (ATR <= 0)
                    return 0;

                return NetMove / ATR;
            }
        }

        // Direction neutral: 0..1
        public double Efficiency
        {
            get
            {
                if (Range <= 0)
                    return 0;

                return Math.Abs(NetMove) / Range;
            }
        }
        #endregion

        #region Constructors
        public DriveState()
        {
        }
        #endregion

        #region Public Methods
        public DriveState Clone()
        {
            return new DriveState
            {
                High = High,
                Low = Low,
                Open = Open,
                Close = Close,
                ATR = ATR
            };
        }
        #endregion
    }
}
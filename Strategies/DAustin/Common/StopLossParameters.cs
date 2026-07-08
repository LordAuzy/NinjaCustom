using NinjaTrader.Custom.DAustin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class StopLossParameters
    {
        #region Properties
        public StopLossTrailingMode SLTrailingMode { get; set; }
        public StopLossInitialPlacement SLInitialPlacement { get; set; }
        public int InitialPlacement_ATRPeriod { get; set; }
        public double InitialPlacement_ATRMult { get; set; }
        public double MoonshotORD { get; set; }
        public double MidpointORD { get; set; }
        public int MidpointAvgRange { get; set; }
        public int TrailingATRPeriod { get; set; }
        public double TrailingATRMultiplier { get; set; }
        #endregion

        #region Constructors
        public StopLossParameters()
        {
        }

        public StopLossParameters(StopLossParameters source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            SLTrailingMode = source.SLTrailingMode;
            SLInitialPlacement = source.SLInitialPlacement;
            InitialPlacement_ATRPeriod = source.InitialPlacement_ATRPeriod;
            InitialPlacement_ATRMult = source.InitialPlacement_ATRMult;
            MoonshotORD = source.MoonshotORD;
            MidpointORD = source.MidpointORD;
            MidpointAvgRange = source.MidpointAvgRange;
            TrailingATRPeriod = source.TrailingATRPeriod;
            TrailingATRMultiplier = source.TrailingATRMultiplier;
        }
        #endregion
    }
}

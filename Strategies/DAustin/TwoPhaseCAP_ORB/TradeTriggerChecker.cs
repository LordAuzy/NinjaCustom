using ActiproSoftware.Text.Utility;
using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
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

namespace NinjaTrader.Custom.Strategies.DAustin.TwoPhaseCAP_ORB
{
    public class TradeTriggerChecker
    {
        #region Properties
        public ValueHistory OpeningRangeHistory { get; set; }
        public Strategy Strategy { get; private set; }
        public TimeWindowPriceRange OpeningRange { get; private set; }
        public DAORBIndicators ORBIndicators { get; private set; }
        public int OpeningRangeMaxWidth { get; set; } = 80;
        public int OpeningRangeMinWidth { get; set; } = 20;
        public DAOrderType OrderType { get; private set; } = DAOrderType.None;
        public static int Reject_VOL { get; set; }
        public static int Reject_Width { get; set; }
        public static int Reject_VWAP { get; set; }
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
             if (OrderType != DAOrderType.None)
            {

            }
            return OrderType;
        }
        #endregion

        #region PrivateMethods




        #endregion
    }
}

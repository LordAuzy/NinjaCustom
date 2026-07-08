#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.TwoPhaseCAP_ORB;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{

/****************************************************************
Strategy mission

Primary goal: Preserve capital with minimal drawdown
Secondary goal: Slow, consistent equity growth
Tertiary goal: Unlock higher R only when conditions justify it

If a rule increases profit but worsens drawdown → it fails.
******************************************************************/
	public class TwoPhaseCAPORB : Strategy
	{
        #region Properties
        private TimeWindowPriceRange _openingRange = null;
        public TimeWindowPriceRange OpeningRange
        {
            get
            {
                if (_openingRange == null)
                {
                    _openingRange = new TimeWindowPriceRange(this, "9:30am", OpeningRangeMinutes, "Eastern Standard Time");
                    _openingRange.SetTradeWindowDurationMinutes(TradingMinutes);
                    _openingRange.HistoryBuffer = OpeningRangeHistory;
                }
                return _openingRange;
            }

            private set { _openingRange = value; }
        }

        private ValueHistory _orHistory = null;
        public ValueHistory OpeningRangeHistory
        {
            get
            {
                if (_orHistory == null)
                {
                    _orHistory = new ValueHistory(60);
                }
                return _orHistory;
            }

            private set { _orHistory = value; }
        }

        private ManualMarketPosition _mmp = null;
        public ManualMarketPosition ManualMarketPosition
        {
            get
            {
                if (_mmp == null)
                {
                    _mmp = new ManualMarketPosition(this);
                }
                return _mmp;
            }

            private set { _mmp = value; }
        }

        private DAORBIndicators _orbIndicators = null;
        public DAORBIndicators ORBIndicators
        {
            get
            {
                if (_orbIndicators == null)
                {
                    _orbIndicators = new DAORBIndicators(this);
                }
                return _orbIndicators;
            }

            private set { _orbIndicators = value; }
        }

        public List<DAOrder> Orders { get; } = new List<DAOrder>();
        #endregion

        #region NinjascriptProperties
        [NinjaScriptProperty]
        [Range(5, 45)]
        [Display(Name = "Opening Range Minutes", Description = "Duration of the opening range in minutes", Order = 1, GroupName = "Parameters")]
        public int OpeningRangeMinutes { get; set; }

        [NinjaScriptProperty]
        [Range(45, 180)]
        [Display(Name = "TradingMinutes", Description = "Minutes from NYOpen you can trade", Order = 2, GroupName = "Parameters")]
        public int TradingMinutes { get; set; }

        [NinjaScriptProperty]
        [Range(50, 150)]
        [Display(Name = "ORMaxWidth", Description = "MaxRangeWidthPercentOfAverage", Order = 5, GroupName = "Parameters")]
        public int ORMaxWidth { get; set; }

        [NinjaScriptProperty]
        [Range(10, 50)]
        [Display(Name = "ORMinWidth", Description = "MinRangeWidthPercentOfAverage", Order = 6, GroupName = "Parameters")]
        public int ORMinWidth { get; set; }

        #endregion

        #region Overrides
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"ORB Strat aimed a preserving capital";
				Name										= "TwoPhaseCAPORB";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;

                // This is to get at least 60 days of the ORBreakout width before
                // making any trades.
                DaysToLoad = 60;
                // 60 days 24 hours minute bars
                BarsRequiredToTrade = DaysToLoad * 1440;
             
                // Default parameter values
                OpeningRangeMinutes = 30;
                TradingMinutes = 60;
                ORMaxWidth = 80;
                ORMinWidth = 20;
            }
            else if (State == State.Configure)
			{
			}
            else if (State == State.DataLoaded)
            {
                ORBIndicators.Initialize();
            }
            else if (State == State.Terminated)
            {
                Print(String.Format("FilterRejectCount:  VOL={0}  Width={1}  VWAP={2}",
                    TradeTriggerChecker.Reject_VOL, TradeTriggerChecker.Reject_Width, TradeTriggerChecker.Reject_VWAP));

                TradeTriggerChecker.Reject_VOL = 0;
                TradeTriggerChecker.Reject_Width = 0;
                TradeTriggerChecker.Reject_VWAP = 0;
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
            {   // the current update is not coming from the primary data series
                return;
            }

            ORBIndicators.Update();
            OpeningRange.Update();

            if (CurrentBars[0] < BarsRequiredToTrade)
            {   // in preload phase
                return;
            }

            // Breakout logic after range is set
            // MarketPosition.Flat means your trading strategy currently holds no
            // open positions (neither long nor short) for a specific instrument or account
            else if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow() && ManualMarketPosition.MarketPosition == MarketPosition.Flat)
            {
                DAOrder order = null;
                TradeTriggerChecker ttc = new TradeTriggerChecker(this, OpeningRange, ORBIndicators);
                ttc.OpeningRangeMaxWidth = ORMaxWidth;
                ttc.OpeningRangeMinWidth = ORMinWidth;

                DAOrderType ot = ttc.Triggered();
                if (ot != DAOrderType.None)
                {
                    order = new DAOrder(this, ot, OpeningRange);
                    order.TakeProfitMultiplier = 2;
                    // TODO: clear the order when completed. This is just a stop gap
                    Orders.Clear();
                    // first add to list so it can be found
                    Orders.Add(order);
                    // then enter the orders
                    order.Enter(2);
                }

            }
        }

        protected override void OnExecutionUpdate(
            Cbi.Execution execution, 
            string executionId, 
            double price, 
            int quantity, 
			Cbi.MarketPosition marketPosition, 
            string orderId, 
            DateTime time)
		{
            ManualMarketPosition.Update(execution.Order, quantity);
        }

        protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice, 
			int quantity, int filled, double averageFillPrice, 
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			
		}

		protected override void OnPositionUpdate(Cbi.Position position, double averagePrice, 
			int quantity, Cbi.MarketPosition marketPosition)
		{
			
		}
        #endregion
    }
}

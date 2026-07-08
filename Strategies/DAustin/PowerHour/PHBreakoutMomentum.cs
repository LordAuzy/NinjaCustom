#region Using declarations
using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies.DAustin.PowerHour;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
    public class PHBreakoutMomentum : StratBase
	{
        #region Fields
        #endregion

        #region Properties
        public TimeConverter TimeConverter { get; } = new TimeConverter();
        private TimeWindowPriceRange _phPriceRange = null;
        public TimeWindowPriceRange PowerHourPriceRange
        {
            get
            {
                if (_phPriceRange == null)
                {
                     _phPriceRange = new TimeWindowPriceRange(this, "3:00pm", 29, "Eastern Standard Time");
                }
                return _phPriceRange;
            }

            private set { _phPriceRange = value; }
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

        private DAPHIndicators _indicators = null;
        public DAPHIndicators Indicators
        {
            get
            {
                if (_indicators == null)
                {
                    _indicators = new DAPHIndicators(this);
                }
                return _indicators;
            }

            private set { _indicators = value; }
        }

        public List<DABaseOrder> Orders { get; } = new List<DABaseOrder>();
        public int TradeCount { get; set; } = 0;
        #endregion

        #region NinjascriptProperties
        [NinjaScriptProperty]
        [Display(Name = "VWAPFilter Enabled", Order = 1, GroupName = "Parameters")]
        public bool VWAPFilterEnabled { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PriorSessionFilter Enabled", Order = 2, GroupName = "Parameters")]
        public bool PriorSessionFilterEnabled { get; set; }

        [NinjaScriptProperty]
        [Range(0.001, double.MaxValue)]
        [Display(Name = "Stop Loss Percent", Order = 3, GroupName = "Parameters")]
        public double StopLossPercent { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Position Size", Order = 4, GroupName = "Parameters")]
        public int PositionSize { get; set; }
        #endregion

        protected override void OnStateChange()
		{
            base.OnStateChange();

            if (State == State.SetDefaults)
			{
				Description									= @"Take advantage of volatility during PowerHour";
				Name										= "PHBreakoutMomentum";
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
				BarsRequiredToTrade							= 20;

                // Default parameter values
                VWAPFilterEnabled = true;
                PriorSessionFilterEnabled = false;
                StopLossPercent = 0.01;		// 1%
                PositionSize = 100;			// Shares or contracts
            }
            else if (State == State.Configure)
            {
                // Assume 1-minute bars for intraday
                AddDataSeries(BarsPeriodType.Minute, 1);
            }
            else if (State == State.DataLoaded)
            {
                Indicators.Initialize();
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
            DABaseOrder daOrder = Orders.Find(x => x.OrderId == execution.Name || x.OrderId == execution.Order.FromEntrySignal);

            if (daOrder == null)
            {
                Print("OnExecutionUpdate - order not found in our orderList");
            }
            else
            {
                daOrder.OnExecute(execution, executionId, price, quantity, marketPosition,
                    orderId, time);
            }

            ManualMarketPosition.Update(execution.Order, quantity);

            if (ManualMarketPosition.MarketPosition == MarketPosition.Flat)
            {   // when position is flat clear order list
                Print("OnExecutionUpdate - MarketPosition is flat. Clearing orderList.");
                Orders.Clear();
            }
        }

        protected override void OnOrderUpdate(
            Cbi.Order order, 
            double limitPrice, 
            double stopPrice, 
			int quantity, 
            int filled, 
            double averageFillPrice, 
			Cbi.OrderState orderState, 
            DateTime time, 
            Cbi.ErrorCode error, 
            string comment)
		{
            DABaseOrder daOrder = Orders.Find(x => x.OrderId == order.Name || x.OrderId == order.FromEntrySignal);

            if (daOrder == null)
            {
                Print("OnOrderUpdate - order not found in our orderList");
                return;
            }

            daOrder.OnUpdate(order, limitPrice, stopPrice, quantity, filled, averageFillPrice,
                orderState, time, error, comment);

            if (daOrder.EntryIsCancelled())
            {
                Print("OnOrderUpdate - EntryOrder cancelled. Removed from order list.");
                Orders.Remove(daOrder);
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0 || CurrentBars[0] < BarsRequiredToTrade)
                return;

            if (Bars.IsFirstBarOfSession == true)
            {
                TradeCount = 0;
            }

            Indicators.Update();
            PowerHourPriceRange.Update();

            if (PowerHourPriceRange.MovedPastRange)
            {
                if (Time[0].TimeOfDay > TimeConverter.ToDataTimeOfDay("3:46pm", "Eastern Standard Time"))
                {   // not trading after 3:45 eastern
                    return;
                }

                if (Time[0].TimeOfDay > TimeConverter.ToDataTimeOfDay("3:58pm", "Eastern Standard Time"))
                {   // close positions
                    if (ManualMarketPosition.MarketPosition == MarketPosition.Long)
                    {
                        ExitLong("TimeExit", "PH_Long");
                    }
                    else if (ManualMarketPosition.MarketPosition == MarketPosition.Short)
                    {
                        ExitShort("TimeExit", "PH_Short");
                    }
                    return;
                }

                // here we can still trade
                if (ManualMarketPosition.MarketPosition == MarketPosition.Flat && TradeCount == 0)
                {   // we can only check for a new trade trigger when we have
                    // no position
                    TradeTriggerChecker ttc = new TradeTriggerChecker(this);
                    ttc.PowerHourPriceRange = PowerHourPriceRange;
                    ttc.Indicators = Indicators;
                    ttc.VWAPFilterEnabled = VWAPFilterEnabled;
                    ttc.PriorSessionFilterEnabled = PriorSessionFilterEnabled;

                    OrderTicket oip = ttc.Triggered();
                    if (oip.Type != DAOrderType.None)
                    {
                        DABaseOrder order = new DABaseOrder(this, oip);
                        // first add to list so it can be found
                        Orders.Add(order);
                        // then enter the orders
                        order.Submit();
                        TradeCount++;
                    }
                }
            }
        }
    }
}

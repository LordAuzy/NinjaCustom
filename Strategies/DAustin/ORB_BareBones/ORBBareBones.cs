using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.ORB_BareBones;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Markup;

namespace NinjaTrader.NinjaScript.Strategies
{ 
    public class ORBStrategy : Strategy
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

        private TimeWindowPriceRange _preMarketBig = null;
        public TimeWindowPriceRange PreMarketBig
        {
            get
            {
                if (_preMarketBig == null)
                {
                    _preMarketBig = new TimeWindowPriceRange(this, "4:30am", 178, "Eastern Standard Time");
                }
                return _preMarketBig;
            }

            private set { _preMarketBig = value; }
        }

        private TimeWindowPriceRange _preMarketSmall = null;
        public TimeWindowPriceRange PreMarketSmall
        {
            get
            { 
                if (_preMarketSmall == null)
                {
                     _preMarketSmall = new TimeWindowPriceRange(this, "7:00am", 178, "Eastern Standard Time");
                }
                return _preMarketSmall;
            }

            private set { _preMarketSmall = value; }
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

        private TradeManager _tradeManager = null;
        public TradeManager TradeManager
        {
            get
            {
                if (_tradeManager == null)
                {
                    _tradeManager = new TradeManager(this, Orders);
                    _tradeManager.EnableStopLossBreakEven = EnableStopLossBreakEven;
                    _tradeManager.EnableTrailingStopLoss = EnableTrailingStopLoss;
                    _tradeManager.MaxTradeMinutes = MaxTradeMinutes;
                    _tradeManager.ManualMarketPosition = ManualMarketPosition;
                }
                return _tradeManager;
            }

            private set { _tradeManager = value; }
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

        public OrderFlowVWAP AVWAP { get; private set; }

        public List<DAOrder> Orders { get; } = new List<DAOrder>();
        #endregion

        #region NinjascriptProperties
        [NinjaScriptProperty]
        [Range(5, 45)]
        [Display(Name = "Opening Range Minutes", Description = "Duration of the opening range in minutes", Order = 1, GroupName = "Parameters")]
        public int OpeningRangeMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVolumeFiltering", Description = "EnableVolumeFiltering", Order = 2, GroupName = "Parameters")]
        public bool EnableVolumeFiltering { get; set; }

        [NinjaScriptProperty]
        [Range(0.2, 2.5)]
        [Display(Name = "VolumeCheckHowFarAboveAverage", Description = "VolumeCheckHowFarAboveAverage", Order = 3, GroupName = "Parameters")]
        public double VolumeCheckHowFarAboveAverage { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableRangeWidthCheck", Description = "EnableRangeWidthCheck", Order = 4, GroupName = "Parameters")]
        public bool EnableRangeWidthCheck { get; set; }

        [NinjaScriptProperty]
        [Range(50, 150)]
        [Display(Name = "ORMaxWidth", Description = "MaxRangeWidthPercentOfAverage", Order = 5, GroupName = "Parameters")]
        public int ORMaxWidth { get; set; }

        [NinjaScriptProperty]
        [Range(10, 50)]
        [Display(Name = "ORMinWidth", Description = "MinRangeWidthPercentOfAverage", Order = 6, GroupName = "Parameters")]
        public int ORMinWidth { get; set; }

        [NinjaScriptProperty]
        [Range(45, 180)]
        [Display(Name = "TradingMinutes", Description = "Minutes from NYOpen you can trade", Order = 7, GroupName = "Parameters")]
        public int TradingMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableSLBE", Description = "EnableSLBE", Order = 8, GroupName = "Parameters")]
        public bool EnableStopLossBreakEven { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableTrailingSL", Description = "EnableTrailingSL", Order = 9, GroupName = "Parameters")]
        public bool EnableTrailingStopLoss { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 3.0)]
        [Display(Name = "TPMultiple", Description = "TakeProfitMultiple", Order = 10, GroupName = "Parameters")]
        public double TakeProfitMultiple { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVWAP", Description = "EnableVWAP", Order = 11, GroupName = "Parameters")]
        public bool EnableVWAP { get; set; }

        [NinjaScriptProperty]
        [Range(0, 300)]
        [Display(Name = "MaxTradeMinutes", Description = "MaxTradeMinutes", Order = 12, GroupName = "Parameters")]
        public int MaxTradeMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "PreMarketFiltering", Description = "PreMarketFiltering", Order = 13, GroupName = "Parameters")]
        public bool PreMarketFiltering { get; set; }
        #endregion

        #region Overrides
        protected override void OnStateChange()
        {
            Print("OnStateChange: state=" + State);

            if (State == State.SetDefaults)
            {
                Description = @"Opening Range Breakout (ORB) Strategy with Bracket Orders";
                Name = "ORBStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = false;  // Or true if you want auto-exit at session close
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;  // Standard safe default
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

                // Default parameter values
                OpeningRangeMinutes = 30;
                EnableVolumeFiltering = true;
                VolumeCheckHowFarAboveAverage = 1.5;
                EnableRangeWidthCheck = true;
                TradingMinutes = 60;
                ORMaxWidth = 80;
                ORMinWidth = 20;
                EnableStopLossBreakEven = false;
                EnableTrailingStopLoss = false;
                TakeProfitMultiple = 1;
                EnableVWAP = true;
                MaxTradeMinutes = 0;
                PreMarketFiltering = false;
            }
            else if (State == State.Configure)
            {

            }
            else if (State == State.DataLoaded)
            {
                ORBIndicators.Initialize();
            }
            else if (State == State.Historical)
            {

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
            {
                // the current update is not coming from the primary data series
                return;
            }

            PreMarketBig.Update();
            PreMarketSmall.Update();
            OpeningRange.Update();
            ORBIndicators.Update();

            if (CurrentBars[0] < BarsRequiredToTrade)
                return;

            if (ManualMarketPosition.MarketPosition != MarketPosition.Flat)
            {   // we have a trade in progress. See if there is anything to do.
                TradeManager.ManageOpenOrders();

            }

            // Breakout logic after range is set
            // MarketPosition.Flat means your trading strategy currently holds no
            // open positions (neither long nor short) for a specific instrument or account
            else if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow() && ManualMarketPosition.MarketPosition == MarketPosition.Flat)
            {
                DAOrder order = null;
                TradeTriggerChecker ttc = new TradeTriggerChecker(this, OpeningRange, ORBIndicators);
                ttc.BreakoutCandleVolumeCheckEnabled = EnableVolumeFiltering;
                ttc.VolumeCheckHowFarAboveAverage = VolumeCheckHowFarAboveAverage;
                ttc.OpeningRangeWidthCheckEnabled = EnableRangeWidthCheck;
                ttc.OpeningRangeMaxWidth = ORMaxWidth;
                ttc.OpeningRangeMinWidth = ORMinWidth;
                ttc.VWAPCheckEnabled = EnableVWAP;
                ttc.ExtermeInbalanceCheckEnabled = true;
                ttc.OpeningRangeHistory = OpeningRangeHistory;
                ttc.PreMarketBig = PreMarketBig;
                ttc.PreMarketSmall = PreMarketSmall;
                ttc.PreMarketFilteringEnabled = PreMarketFiltering;

                DAOrderType ot = ttc.Triggered();
                if (ot != DAOrderType.None) 
                {
                    order = new DAOrder(this, ot, OpeningRange);
                    order.TakeProfitMultiplier = TakeProfitMultiple;
                    // TODO: clear the order when completed. This is just a stop gap
                    Orders.Clear();
                    // first add to list so it can be found
                    Orders.Add(order);
                    // then enter the orders
                    order.Enter(2);
                }
            }
        }

        // triggers on any order state change
        protected override void OnOrderUpdate(
            Order order, 
            double limitPrice, 
            double stopPrice,
            int quantity, 
            int filled, 
            double averageFillPrice, 
            OrderState orderState,
            DateTime time, 
            ErrorCode error, 
            string nativeError)
        {
            DAOrder daOrder = Orders.Find(x => x.OrderId == order.Name || x.OrderId == order.FromEntrySignal);

            if (daOrder == null)
            {
                Print("OnOrderUpdate - order not found in our orderList");
                return;
            }

            daOrder.OnUpdate(order, limitPrice, stopPrice, quantity, filled, averageFillPrice,
                orderState, time, error, nativeError);

            TradeManager.OrderUpdated(daOrder);

            if (daOrder.EntryIsCancelled())
            {
                Print("OnOrderUpdate - EntryOrder cancelled. Removed from order list.");
                Orders.Remove(daOrder);
            }
        }

        // Triggers when an order is filled
        protected override void OnExecutionUpdate(
            Execution execution, 
            string executionId,
            double price, 
            int quantity, 
            MarketPosition marketPosition,
            string orderId, 
            DateTime time)
        {
            // see if we have that order
            DAOrder daOrder = Orders.Find(x => x.OrderId == execution.Name || x.OrderId == execution.Order.FromEntrySignal);

            if (daOrder == null)
            {
                Print("OnExecutionUpdate - order not found in our orderList");
            }
            else
            {
                daOrder.OnExecute(execution, executionId, price, quantity, marketPosition,
                    orderId, time);
                TradeManager.OrderExecuted(daOrder);
            }

            ManualMarketPosition.Update(execution.Order, quantity);
            if (ManualMarketPosition.MarketPosition == MarketPosition.Flat)
            {   // clear order list when flat
                Print("OnExecutionUpdate - MarketPosition is flat. Clearing orderList.");
                Orders.Clear();
            }
        }
        #endregion
    }
}   
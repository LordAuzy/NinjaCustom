//#define ORB_ENABLED
#define MEANREVERSION_ENABLED

#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.AccountData;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
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
	public class Mom921CrossStrat : StratBase
	{
		//What hours are open for trading
		//We will be trading high volume times in the NY session.
		//We drop the first minutes of the session because noise to signal
		//  ratio too high for automated  trading
		//The Open: 09:35 – 11:30 AM EST (Peak momentum).
		//The Close: 03:00 – 04:00 PM EST (The "Power Hour").
		//  times will be specified in minutes from session start for use in the strategy optimizer
		//
        #region Fields
        #endregion

        #region Properties
        public TimeConverter TimeConverter { get; } = new TimeConverter();

        // the time windows during which we will look for trade entries
        private TimeWindows _timeWindows921 = null;
        public TimeWindows TradingTimeWindows921
        {
            get
            {
                if (_timeWindows921 == null)
                {
                    _timeWindows921 = new TimeWindows(this, "9:30am", "Eastern Standard Time");
					_timeWindows921.AddTimeBlock(      // 9:35am-11:30am
                        anchorOffsetStart: new TimeSpan(0, minutes: 5, 0), 
                        anchorOffsetEnd: new TimeSpan(hours: 2, 0, 0));
                    _timeWindows921.AddTimeBlock(      // 2:30pm-3:45pm
                        anchorOffsetStart: new TimeSpan(hours: 5, minutes: 0, 0), 
                        anchorOffsetEnd: new TimeSpan(hours: 6, minutes: 15, 0));
                }
                return _timeWindows921;
            }

            private set { _timeWindows921 = value; }
        }

        private TimeWindows _timeWindowsORB = null;
        public TimeWindows TradingTimeWindowsORB
        {
            get
            {
                if (_timeWindowsORB == null)
                {
                    _timeWindowsORB = new TimeWindows(this, "9:30am", "Eastern Standard Time");
                    _timeWindowsORB.AddTimeBlock(      // 9:35am-11:30am
                        anchorOffsetStart: new TimeSpan(0, minutes: 5, 0),
                        anchorOffsetEnd: new TimeSpan(hours: 2, 0, 0));
                    _timeWindowsORB.AddTimeBlock(      // 2:30pm-3:45pm
                        anchorOffsetStart: new TimeSpan(hours: 5, minutes: 0, 0),
                        anchorOffsetEnd: new TimeSpan(hours: 6, minutes: 15, 0));
                }
                return _timeWindowsORB;
            }

            private set { _timeWindowsORB = value; }
        }

        private DAMomCrossIndicators _indicators = null;
        public DAMomCrossIndicators Indicators
        {
            get
            {
                if (_indicators == null)
                {
                    _indicators = new DAMomCrossIndicators(this);
                }
                return _indicators;
            }

            private set { _indicators = value; }
        }

        public List<DABaseOrder> Orders { get; } = new List<DABaseOrder>();
        public Dictionary<string, IEntryConditionsEvaluator> EntryConditionsEvaluatortList { get; } = new Dictionary<string, IEntryConditionsEvaluator>();

        public int TradeCount { get; set; } = 0;

        #endregion

        #region NinjascriptProperties
#if ORB_ENABLED
        [NinjaScriptProperty]
        [Range(5, 45)]
        [Display(Name = "Opening Range Minutes", Description = "Duration of the opening range in minutes", Order = 1, GroupName = "Parameters")]
        public int OpeningRangeMinutes { get; set; }

        [NinjaScriptProperty]
        [Range(45, 180)]
        [Display(Name = "TradingMinutes", Description = "Minutes from NYOpen you can trade", Order = 2, GroupName = "Parameters")]
        public int TradingMinutes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVolumeFiltering", Description = "EnableVolumeFiltering", Order = 3, GroupName = "Parameters")]
        public bool EnableVolumeFiltering { get; set; }

        [NinjaScriptProperty]
        [Range(0.2, 2.5)]
        [Display(Name = "VolumeCheckHowFarAboveAverage", Description = "VolumeCheckHowFarAboveAverage", Order = 4, GroupName = "Parameters")]
        public double VolumeCheckHowFarAboveAverage { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableRangeWidthCheck", Description = "EnableRangeWidthCheck", Order = 5, GroupName = "Parameters")]
        public bool EnableRangeWidthCheck { get; set; }

        [NinjaScriptProperty]
        [Range(50, 150)]
        [Display(Name = "ORMaxWidth", Description = "MaxRangeWidthPercentOfAverage", Order = 6, GroupName = "Parameters")]
        public int ORMaxWidth { get; set; }

        [NinjaScriptProperty]
        [Range(10, 50)]
        [Display(Name = "ORMinWidth", Description = "MinRangeWidthPercentOfAverage", Order = 7, GroupName = "Parameters")]
        public int ORMinWidth { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EnableVWAP", Description = "EnableVWAP", Order = 8, GroupName = "Parameters")]
        public bool EnableVWAP { get; set; }

#endif
#if MEANREVERSION_ENABLED
        [NinjaScriptProperty]
        [Display(Name = "BB Period", GroupName = "1. Indicators", Order = 1)]
        public int BBPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "BB StdDev", GroupName = "1. Indicators", Order = 2)]
        public double BBDev { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Period", GroupName = "1. Indicators", Order = 3)]
        public int RSIPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Oversold", GroupName = "1. Indicators", Order = 4)]
        public int RSIOversold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RSI Overbought", GroupName = "1. Indicators", Order = 5)]
        public int RSIOverbought { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA Period (mean)", GroupName = "1. Indicators", Order = 6)]
        public int EMAPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "% from EMA for entry", GroupName = "1. Indicators", Order = 7)]
        public double PercentFromEMA { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ADX Period", GroupName = "1. Indicators", Order = 8)]
        public int ADXPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ADX Threshold (skip if >=)", GroupName = "1. Indicators", Order = 9)]
        public int ADXThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop Distance (points)", GroupName = "2. Risk", Order = 1)]
        public double StopPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Default Quantity", GroupName = "2. Risk", Order = 2)]
        public int DefaultQty { get; set; }
#endif
        #endregion

        protected override void OnStateChange()
		{
            base.OnStateChange();

            if (State == State.SetDefaults)
			{
				Description									= @"Trigger 9 21 Crossover";
				Name										= "Mom921Cross";
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

                // set input parameter default values here
#if ORB_ENABLED
                OpeningRangeMinutes = 15;
                TradingMinutes = 120;
                EnableVolumeFiltering = false;
                VolumeCheckHowFarAboveAverage = 1.5;
                EnableRangeWidthCheck = false;
                ORMaxWidth = 80;
                ORMinWidth = 20;
                EnableVWAP = false;
#endif
#if MEANREVERSION_ENABLED
                // Default parameter values (optimized for MNQ)
                BBPeriod = 20;
                BBDev = 2.0;
                RSIPeriod = 14;
                RSIOversold = 30;      // use 25 for stricter
                RSIOverbought = 70;
                EMAPeriod = 50;
                PercentFromEMA = 1.0;
                ADXPeriod = 14;
                ADXThreshold = 25;
                StopPoints = 40;      // ~20-40 point stops common on MNQ
                DefaultQty = 1;
#endif
            }
            else if (State == State.Configure)
			{
#if ORB_ENABLED
                InputParams.OpeningRangeMinutes = OpeningRangeMinutes;
                InputParams.TradingMinutes = TradingMinutes;
                InputParams.BreakoutCandleVolumeCheckEnabled = EnableVolumeFiltering;
                InputParams.VolumeCheckHowFarAboveAverage = VolumeCheckHowFarAboveAverage;
                InputParams.OpeningRangeWidthCheckEnabled = EnableRangeWidthCheck;
                InputParams.OpeningRangeMaxWidth = ORMaxWidth;
                InputParams.OpeningRangeMinWidth = ORMinWidth;
                InputParams.VWAPCheckEnabled = EnableVWAP;
#endif
            }
            else if (State == State.DataLoaded)
            {
                Indicators.Initialize();

#if ORB_ENABLED
                TradeContext tc = new TradeContext();
                tc.StateList = new List<TradeState>() 
                    { 
                        TradeState.Idle, 
                        TradeState.EntryPending,
                        TradeState.TrailingStop,
                        TradeState.ExitPending,
                        TradeState.Exited
                    };
                tc.SetState(TradeState.Idle);
                tc.EntryConditionsEvaluator = GetEntryConditionsEvaluator("ECE-ORB");
                tc.EntryConditionsEvaluator.Reset();
                tc.FlattenTOD = TimeConverter.ToDataTimeOfDay("3:55pm", "Eastern Standard Time");
                TradeManagerV2.AddTradeContext(tc);
                TradeManagerV2.Indicators = Indicators;
#endif

                // create the 9/21 cross TradeCoontext and add it to the trade manager
                //TradeContext tc = new TradeContext();
                //tc.EntryConditionsEvaluator = GetEntryConditionsEvaluator("ECE-PriceTouch");
                //tc.FlattenTOD = TimeConverter.ToDataTimeOfDay("3:55pm", "Eastern Standard Time");
                //TradeManagerV2.AddTradeContext(tc);
                //TradeManagerV2.Indicators = Indicators;

                // create the ORB TradeContext and add it to the trade manager
                //tc = new TradeContext();
                //tc.EntryConditionsEvaluator = EntryConditionsEvaluatorORB;
                //tc.FlattenTOD = TimeConverter.ToDataTimeOfDay("3:55pm", "Eastern Standard Time");
                //TradeManagerV2.AddTradeContext(tc);
                //TradeManagerV2.Indicators = Indicators;
                //TradeManagerV2.FlattenTOD = TimeConverter.ToDataTimeOfDay("3:55pm", "Eastern Standard Time");
            }
            else if (State == State.Terminated)
            {

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
            base.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);
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
            base.OnOrderUpdate(order, limitPrice, stopPrice, quantity, filled, averageFillPrice, orderState, time, error, comment);
        }

        protected override void OnBarUpdate()
		{
            if (BarsInProgress != 0)
            {   // the current update is not coming from the primary data series
                return;
            }

            Indicators.Update();

            base.OnBarUpdate();
        }
    }
}

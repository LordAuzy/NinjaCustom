#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
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
	public class Strat_MeanReversion : StratBase
	{
        //+--------------------------------------------------------------------------------------+
        //| This is the exact "most profitable" mean reversion strategy I described for MNQ       |
        //| (Micro E-mini Nasdaq-100 futures) on the 15-minute chart.                            |
        //|                                                                                      |
        //| Rules implemented:                                                                   |
        //|   • Bollinger Bands (20,2) + RSI(14) + 50 EMA                                        |
        //|   • ADX < 25 (range filter)                                                          |
        //|   • Longs only when price > daily 200 EMA                                            |
        //|   • Entry on close back inside band after spike + RSI extreme + 1% from 50 EMA       |
        //|   • Stop: fixed points (default 40 points = ~$80 risk on 1 contract)                 |
        //|   • Exit: at middle band OR RSI crosses 50                                           |
        //|   • Intraday only (exits at session close, no entries outside RTH)                   |
        //|                                                                                      |
        //| Apply to a 15-minute MNQ chart (continuous contract recommended).                    |
        //| Start with 1 contract. Paper trade first!                                            |
        //+--------------------------------------------------------------------------------------+

        #region Fields
        #endregion

        #region Properties
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
        [Display(Name = "StopATRMultiplier", GroupName = "1. Indicators", Order = 10)]
        public double StopATRMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Period", GroupName = "1. Indicators", Order = 11)]
        public int ATRPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "RiskAccountPercent", GroupName = "1. Indicators", Order = 12)]
        public int RiskAccountPercent { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "MaxTradeMinutes", GroupName = "1. Indicators", Order = 13)]
        public int MaxTradeMinutes { get; set; }

        #endregion

        protected override void OnStateChange()
		{
            base.OnStateChange();

            if (State == State.SetDefaults)
			{
                Description = "Mean reversion BB + RSI + EMA strategy for MNQ futures (15-min)";
                Name = "DA_MeanReversion";
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
				BarsRequiredToTrade							= 200; // enough so the200ema is accurate

                // set optimization parameter default values
                BBPeriod = 20;
                BBDev = 2.0;
                RSIPeriod = 14;
                RSIOversold = 30;      // use 25 for stricter
                RSIOverbought = 70;
                EMAPeriod = 50;
                PercentFromEMA = 1.0;
                ADXPeriod = 14;
                ADXThreshold = 25;
                StopATRMultiplier = 1.2; // 1.5 is more conservative, 1.0 is more aggressive
                ATRPeriod = 14;
                RiskAccountPercent = 2;
                MaxTradeMinutes = 0; // no max trade length, exit based on indicators and time of day only

                GetOptimizationParameters("OP-MEANREVERSION").UpdateFromStrat();
            }
            else if (State == State.Configure)
			{
                // Add daily series for 200 EMA trend filter
                AddDataSeries(BarsPeriodType.Day, 1);
            }
            else if (State == State.DataLoaded)
            {
                IOptimizationParameters optParams = GetOptimizationParameters("OP-MEANREVERSION");
                optParams.UpdateFromStrat();

                Indicators_MeanReversion indicators = GetIndicators("IDC-MEANREVERSION") as Indicators_MeanReversion;
                indicators.OptParams = optParams;
                indicators.Initialize();

                IEntryConditionsEvaluator ece = GetEntryConditionsEvaluator("ECE-MEANREVERSION");
                ece.OrderIdPrefix = "DAMR";
                ece.Reset();
                ece.Indicators = indicators;
                ece.OptParams = optParams;

                TradeContext tc = new TradeContext();
                tc.StateList = new List<TradeState>() 
                    { 
                        TradeState.Idle, 
                        TradeState.FillPending,
                        TradeState.InPosition,
                        TradeState.Exited
                    };
                tc.SetState(TradeState.Idle);
                tc.EntryConditionsEvaluator = ece;
                TradeManager.FlattenTOD = new TimeConverter().ToDataTimeOfDay("3:55pm", "Eastern Standard Time");
                TradeManager.AddTradeContext(tc);
                TradeManager.Indicators = indicators;
            }
            else if (State == State.Terminated)
            {

            }
        }

        protected override void OnBarUpdate()
		{
            if (BarsInProgress != 0)
            {   // the current update is not coming from the primary data series
                return;
            }
            base.OnBarUpdate();
        }
    }
}

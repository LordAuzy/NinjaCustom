using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.DataCollectors;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.SuperDomColumns;
using NTRes.NinjaTrader.Gui.Tools.Account;
using Rules1;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static NinjaTrader.CQG.ProtoBuf.MarketDataSubscription.Types;
using static NinjaTrader.CQG.ProtoBuf.Quote.Types;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators
{
    [StrategyComponentId("ECE-TREND")]
    public class ECE_TREND : EntryConditionsEvaluatorBase
    {
        #region Properties
        public Indicators_TREND IndicatorsTREND { get { return Indicators as Indicators_TREND; } }
        public OptimizationParameters_TREND OptParamsTREND { get { return OptParams as OptimizationParameters_TREND; } }
        public ECE_TREND_DataCollector DataCollector { get; private set; } = new ECE_TREND_DataCollector();

        // Break-and-retest state (reset each session)
        private bool _breakoutLongOccurred = false;
        private bool _breakoutShortOccurred = false;
        private bool _retestLongOccurred = false;
        private bool _retestShortOccurred = false;
        private DateTime _breakRetestDate = DateTime.MinValue;

        //chatgpt variables reset when new session starts
        private int marketBias = 0;
        // 1 = bullish
        // -1 = bearish
        // 0 = neutral
        private int bullPullbackBars = 0;
        private int bearPullbackBars = 0;
        private bool tradedCurrentBias = false;


        private int pullbackBars = 0;
        #endregion

        #region constructors
        public ECE_TREND(StratBase strat)
        {
            Strategy = strat;
            OrderIdPrefix = "DATREND";
            Initialize();
        }
        #endregion

        #region PublicMethods
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            Indicators_TREND Indicators = IndicatorsTREND;

            //if (Indicators.EntryTimeWindows != null && !Indicators.EntryTimeWindows.IsInTimeWindow())
            //{   // not in an entry time window
            //    return null;
            //}

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            switch (OptParamsTREND.Entry.TriggerType)
            {
                case EntryTriggerType.ChatGPT:
                    orderTicket = EvaluateChatGPT();
                    // Implement logic for indicator crossover trigger
                    break;
                case EntryTriggerType.Claude:
                    break;

                case EntryTriggerType.Grok:
                    break;

                default:
                    // just do nothing
                    break;
            }

            if (orderTicket != null)
            {
                orderTicket.AllowedRiskPercentOfAccount = OptParamsTREND.EquityRiskPct;
            }

            return orderTicket;
        }

        public OrderTicket EvaluateChatGPT()
        {
            return EvaluateChatGPTV2();

            OrderTicket orderTicket = null;
            Indicators_TREND Indicators = IndicatorsTREND;
            TimeWindowPriceRange OpeningRange = Indicators.OpeningRange;

            if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow())
            {
                DataCollector.InTradeTimeWindowCount++;

                double currentATR = Indicators.atr[0];

                // Bias
                bool bullishBias = Strategy.Close[0] > OpeningRange.RangeHigh && (Strategy.Close[0] - OpeningRange.RangeHigh) > OptParamsTREND.GPT_MinORBreakATR * currentATR;
                bool bearishBias = Strategy.Close[0] < OpeningRange.RangeLow && (OpeningRange.RangeLow - Strategy.Close[0]) > OptParamsTREND.GPT_MinORBreakATR * currentATR;
                // Filters
                if (Indicators.adx[0] >= OptParamsTREND.GPT_MinADX)
                {
                    DataCollector.PassedMinAdxFilterCount++;

                    DAVWAP vwap = Indicators.NYSessionAnchoredVWAP;

                    double vwapDist = Math.Abs(Strategy.Close[0] - vwap.Value) / currentATR;
                    if (vwapDist <= OptParamsTREND.GPT_MaxVWAPDistanceATR)
                    {
                        DataCollector.PassedMaxVWAPDistanceFilterCount++;

                        // Pullback logic
                        if (bullishBias)
                        {
                            if (Strategy.Close[0] < Indicators.emaFast[0])
                            {
                                pullbackBars++;
                            }
                            else
                            {
                                pullbackBars = 0;
                            }
                        }

                        if (bearishBias)
                        {
                            if (Strategy.Close[0] > Indicators.emaFast[0])
                            {
                                pullbackBars++;
                            }
                            else
                            {
                                pullbackBars = 0;
                            }
                        }

                        bool validPullback =
                            pullbackBars >= OptParamsTREND.GPT_PullbackMinBars &&
                            pullbackBars <= OptParamsTREND.GPT_PullbackMaxBars &&
                            Math.Abs(Strategy.Close[0] - Indicators.emaFast[0]) / currentATR < OptParamsTREND.GPT_PullbackMaxATR;

                        if (validPullback)
                        {
                            DataCollector.IsValidPullbackCount++;
                        }

                        // Liquidity sweep
                        bool sweepLong = Strategy.Low[0] < Strategy.Low[1] && Strategy.Close[0] > Strategy.Low[1];
                        bool sweepShort = Strategy.High[0] > Strategy.High[1] && Strategy.Close[0] < Strategy.High[1];

                        double stopDist = currentATR * OptParamsTREND.GPT_RiskATR;

                        // LONG
                        if (bullishBias && validPullback && sweepLong && Strategy.Close[0] > Strategy.High[1])
                        {
                            DataCollector.LongTradeTriggeredCount++;

                            double initialStop = Strategy.Close[0] - stopDist;
                            orderTicket = CreateOrderTicket(DAOrderType.Long, initialStop);
                            //EnterLong(2, "L"); 
                            //SetStopLoss("L", CalculationMode.Price, Close[0] - stopDist, false);
                            //SetProfitTarget("L", CalculationMode.Price, Close[0] + stopDist * RewardR);
        }

                        // SHORT
                        if (bearishBias && validPullback && sweepShort && Strategy.Close[0] < Strategy.Low[1])
                        {
                            DataCollector.ShortTradeTriggeredCount++;

                            double initialStop = Strategy.Close[0] + stopDist;
                            orderTicket = CreateOrderTicket(DAOrderType.Short, initialStop);


                            //SetStopLoss("S", CalculationMode.Price, Close[0] + stopDist, false);
                            //SetProfitTarget("S", CalculationMode.Price, Close[0] - stopDist * RewardR);
                        }
                    }
                }

            }
            return orderTicket;
        }

        public OrderTicket EvaluateChatGPTV2()
        {
            OrderTicket orderTicket = null;
            Indicators_TREND Indicators = IndicatorsTREND;
            TimeWindowPriceRange OpeningRange = Indicators.OpeningRange;

            if (OpeningRange.RangeSet && OpeningRange.IsInTradeTimeWindow())
            {
                DataCollector.InTradeTimeWindowCount++;

                double currentATR = Indicators.atr[0];

                // ==========================================
                // PERSISTENT BIAS DETECTION
                // ==========================================

                if (Strategy.Close[0] > OpeningRange.RangeHigh +
                    (OptParamsTREND.GPT_MinORBreakATR * currentATR))
                {
                    if (marketBias != 1)
                    {
                        tradedCurrentBias = false;
                        bullPullbackBars = 0;
                    }

                    marketBias = 1;
                }

                if (Strategy.Close[0] < OpeningRange.RangeLow -
                    (OptParamsTREND.GPT_MinORBreakATR * currentATR))
                {
                    if (marketBias != -1)
                    {
                        tradedCurrentBias = false;
                        bearPullbackBars = 0;
                    }

                    marketBias = -1;
                }

                // FIRST PULLBACK ONLY
                if (tradedCurrentBias)
                    return null;

                // ==========================================
                // ADX FILTER
                // ==========================================

                if (Indicators.adx[0] < OptParamsTREND.GPT_MinADX)
                    return null;

                DataCollector.PassedMinAdxFilterCount++;

                // ==========================================
                // VWAP FILTER
                // ==========================================

                DAVWAP vwap = Indicators.NYSessionAnchoredVWAP;

                double vwapDist =
                    Math.Abs(Strategy.Close[0] - vwap.Value) / currentATR;

                if (vwapDist > OptParamsTREND.GPT_MaxVWAPDistanceATR)
                    return null; 

                DataCollector.PassedMaxVWAPDistanceFilterCount++;

                // ==========================================
                // EMA SLOPE FILTER
                // HUGE improvement on MNQ
                // ==========================================

                bool emaBullSlope =
                    Indicators.emaFast[0] > Indicators.emaFast[5];

                bool emaBearSlope =
                    Indicators.emaFast[0] < Indicators.emaFast[5];

                // ==========================================
                // PULLBACK TRACKING
                // ==========================================

                if (marketBias == 1)
                {
                    if (Strategy.Low[0] < Indicators.emaFast[0])
                    {
                        bullPullbackBars++;
                    }
                    else if (
                        Strategy.Close[0] > Indicators.emaFast[0] &&
                        Strategy.Close[1] > Indicators.emaFast[1])
                    {
                        bullPullbackBars = 0;
                    }
                }

                if (marketBias == -1)
                {
                    if (Strategy.High[0] > Indicators.emaFast[0])
                    {
                        bearPullbackBars++;
                    }
                    else if (
                        Strategy.Close[0] < Indicators.emaFast[0] &&
                        Strategy.Close[1] < Indicators.emaFast[1])
                    {
                        bearPullbackBars = 0;
                    }
                }

                bool validBullPullback =
                    bullPullbackBars >= OptParamsTREND.GPT_PullbackMinBars &&
                    bullPullbackBars <= OptParamsTREND.GPT_PullbackMaxBars;

                bool validBearPullback =
                    bearPullbackBars >= OptParamsTREND.GPT_PullbackMinBars &&
                    bearPullbackBars <= OptParamsTREND.GPT_PullbackMaxBars;

                if (validBullPullback || validBearPullback)
                    DataCollector.IsValidPullbackCount++;

                // ==========================================
                // LIQUIDITY SWEEP
                // loosened
                // ==========================================

                bool sweepLong =
                    Strategy.Low[0] < Strategy.MIN(Strategy.Low, 3)[1];

                bool sweepShort =
                    Strategy.High[0] > Strategy.MAX(Strategy.High, 3)[1];

                // ==========================================
                // MOMENTUM TRIGGER
                // loosened
                // ==========================================

                bool longTrigger =
                    Strategy.High[0] > Strategy.High[1];

                bool shortTrigger =
                    Strategy.Low[0] < Strategy.Low[1];

                double stopDist =
                    currentATR * OptParamsTREND.GPT_RiskATR;

                // ==========================================
                // LONG
                // ==========================================

                if (marketBias == 1 &&
                    emaBullSlope &&
                    validBullPullback &&
                    sweepLong &&
                    longTrigger)
                {
                    double initialStop = Strategy.Close[0] - stopDist;
                    DataCollector.LongTradeTriggeredCount++;

                    if (OptParamsTREND.Entry.OrderType == EntryOrderType.StopMarket)
                    {
                        double entryPrice = Strategy.High[0] + Strategy.TickSize;

                        // Ensure valid stop placement
                        double bid = Strategy.GetCurrentBid();
                        if (entryPrice <= bid)
                        {
                            entryPrice = bid + Strategy.TickSize;
                        }
                        initialStop = entryPrice - stopDist;
                        orderTicket = CreateOrderTicket(
                            orderType: DAOrderType.LongStopMarket, 
                            initialStop: initialStop, 
                            initialTP: 0,
                            entryPrice: entryPrice, 
                            orderExpiryBars: OptParamsTREND.Entry.OrderExpiryBars);
                    }
                    else
                    {
                        orderTicket = CreateOrderTicket(DAOrderType.Long, initialStop);
                    }
                    tradedCurrentBias = true;
                }

                // ==========================================
                // SHORT
                // ==========================================

                if (marketBias == -1 &&
                    emaBearSlope &&
                    validBearPullback &&
                    sweepShort &&
                    shortTrigger)
                {
                    double initialStop = Strategy.Close[0] + stopDist;
                    DataCollector.ShortTradeTriggeredCount++;

                    if (OptParamsTREND.Entry.OrderType == EntryOrderType.StopMarket)
                    {
                        double entryPrice = Strategy.Low[0] - Strategy.TickSize;

                        // Ensure valid stop placement
                        double ask = Strategy.GetCurrentAsk();
                        if (entryPrice >= ask)
                        {
                            entryPrice = ask - Strategy.TickSize;
                        }
                        initialStop = entryPrice + stopDist;
                        orderTicket = CreateOrderTicket(
                            orderType: DAOrderType.ShortStopMarket, 
                            initialStop: initialStop, 
                            initialTP: 0,
                            entryPrice: entryPrice, 
                            orderExpiryBars: OptParamsTREND.Entry.OrderExpiryBars);
                    }
                    else
                    {
                        orderTicket = CreateOrderTicket(DAOrderType.Short, initialStop);
                    }
                    tradedCurrentBias = true;
                }
            }
            return orderTicket;
        }

        public OrderTicket EvaluateClaude()
        {
            OrderTicket orderTicket = null;

            return orderTicket;
        }

        public OrderTicket EvaluateGrok()
        {
            OrderTicket orderTicket = null;


            return orderTicket;
        }

        public void Reset()
        {
            Initialize();
        }

        public override void SessionReset()
        {
            base.SessionReset();

            // Properties that reset each session
            _breakoutLongOccurred = false;
            _breakoutShortOccurred = false;
            _retestLongOccurred = false;
            _retestShortOccurred = false;
            _breakRetestDate = DateTime.MinValue;

            //chatgpt variables reset when new session starts
            marketBias = 0;
            bullPullbackBars = 0;
            bearPullbackBars = 0;
            tradedCurrentBias = false;
            pullbackBars = 0;
        }

        public void Initialize()
        {
            _breakoutLongOccurred = false;
            _breakoutShortOccurred = false;
            _retestLongOccurred = false;
            _retestShortOccurred = false;
            _breakRetestDate = DateTime.MinValue;
        }
        #endregion

        #region VirtualMethods
        #endregion
    }
}

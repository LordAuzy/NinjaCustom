using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
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
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using NLog;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Custom.DAustin.Common.ScheduleFilter;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Calendars;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB
{
    [StrategyComponentId("ECE-VWAPPB")]
    public class ECE_VWAPPB : EntryConditionsEvaluatorBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Logger _loggerTP = null;
        private bool _fullyInitialized = false;
        private Logger LoggerTP
        {
            get
            {
                if (_loggerTP == null || _fullyInitialized == false)
                {
                    (_loggerTP, _fullyInitialized) = Strategy.CreateLoggerWithBaseProps(_logger);
                }
                return _loggerTP;
            }
        }

        #region Properties
        public Indicators_VWAPPB IndicatorsVWAPPB { get { return Indicators as Indicators_VWAPPB; } }
        public OptimizationParameters_VWAPPB OptParamsVWAPPB { get { return OptParams as OptimizationParameters_VWAPPB; } }
        public ECE_VWAPPB_DataCollector DataCollector { get; private set; } = new ECE_VWAPPB_DataCollector();

        // Break-and-retest state (reset each session)
        private bool _breakoutLongOccurred = false;
        private bool _breakoutShortOccurred = false;
        private bool _retestLongOccurred = false;
        private bool _retestShortOccurred = false;
        private DateTime _breakRetestDate = DateTime.MinValue;
        #endregion

        #region constructors
        public ECE_VWAPPB(StratBase strat)
        {
            Strategy = strat;
            Initialize();
        }
        #endregion

        #region Overrides
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            VWAPPB_EntryParameters EntryOptParams = OptParamsVWAPPB.Entry;
            GeneralParameters GenOptParams = OptParamsVWAPPB.General;
            Indicators_VWAPPB Indicators = IndicatorsVWAPPB;
            EMA fastEMA = Indicators.Entry.FastEMA;
            EMA slowEMA = Indicators.Entry.SlowEMA;
//            DAVWAP vwap = Indicators.NYSessionAnchoredVWAP;
//            double VWAPValue = Indicators.NYSessionAnchoredVWAP.Value;
            DAVWAPIndicator VWAP = Indicators.Entry.AnchoredVWAP;
            double VWAPValue = VWAP[0];
            double atrValue = Indicators.Entry.ATR[0];
            BiasFilter biasFilter = Indicators.BiasFilter;

            if (FOMCCalendar.IsFOMCDay(Strategy.Time[0]))
            {
                LoggerTP.Trace("Is FOMC Day");
                return null;
            }

            // for this strategy, we want to be especially strict about avoiding entries on NFP days
            // due to the high volatility and potential for slippage. Even if the entry conditions are met,
            // the risk of adverse price movements around the NFP release is significant. Therefore,
            // we will skip all entries on NFP days to protect the account from unexpected losses.
            if (NFPCalendar.IsNFPDay(Strategy.Time[0]))
            {
                LoggerTP.Trace("Is NFP Day");
                return null;
            }

            if (tradeContext.TradesTakenThisSession >= GenOptParams.MaxTradesPerSession)
            {   // max trades per session reached
                LoggerTP.Info($"Max trades per session reached: {tradeContext.TradesTakenThisSession}/{GenOptParams.MaxTradesPerSession}");
                return null;
            }


            if (Indicators.EntryTimeWindows != null && !Indicators.EntryTimeWindows.IsInTimeWindow())
            {   // not in an entry time window
                LoggerTP.Trace("Not in entry time window");
                return null;
            }

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                LoggerTP.Trace("In preload phase");
                return null;
            }

            TradingStance ts = biasFilter.GetCurrentTradingStance(Strategy.Time[0]);
            if (ts == TradingStance.None)
            {   // no trades allowed per bias filter
                LoggerTP.Trace("Trading stance is TradingStance.None");
                return null;
            }

            if (Strategy.CurrentBars[0] < Math.Max(EntryOptParams.ATRPeriod, EntryOptParams.SlowEMAPeriod))
            {   // not enough bars to calculate indicators
                LoggerTP.Trace("Not enough bars to calculate indicators");
                return null;
            }

            // put new code here for chatgpt No-Chop entry conditions . . .
            double currentPrice = Strategy.Close[0];

            // --- Basic indicators ---
            double emaSpread = Math.Abs(fastEMA[0] - slowEMA[0]);
            double vwapDistance = Math.Abs(currentPrice - VWAPValue);
            double vwapSlope = VWAPValue - VWAP[3];

            // =========================
            // 🚫 CHOP FILTER (CRITICAL)
            // =========================
            bool chopZone =
                vwapDistance < (EntryOptParams.MinVWAPDistanceATR * atrValue) ||
                Math.Abs(vwapSlope) < (EntryOptParams.MinVWAPSlopeATR * atrValue) ||
                emaSpread < (EntryOptParams.MinEMASpreadATR * atrValue);

            bool aboveVWAP =
                Strategy.Close[0] > VWAPValue &&
                Strategy.Close[1] > VWAP[1] &&
                Strategy.Close[2] > VWAP[2];
            bool belowVWAP =
                Strategy.Close[0] < VWAPValue &&
                Strategy.Close[1] < VWAP[1] &&
                Strategy.Close[2] < VWAP[2];

            if (aboveVWAP && (ts == TradingStance.LongOnly || ts == TradingStance.All))
            {
                // =========================
                // LONG SETUP
                // =========================
                DataCollector.AboveVWAPCount++;

                bool upTrend = aboveVWAP && fastEMA[0] > slowEMA[0];
                if (upTrend)
                {
                    DataCollector.UpTrendCount++;
                    if (chopZone)
                    {
                        DataCollector.UpTrendChopZoneCount++;
                    }
                }

                if (upTrend && !chopZone)
                {
                    double recentPullbackLow = Strategy.MIN(Strategy.Low, 3)[0];
                    double pullbackDistance = recentPullbackLow - VWAPValue;

                    bool validPullback =
                        pullbackDistance >= (-0.1 * atrValue) &&
                        pullbackDistance <= (EntryOptParams.MaxPullbackATR * atrValue);

                    bool bullishTrigger = Strategy.Close[0] > Strategy.Open[0];

                    if (validPullback)
                    {
                        DataCollector.ValidPullbackLongCount++;
                    }

                    if (bullishTrigger)
                    {
                        DataCollector.BullishTriggerCount++;
                    }


                    if (validPullback && bullishTrigger)
                    {
                        double swingLow = Strategy.MIN(Strategy.Low, 5)[0];
                        double initialStop = swingLow - (EntryOptParams.InitialStopATRBuffer * atrValue);

                        DataCollector.LongEntryTriggeredCount++;
                        // =========================
                        // ENTRY TYPE
                        // =========================
                        if (EntryOptParams.OrderType == EntryOrderType.StopMarket)
                        {
                            double entryPrice = Strategy.High[1] + Strategy.TickSize;

                            // 🚫 Skip if already triggered (avoid chasing)
                            if (currentPrice >= entryPrice)
                                return null;

                            // Ensure valid stop placement
                            double ask = Strategy.GetCurrentAsk();
                            if (entryPrice <= ask)
                                entryPrice = ask + Strategy.TickSize;

                            double risk = entryPrice - initialStop;
                            if (risk <= 0)
                                return null;

                            orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                            orderTicket.Type = DAOrderType.LongStopMarket;
                            orderTicket.Price = entryPrice;
                            orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);

                            if (EntryOptParams.OrderExpiryBars > 0)
                                orderTicket.StopExpiryBars = EntryOptParams.OrderExpiryBars;
                        }
                        else if (EntryOptParams.OrderType == EntryOrderType.Market)
                        {
                            // Optional: disable if you want pure stop-entry testing
                            orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                            orderTicket.Type = DAOrderType.Long;
                            orderTicket.Risk = FlexibleValue.FromPoints(currentPrice - initialStop, Strategy);
                        }
                    }
                }
            }
            else if (belowVWAP && (ts == TradingStance.ShortOnly || ts == TradingStance.All))
            {
                // =========================
                // SHORT SETUP
                // =========================
                DataCollector.BelowVWAPCount++;

                bool downTrend = belowVWAP && fastEMA[0] < slowEMA[0];
                if (downTrend)
                {
                    DataCollector.DownTrendCount++;
                    if (chopZone)
                    {
                        DataCollector.DownTrendChopZoneCount++;
                    }
                }

                if (downTrend && !chopZone)
                {
                    double recentPullbackHigh = Strategy.MAX(Strategy.High, 3)[0];
                    double pullbackDistance = VWAPValue - recentPullbackHigh;

                    bool validPullback =
                        pullbackDistance >= (-0.1 * atrValue) &&
                        pullbackDistance <= (EntryOptParams.MaxPullbackATR * atrValue);

                    bool bearishTrigger = Strategy.Close[0] < Strategy.Open[0];

                    if (validPullback)
                    {
                        DataCollector.ValidPullShortCount++;
                    }

                    if (bearishTrigger)
                    {
                        DataCollector.BearishTriggerCount++;
                    }


                    if (validPullback && bearishTrigger)
                    {
                        double swingHigh = Strategy.MAX(Strategy.High, 5)[0];
                        double initialStop = swingHigh + (EntryOptParams.InitialStopATRBuffer * atrValue);

                        DataCollector.ShortEntryTriggeredCount++;

                        if (EntryOptParams.OrderType == EntryOrderType.StopMarket)
                        {
                            double entryPrice = Strategy.Low[1] - Strategy.TickSize;

                            // 🚫 Skip if already triggered
                            if (currentPrice <= entryPrice)
                                return null;

                            // Ensure valid stop placement
                            double bid = Strategy.GetCurrentBid();
                            if (entryPrice >= bid)
                                entryPrice = bid - Strategy.TickSize;

                            double risk = initialStop - entryPrice;
                            if (risk <= 0)
                                return null;

                            orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                            orderTicket.Type = DAOrderType.ShortStopMarket;
                            orderTicket.Price = entryPrice;
                            orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);

                            if (EntryOptParams.OrderExpiryBars > 0)
                                orderTicket.StopExpiryBars = EntryOptParams.OrderExpiryBars;
                        }
                        else if (EntryOptParams.OrderType == EntryOrderType.Market)
                        {
                            orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                            orderTicket.Type = DAOrderType.Short;
                            orderTicket.Risk = FlexibleValue.FromPoints(initialStop - currentPrice, Strategy);
                        }
                    }
                }
            }

            if (orderTicket != null)
            {
                double riskMultiplier = Indicators.SizingFilter.GetCurrentSizingMultiplier(Strategy.Time[0]);
                double riskPct = OptParamsVWAPPB.General.EquityRiskPercent;

                orderTicket.AllowedRiskPercentOfAccount = riskPct * riskMultiplier;
            }
            return orderTicket;
        }
        #endregion

        #region PublicMethods
        public void Reset()
        {
            Initialize();
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

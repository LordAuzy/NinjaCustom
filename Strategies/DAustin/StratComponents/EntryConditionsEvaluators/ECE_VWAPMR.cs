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
    [StrategyComponentId("ECE-VWAPMR")]
    public class ECE_VWAPMR : EntryConditionsEvaluatorBase
    {
        #region Properties
        public Indicators_VWAPMR IndicatorsVWAPMR { get { return Indicators as Indicators_VWAPMR; } }
        public OptimizationParameters_VWAPMR OptParamsVWAPMR { get { return OptParams as OptimizationParameters_VWAPMR; } }
        public ECE_VWAPMR_DataCollector DataCollector { get; private set; } = new ECE_VWAPMR_DataCollector();

        // Break-and-retest state (reset each session)
        private bool _breakoutLongOccurred = false;
        private bool _breakoutShortOccurred = false;
        private bool _retestLongOccurred = false;
        private bool _retestShortOccurred = false;
        private DateTime _breakRetestDate = DateTime.MinValue;
        #endregion

        #region constructors
        public ECE_VWAPMR(StratBase strat)
        {
            Strategy = strat;
            Initialize();
        }
        #endregion

        #region PublicMethods
        // This method contains the "ChatGPT No-Chop" entry logic that was developed with the
        // assistance of ChatGPT. It incorporates a VWAP bias, EMA trend confirmation, and a
        // chop filter based on distance from VWAP and EMA spread. It also includes pullback
        // validation and bullish/bearish trigger conditions for more precise entries.
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            VWAPMR_EntryParameters EntryOptParams = OptParamsVWAPMR.Entry;
            Indicators_VWAPMR Indicators = IndicatorsVWAPMR;
            ATR atr = Indicators.Entry.ATR;
            EMA slowEMA = Indicators.SlowEMA;
            EMA trendEMA = Indicators.TrendEMA;
            double atrValue = atr[0];

            if (Indicators.EntryTimeWindows != null && !Indicators.EntryTimeWindows.IsInTimeWindow())
            {   // not in an entry time window
                return null;
            }

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                return null;
            }

            // distance from EMA filter
            if (Math.Abs(Strategy.Close[0] - slowEMA[0]) > (1.5 * atrValue))
                return null;

            double emaTrend = Math.Abs(slowEMA[0] - trendEMA[0]);

            if (emaTrend > (0.6 * atrValue))
                return null;

            DataCollector.CheckForEntryCount++;
            if (atrValue >= EntryOptParams.MinATRFilter)
            {
                DataCollector.PassedMinATRFilter++;

                double currentPrice = Strategy.Close[0];
                double vwap = Indicators.NYSessionAnchoredVWAP.Value;
                double vwapSlope = vwap - Indicators.NYSessionAnchoredVWAP.GetFromMostRecent(EntryOptParams.VWAPSlopeLookback);
                if (Math.Abs(vwapSlope) <= (EntryOptParams.MaxVWAPSlopeATR * atrValue))
                {
                    DataCollector.PassedMaxVWAPSlopeFilter++;

                    // =========================
                    // 📏 DISTANCE FROM VWAP
                    // =========================
                    double deviation = currentPrice - vwap;

                    if (Math.Abs(deviation) >= (EntryOptParams.DeviationATR * atrValue))
                    {
                        DataCollector.PassedDeviationFilter++;

                        // =========================
                        // 🟢 LONG SETUP
                        // =========================
                        if (currentPrice < vwap)
                        {
                            DataCollector.CurrentPriceBelowVWAPCount++;

                            bool bullishReversal =
                                Strategy.Close[0] > Strategy.Open[0] &&
                                Strategy.Close[0] > Strategy.High[1];

                            if (bullishReversal)
                            {
                                DataCollector.LongEntryTriggered++;

                                double entryPrice = currentPrice;
                                double initialStop = entryPrice - (EntryOptParams.StopATR * atrValue);

                                orderTicket = CreateOrderTicket(DAOrderType.Long, initialStop);
                            }
                        }
                        // =========================
                        // 🔴 SHORT SETUP
                        // =========================
                        else if (currentPrice > vwap)
                        {
                            DataCollector.CurrentPriceAboveVWAPCount++;

                            bool bearishReversal =
                                Strategy.Close[0] < Strategy.Open[0] &&
                                Strategy.Close[0] < Strategy.Low[1];

                            if (bearishReversal)
                            {
                                DataCollector.ShortEntryTriggered++;

                                double entryPrice = currentPrice;
                                double initialStop = entryPrice + (EntryOptParams.StopATR * atrValue);

                                orderTicket = CreateOrderTicket(DAOrderType.Short, initialStop);
                            }
                        }
                    }
                }
            }

            if (orderTicket != null)
            {
                double riskPct = OptParamsVWAPMR.EquityRiskPct;
                orderTicket.AllowedRiskPercentOfAccount = riskPct;
            }
            return orderTicket;
        }


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

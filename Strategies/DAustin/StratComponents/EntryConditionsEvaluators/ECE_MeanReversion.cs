using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
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
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators
{
    [StrategyComponentId("ECE-MEANREVERSION")]
    public class ECE_MeanReversion : EntryConditionsEvaluatorBase
    {
        #region Properties
        private Indicators_MeanReversion IndicatorsMR { get { return Indicators as Indicators_MeanReversion; } }
        private OptimizationParameters_MeanReversion OptParamsMR { get { return OptParams as OptimizationParameters_MeanReversion; } }
        #endregion

        #region constructors
        public ECE_MeanReversion(StratBase strat)
        {
            Strategy = strat;
            OrderIdPrefix = "DAMR";
            Initialize();
        }
        #endregion

        #region PublicMethods
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade || Strategy.CurrentBars[1] < 1)
            {   // in preload phase
                return null;
            }

            if (!EntryTimeWindows.IsInDefinedTimeBlock())
            {
                return null;
            }

            // ==================== RANGE FILTER ====================
            if (IndicatorsMR.ADX[0] >= this.OptParamsMR.ADXThreshold)
            {
                return null;   // skip trending days
            }

            orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
            if (!MeanReversionTradeTriggered(orderTicket))
            {
                orderTicket = null;
            }

            return orderTicket;
        }

        public bool MeanReversionTradeTriggered(OrderTicket orderTicket)
        {
            DAOrderType newot = DAOrderType.None;

            // ==================== INDICATOR VALUES ====================
            double bbLower = IndicatorsMR.Bollinger.Lower[0];
            double bbUpper = IndicatorsMR.Bollinger.Upper[0];
            double bbMiddle = IndicatorsMR.Bollinger.Middle[0];
            double rsiVal = IndicatorsMR.RSI[0];
            double ema50Val = IndicatorsMR.EMA50[0];
            double daily200 = IndicatorsMR.EMA200Daily[0];

            if (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Long)
            {   // Check for long entry
                bool prevBelowLower = Strategy.Close[1] < IndicatorsMR.Bollinger.Lower[1];
                bool nowInsideLower = Strategy.Close[0] >= bbLower;
                bool rsiOversold = rsiVal <= OptParamsMR.RSIOversold;
                bool farBelowEMA = Strategy.Close[0] <= ema50Val * (1 - OptParamsMR.PercentFromEMA / 100.0);
                bool aboveDailyEMA = Strategy.Close[0] > daily200;

                if (    prevBelowLower &&
                        nowInsideLower &&
                        rsiOversold &&
                        farBelowEMA &&
                        aboveDailyEMA)
                {
                    newot = DAOrderType.Long;
                }
            }

            if (newot == DAOrderType.None && (orderTicket.Type == DAOrderType.None || orderTicket.Type == DAOrderType.Short))
            {   // Check for short entry
                bool prevAboveUpper = Strategy.Close[1] > IndicatorsMR.Bollinger.Upper[1];
                bool nowInsideUpper = Strategy.Close[0] <= bbUpper;
                bool rsiOverbought = rsiVal >= OptParamsMR.RSIOverbought;
                bool farAboveEMA = Strategy.Close[0] >= ema50Val * (1 + OptParamsMR.PercentFromEMA / 100.0);

                if (    prevAboveUpper &&
                        nowInsideUpper &&
                        rsiOverbought &&
                        farAboveEMA)
                {
                    newot = DAOrderType.Short;
                }
            }

            if (newot != DAOrderType.None)
            {
                if (orderTicket.Type == DAOrderType.None)
                {   // if current order type is None, we can set it to new order type
                    orderTicket.Type = newot;
                }
                else if (newot != orderTicket.Type)
                {   // if new order type is different than current order type
                    // we set the type to None
                    orderTicket.Type = DAOrderType.None;
                }
                orderTicket.Type = newot;
            }

            // now we determine stops and watnots based on order type
            if (orderTicket.Type != DAOrderType.None)
            {
                // we just really need to set the orderticket risk based on stop loss distance,
                // so we can calculate position size. The actual stop loss price will be set in
                // the orderTicket code.
                orderTicket.Risk = FlexibleValue.FromPoints(IndicatorsMR.ATR[0] * OptParamsMR.StopATRMultiplier, Strategy);
                double tpOffsetPoints = Math.Abs(Strategy.Close[0] - IndicatorsMR.NYSessionAnchoredVWAP.Value) * .85;
                orderTicket.TPOffset = FlexibleValue.FromPoints(tpOffsetPoints, Strategy);
                orderTicket.AllowedRiskPercentOfAccount = OptParamsMR.RiskAccountPercent;
                orderTicket.MaxTradeMinutes = OptParamsMR.MaxTradeMinutes;
            }
            return orderTicket.Type != DAOrderType.None;
        }


        public void Reset()
        {
            Initialize();
        }

        public void Initialize()
        {
        }
        #endregion

        #region VirtualMethods
        #endregion
    }
}

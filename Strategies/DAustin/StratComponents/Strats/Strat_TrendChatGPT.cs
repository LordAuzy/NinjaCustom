#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
using NinjaTrader.Custom.Strategies.DAustin.TradeManagers;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.AccountData;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies.DAustin.EntryConditionsEvaluators;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using NLog;
using NLog.Config;
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

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MNQ_Hybrid_ORB_Momentum : Strategy
    {
        private EMA emaFast;
        private EMA emaSlow;
        private ATR atr;
        private ADX adx;
        private DAVWAP vwap;

        private double orHigh = 0;
        private double orLow = 0;
        private bool orLocked = false;
        private DateTime sessionStart;

        private int pullbackBars = 0;

        #region Parameters

        [NinjaScriptProperty] public int ORMinutes { get; set; } = 30;
        [NinjaScriptProperty] public double MinORBreakATR { get; set; } = 0.3;
        [NinjaScriptProperty] public int PullbackMinBars { get; set; } = 2;
        [NinjaScriptProperty] public int PullbackMaxBars { get; set; } = 5;
        [NinjaScriptProperty] public double PullbackMaxATR { get; set; } = 1.5;
        [NinjaScriptProperty] public double RiskATR { get; set; } = 1.2;
        [NinjaScriptProperty] public double RewardR { get; set; } = 1.5;
        [NinjaScriptProperty] public double BETriggerR { get; set; } = 1.0;
        [NinjaScriptProperty] public double TrailATR { get; set; } = 1.5;
        [NinjaScriptProperty] public double MinADX { get; set; } = 20;
        [NinjaScriptProperty] public double MaxVWAPDistanceATR { get; set; } = 2.5;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MNQ_Hybrid_ORB_Momentum";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 2;
            }
            else if (State == State.DataLoaded)
            {
                emaFast = EMA(20);
                emaSlow = EMA(50);
                atr = ATR(14);
                adx = ADX(14);
                vwap = new DAVWAP(this, "9:30am", "Eastern Standard Time");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar > 1)
            {
                vwap.Update();
            }

            if (CurrentBar < 50) return;

            // Detect session start
            if (Bars.IsFirstBarOfSession)
            {
                orHigh = High[0];
                orLow = Low[0];
                orLocked = false;
                sessionStart = Time[0];
            }

            double minutesSinceOpen = (Time[0] - sessionStart).TotalMinutes;

            // Build OR
            if (!orLocked)
            {
                orHigh = Math.Max(orHigh, High[0]);
                orLow = Math.Min(orLow, Low[0]);

                if (minutesSinceOpen >= ORMinutes)
                    orLocked = true;

                return;
            }

            double currentATR = atr[0];

            // Bias
            bool bullishBias = Close[0] > orHigh && (Close[0] - orHigh) > MinORBreakATR * currentATR;
            bool bearishBias = Close[0] < orLow && (orLow - Close[0]) > MinORBreakATR * currentATR;

            // Filters
            if (adx[0] < MinADX) return;

            double vwapDist = Math.Abs(Close[0] - vwap.Value) / currentATR;
            if (vwapDist > MaxVWAPDistanceATR) return;

            // Pullback logic
            if (bullishBias)
            {
                if (Close[0] < emaFast[0]) pullbackBars++;
                else pullbackBars = 0;
            }

            if (bearishBias)
            {
                if (Close[0] > emaFast[0]) pullbackBars++;
                else pullbackBars = 0;
            }

            bool validPullback =
                pullbackBars >= PullbackMinBars &&
                pullbackBars <= PullbackMaxBars &&
                Math.Abs(Close[0] - emaFast[0]) / currentATR < PullbackMaxATR;

            // Liquidity sweep
            bool sweepLong = Low[0] < Low[1] && Close[0] > Low[1];
            bool sweepShort = High[0] > High[1] && Close[0] < High[1];

            double stopDist = currentATR * RiskATR;

            // LONG
            if (bullishBias && validPullback && sweepLong && Close[0] > High[1] && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterLong(2, "L");

                SetStopLoss("L", CalculationMode.Price, Close[0] - stopDist, false);
                SetProfitTarget("L", CalculationMode.Price, Close[0] + stopDist * RewardR);
            }

            // SHORT
            if (bearishBias && validPullback && sweepShort && Close[0] < Low[1] && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterShort(2, "S");

                SetStopLoss("S", CalculationMode.Price, Close[0] + stopDist, false);
                SetProfitTarget("S", CalculationMode.Price, Close[0] - stopDist * RewardR);
            }

            // Management
            if (Position.MarketPosition == MarketPosition.Long)
            {
                double r = (Close[0] - Position.AveragePrice) / stopDist;

                if (r >= BETriggerR)
                    SetStopLoss(CalculationMode.Price, Position.AveragePrice);

                SetStopLoss(CalculationMode.Price, Close[0] - atr[0] * TrailATR);
            }

            if (Position.MarketPosition == MarketPosition.Short)
            {
                double r = (Position.AveragePrice - Close[0]) / stopDist;

                if (r >= BETriggerR)
                    SetStopLoss(CalculationMode.Price, Position.AveragePrice);

                SetStopLoss(CalculationMode.Price, Close[0] + atr[0] * TrailATR);
            }
        }
    }
}

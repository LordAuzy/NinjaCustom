using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters
{
    [StrategyComponentId("OP-MEANREVERSION")]
    public class OptimizationParameters_MeanReversion : OptimizationParametersBase
    {
        #region Properties
        public int BBPeriod { get; set; }
        public double BBDev { get; set; }
        public int RSIPeriod { get; set; }
        public int RSIOversold { get; set; }
        public int RSIOverbought { get; set; } 
        public int EMAPeriod { get; set; }
        public double PercentFromEMA { get; set; }
        public int ADXPeriod { get; set; }
        public int ADXThreshold { get; set; }
        public double StopATRMultiplier { get; set; }
        public int ATRPeriod { get; set; }
        public int RiskAccountPercent { get; set; }
        public int MaxTradeMinutes { get; set; }

        #endregion

        #region constructors
        public OptimizationParameters_MeanReversion(StratBase strat) : base(strat)
        {

        }
        #endregion

        public override void UpdateFromStrat()
        {
            base.UpdateFromStrat();

            Strat_MeanReversion strat = Strategy as Strat_MeanReversion;

            BBPeriod = strat.BBPeriod;
            BBDev = strat.BBDev;
            RSIPeriod = strat.RSIPeriod;
            RSIOversold = strat.RSIOversold;
            RSIOverbought = strat.RSIOverbought;
            EMAPeriod = strat.EMAPeriod;
            PercentFromEMA = strat.PercentFromEMA;
            ADXPeriod = strat.ADXPeriod;
            ADXThreshold = strat.ADXThreshold;
            StopATRMultiplier = strat.StopATRMultiplier;
            ATRPeriod = strat.ATRPeriod;
            RiskAccountPercent = strat.RiskAccountPercent;
            MaxTradeMinutes = strat.MaxTradeMinutes;
        }

    }
}

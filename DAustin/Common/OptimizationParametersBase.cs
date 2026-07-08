using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class OptimizationParametersBase : IOptimizationParameters
    {
        #region ClassDefinitions
        public class GeneralParameters
        {
            public double EquityRiskPercent { get; set; }
            public StopLossTrailingMode SLTrailingMode { get; set; }
            public TimeWindowTimeZone TimeWindowTimeZone { get; set; }
            public string TWAnchorTime { get; set; }
            public int MaxTradesPerSession { get; set; }
            public LoggingMode LoggingMode { get; set; }
        }

        public class ChandelierGuardStopParameters
        {
            public int ATRPeriod { get; set; }
            public double InitialATRBuffer { get; set; }
            public double BE_Expanding_R { get; set; }
            public double BE_Contracting_R { get; set; }
            public double ChandelierATRMult { get; set; }
            public double TightATRMult { get; set; }
            public double TightenTriggerR { get; set; }
        }

        public class AdaptiveTrailingStopParameters
        {
            public int FastEMAPeriod { get; set; }
            public int SlowEMAPeriod { get; set; }
            public int ATRPeriod { get; set; }
            public double ATRSpreadMultiplier { get; set; }
        }

        public class TrendStructuralTrailingStopParameters
        {
            public int EMAPeriod { get; set; }
            public int ATRPeriod { get; set; }
            public double ATRMultiplier { get; set; }
            public double ActivationR { get; set; }
        }

        public class BreakEvenParameters
        {
            public double R { get; set; }
            public bool UseATR { get; set; }
            public int ATRPeriod { get; set; }
            public double Expanding_R { get; set; }
            public double Contracting_R { get; set; }
        }

        public class TimeParameters
        {
            public TimeWindowTimeZone TimeZone { get; set; }
            public string FlattenTOD { get; set; }
            public int MaxMinutesInTrade { get; set; }
            public string TWAnchorTime { get; set; }
            public int TWOffset1 { get; set; }
            public int TWDuration1 { get; set; }
            public int TWOffset2 { get; set; }
            public int TWDuration2 { get; set; }
        }

        public class  DOWTimeblock
        {
            public int Offset { get; set; } = 0;
            public int Duration { get; set; } = 0;
            public DADayOfWeek DayOfWeek { get; set; } = DADayOfWeek.None;
        }


        public class ScheduleBiasFilterParameters : DOWTimeblock
        {
            public TradingStance TradingStance { get; set; } = TradingStance.None;
            public DAMonth Month { get; set; } = DAMonth.None;
        }

        public class ScheduleSizingFilterParameters : DOWTimeblock
        {
            public double Multiplier { get; set; } = 1.0;
        }

        #endregion

        public StratBase Strategy { get; set; }

        #region constructors
        public OptimizationParametersBase(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        public virtual void UpdateFromStrat()
        {

        }

        public virtual void UpdateStratParamValues()
        {

        }

        public virtual void SetDefaultValues()
        {

        }

        public virtual void ToStringBuilder(StringBuilder sb)
        {

        }

        public virtual int GetMaxMinutesInTrade() { return 0; }
        public virtual double GetTrailingATRMultiplier() { return 0; }
        public virtual EarlyExitMode GetEarlyExitMode() { return EarlyExitMode.None; }

        public virtual ATRRegimeParameters GetRegimeParameters() { return null; }
        public virtual StopLossParameters GetStopLossParameters() { return null; }
        public virtual ChandelierGuardStopParameters GetChandelierGuardStopParameters() { return null; }
        public virtual BreakEvenParameters GetBreakEvenParameters() { return null; }
        public virtual TimeParameters GetTimeParameters() { return null; }
        public virtual AdaptiveTrailingStopParameters GetAdaptiveTrailingStopParameters() { return null; }
        public virtual TrendStructuralTrailingStopParameters GetTrendStructuralTrailingStopParameters() { return null; }
        public virtual GeneralParameters GetGeneralParameters() { return null; }
    }
}

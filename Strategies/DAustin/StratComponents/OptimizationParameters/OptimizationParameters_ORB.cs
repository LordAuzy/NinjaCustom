using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters
{
    [StrategyComponentId("OP-ORB")]
    public class OptimizationParameters_ORB : OptimizationParametersBase
    {
        #region Properties
        public ATRRegimeParameters ATRRegime { get; set; } = new ATRRegimeParameters();
        public StopLossParameters StopLoss { get; set; } = new StopLossParameters();
        public AdaptiveTrailingStopParameters AdaptiveTrailingStop { get; set; } = new AdaptiveTrailingStopParameters();
        public int OpeningRangeMinutes { get; set; }
        public double BreakoutBarVolumeMultiplier { get; set; }
        public int VolumeAverageBarCount { get; set; }
        public int ORMaxWidth { get; set; }
        public int ORMinWidth { get; set; }
        public int SessionTradingMinutes { get; set; }
        public EarlyExitMode EarlyExitMode { get; set; } = EarlyExitMode.None;
        public double TakeProfitRiskMultiple { get; set; }
        public bool EnableVWAP { get; set; }
        public bool EnableVWAPSlope { get; set; }
        public int MaxMinutesInTrade { get; set; }
        public bool PreMarketFiltering { get; set; }
        public int RiskAccountPercent { get; set; }
        public int RSIPeriod { get; set; }
        public int ATRPeriod { get; set; }
        public int FastEMAPeriod { get; set; }
        public int SlowEMAPeriod { get; set; }
        public bool BreakAndRetest { get; set; }
        public double ProfitRForDynamicExit { get; set; }
        #endregion

        #region constructors
        public OptimizationParameters_ORB(StratBase strat) : base(strat)
        {

        }
        #endregion

        #region overrides
        public override void SetDefaultValues()
        {
            base.SetDefaultValues();

            OpeningRangeMinutes = 21;
            BreakoutBarVolumeMultiplier = 1.2;
            VolumeAverageBarCount = 10;
            SessionTradingMinutes = 156;
            ORMaxWidth = 0;
            ORMinWidth = 50;
            EarlyExitMode = EarlyExitMode.None;
            TakeProfitRiskMultiple = 2.5;
            EnableVWAP = true;
            EnableVWAPSlope = true;
            MaxMinutesInTrade = 0;
            PreMarketFiltering = false;
            RiskAccountPercent = 3;
            RSIPeriod = 14;
            ATRPeriod = 14;
            FastEMAPeriod = 9;
            SlowEMAPeriod = 21;
            BreakAndRetest = false;
            ProfitRForDynamicExit = 1.4;
            // Groupname = Adaptive Trailing Stop
            AdaptiveTrailingStop.FastEMAPeriod = 9;
            AdaptiveTrailingStop.SlowEMAPeriod = 21;
            AdaptiveTrailingStop.ATRPeriod = 14;
            AdaptiveTrailingStop.ATRSpreadMultiplier = 2.5;
            // GroupName = ATR Regime Filter
            ATRRegime.EnableAtrRegimeFilter = false;
            ATRRegime.AtrRegimeFastPeriod = 8;      // recent volatility expansion
            ATRRegime.AtrRegimeSlowPeriod = 25;     // baseline regime (fits within opening range + some)
            ATRRegime.AtrRegimeAtrPeriod = 14;      // Standard ATR period
            ATRRegime.MinAtrRegimeRatio = 1.05;     // expansion (fast > slow)
            ATRRegime.MinAtrPercent = 0.0010;       // absolute floor (blocks dead markets)
            // GroupName = StopLoss Parameters
            StopLoss.SLTrailingMode = StopLossTrailingMode.TrailingAdaptive;
            StopLoss.SLInitialPlacement = StopLossInitialPlacement.Distance;
            StopLoss.InitialPlacement_ATRPeriod = 14;
            StopLoss.InitialPlacement_ATRMult = 1.5;
            StopLoss.MoonshotORD = 1.5;
            StopLoss.MidpointORD = .5;
            StopLoss.MidpointAvgRange = 140;
            StopLoss.TrailingATRPeriod = 20;
            StopLoss.TrailingATRMultiplier = 4.75;
        }

        public override void UpdateStratParamValues()
        {
            base.UpdateStratParamValues();

            Strat_ORB strat = Strategy as Strat_ORB;

            strat.OpeningRangeMinutes = OpeningRangeMinutes;
            strat.BreakoutBarVolumeMultiplier = BreakoutBarVolumeMultiplier;
            strat.VolumeAverageBarCount = VolumeAverageBarCount;
            strat.SessionTradingMinutes = SessionTradingMinutes;
            strat.ORMaxWidth = ORMaxWidth;
            strat.ORMinWidth = ORMinWidth;
            strat.EarlyExitMode = EarlyExitMode;
            strat.TakeProfitRiskMultiple = TakeProfitRiskMultiple;
            strat.EnableVWAP = EnableVWAP;
            strat.EnableVWAPSlope = EnableVWAPSlope;
            strat.MaxMinutesInTrade = MaxMinutesInTrade;
            strat.PreMarketFiltering = PreMarketFiltering;
            strat.RiskAccountPercent = RiskAccountPercent;
            strat.ATRPeriod = ATRPeriod;
            strat.RSIPeriod = RSIPeriod;
            strat.FastEMAPeriod = FastEMAPeriod;
            strat.SlowEMAPeriod = SlowEMAPeriod;
            strat.BreakAndRetest = BreakAndRetest;
            strat.ProfitRForDynamicExit = ProfitRForDynamicExit;
            // AdaptiveTrailingStop Parameters
            strat.ATS_ATRPeriod = AdaptiveTrailingStop.ATRPeriod;
            strat.ATS_FastEMAPeriod = AdaptiveTrailingStop.FastEMAPeriod;
            strat.ATS_SlowEMAPeriod = AdaptiveTrailingStop.SlowEMAPeriod;
            strat.ATS_ATRSpreadMultiplier = AdaptiveTrailingStop.ATRSpreadMultiplier;
            // ATR Regime Filter
            strat.EnableAtrRegimeFilter = ATRRegime.EnableAtrRegimeFilter;
            strat.AtrRegimeFastPeriod = ATRRegime.AtrRegimeFastPeriod;
            strat.AtrRegimeSlowPeriod = ATRRegime.AtrRegimeSlowPeriod;
            strat.AtrRegimeAtrPeriod = ATRRegime.AtrRegimeAtrPeriod;
            strat.MinAtrRegimeRatio = ATRRegime.MinAtrRegimeRatio;
            strat.MinAtrPercent = ATRRegime.MinAtrPercent;
            // StopLoss Parameters
            strat.SLTrailingMode = StopLoss.SLTrailingMode;
            strat.SLInitialPlacement = StopLoss.SLInitialPlacement;
            strat.StopLossIP_ATRPeriod = StopLoss.InitialPlacement_ATRPeriod;
            strat.StopLossIP_ATRMult = StopLoss.InitialPlacement_ATRMult;
            strat.MoonshotORD = StopLoss.MoonshotORD;
            strat.MidpointORD = StopLoss.MidpointORD;
            strat.MidpointAvgRange = StopLoss.MidpointAvgRange;
            strat.TrailingATRPeriod = StopLoss.TrailingATRPeriod;
            strat.TrailingATRMultiplier = StopLoss.TrailingATRMultiplier;
        }

        public override void UpdateFromStrat()
        {
            base.UpdateFromStrat();

            Strat_ORB strat = Strategy as Strat_ORB;

            OpeningRangeMinutes = strat.OpeningRangeMinutes;
            BreakoutBarVolumeMultiplier = strat.BreakoutBarVolumeMultiplier;
            VolumeAverageBarCount = strat.VolumeAverageBarCount;
            SessionTradingMinutes = strat.SessionTradingMinutes;
            ORMaxWidth = strat.ORMaxWidth;
            ORMinWidth = strat.ORMinWidth;
            EarlyExitMode = strat.EarlyExitMode;
            TakeProfitRiskMultiple = strat.TakeProfitRiskMultiple;
            EnableVWAP = strat.EnableVWAP;
            EnableVWAPSlope = strat.EnableVWAPSlope;
            MaxMinutesInTrade = strat.MaxMinutesInTrade;
            PreMarketFiltering = strat.PreMarketFiltering;
            RiskAccountPercent = strat.RiskAccountPercent;
            ATRPeriod = strat.ATRPeriod;
            RSIPeriod = strat.RSIPeriod;
            FastEMAPeriod = strat.FastEMAPeriod;
            SlowEMAPeriod = strat.SlowEMAPeriod;
            BreakAndRetest = strat.BreakAndRetest;
            ProfitRForDynamicExit = strat.ProfitRForDynamicExit;
            TakeProfitRiskMultiple = strat.TakeProfitRiskMultiple;
            // AdaptiveTrailingStop Parameters
            AdaptiveTrailingStop.ATRPeriod = strat.ATS_ATRPeriod;
            AdaptiveTrailingStop.FastEMAPeriod = strat.ATS_FastEMAPeriod;
            AdaptiveTrailingStop.SlowEMAPeriod = strat.ATS_SlowEMAPeriod;
            AdaptiveTrailingStop.ATRSpreadMultiplier = strat.ATS_ATRSpreadMultiplier;
            // ATR Regime Filter
            ATRRegime.EnableAtrRegimeFilter = strat.EnableAtrRegimeFilter;
            ATRRegime.AtrRegimeFastPeriod = strat.AtrRegimeFastPeriod;
            ATRRegime.AtrRegimeSlowPeriod = strat.AtrRegimeSlowPeriod;
            ATRRegime.AtrRegimeAtrPeriod = strat.AtrRegimeAtrPeriod;
            ATRRegime.MinAtrRegimeRatio = strat.MinAtrRegimeRatio;
            ATRRegime.MinAtrPercent = strat.MinAtrPercent;
            // StopLoss Parameters
            StopLoss.SLTrailingMode = strat.SLTrailingMode;
            StopLoss.SLInitialPlacement = strat.SLInitialPlacement;
            StopLoss.InitialPlacement_ATRPeriod = strat.StopLossIP_ATRPeriod;
            StopLoss.InitialPlacement_ATRMult = strat.StopLossIP_ATRMult;
            StopLoss.MoonshotORD = strat.MoonshotORD;
            StopLoss.MidpointORD = strat.MidpointORD;
            StopLoss.MidpointAvgRange = strat.MidpointAvgRange;
            StopLoss.TrailingATRMultiplier = strat.TrailingATRMultiplier;
        }

        public override int GetMaxMinutesInTrade()
        {
            return MaxMinutesInTrade;
        }

        public override double GetTrailingATRMultiplier()
        { 
            return StopLoss.TrailingATRMultiplier; 
        }

        public override EarlyExitMode GetEarlyExitMode() 
        { 
            return EarlyExitMode;
        }

        public override ATRRegimeParameters GetRegimeParameters() 
        { 
            ATRRegimeParameters regimeParameters = new ATRRegimeParameters(ATRRegime);
            return regimeParameters; 
        }

        public override StopLossParameters GetStopLossParameters()
        {
            StopLossParameters stopLossParameters = new StopLossParameters(StopLoss);
            return stopLossParameters;
        }

        public override AdaptiveTrailingStopParameters GetAdaptiveTrailingStopParameters() { return AdaptiveTrailingStop; }

        public override void ToStringBuilder(StringBuilder sb)
        {
            sb.AppendLine("======OptimizationParameters=Start=====");
            sb.AppendFormat("OpeningRangeMinutes={0}", OpeningRangeMinutes).AppendLine();
            sb.AppendFormat("BreakoutBarVolumeMultiplier={0}", BreakoutBarVolumeMultiplier).AppendLine();
            sb.AppendFormat("VolumeAverageBarCount={0}", VolumeAverageBarCount).AppendLine();
            sb.AppendFormat("ORMaxWidth={0}", ORMaxWidth).AppendLine();
            sb.AppendFormat("ORMinWidth={0}", ORMinWidth).AppendLine();
            sb.AppendFormat("SessionTradingMinutes={0}", SessionTradingMinutes).AppendLine();
            sb.AppendFormat("StopLossTrailingMode={0}", StopLoss.SLTrailingMode).AppendLine();
            sb.AppendFormat("TrailingATRPeriod={0}", StopLoss.TrailingATRPeriod).AppendLine();
            sb.AppendFormat("TrailingATRMultiplier={0}", StopLoss.TrailingATRMultiplier).AppendLine();
            sb.AppendFormat("TakeProfitRiskMultiple={0}", TakeProfitRiskMultiple).AppendLine();
            sb.AppendFormat("EnableVWAP={0}", EnableVWAP).AppendLine();
            sb.AppendFormat("EnableVWAPSlope={0}", EnableVWAPSlope).AppendLine();
            sb.AppendFormat("MaxMinutesInTrade={0}", MaxMinutesInTrade).AppendLine();
            sb.AppendFormat("PreMarketFiltering={0}", PreMarketFiltering).AppendLine();
            sb.AppendFormat("RiskAccountPercent={0}", RiskAccountPercent).AppendLine();
            sb.AppendFormat("RSIPeriod={0}", RSIPeriod).AppendLine();
            sb.AppendFormat("ATRPeriod={0}", ATRPeriod).AppendLine();
            sb.AppendFormat("FastEMAPeriod={0}", FastEMAPeriod).AppendLine();
            sb.AppendFormat("SlowEMAPeriod={0}", SlowEMAPeriod).AppendLine();
            sb.AppendFormat("BreakAndRetest={0}", BreakAndRetest).AppendLine();
            sb.AppendLine("======OptimizationParameters=End=======");
        }
        #endregion
    }
}

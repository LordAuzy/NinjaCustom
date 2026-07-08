using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class IndicatorsBase : IIndicators
    {
        #region ClassDefinitions
        public class BreakEvenIndicators
        {
            public ATR ATR { get; set; }
        }

        public class TrendStructuralTrailingIndicators
        {
            public ATR ATR { get; set; }
            public EMA EMA { get; set; }
        }

        public class ChandelierGuardIndicators
        {
            public ATR ATR { get; set; }
        }
        public class AdaptiveTrailingStopIndicators
        {
            public EMA FastEMA { get; set; }
            public EMA SlowEMA { get; set; }
            public ATR ATR { get; set; }
        }

        #endregion

        #region Properties
        public DAVWAP NYSessionAnchoredVWAP { get; private set; }
        public StratBase Strategy { get; set; }
        public IOptimizationParameters OptParams { get; set; }
        public OptimizationParametersBase OptParamsBase { get { return OptParams as OptimizationParametersBase; } }
        private TimeWindows _entryTimeWindows = null;
        public virtual TimeWindows EntryTimeWindows 
        { 
            get
            {
                if (_entryTimeWindows == null)
                {
                    _entryTimeWindows = InitializeEntryTimeWindows();
                }
                return _entryTimeWindows;   
            }    
            set { _entryTimeWindows = value; }
        }

        #endregion

        #region constructors
        public IndicatorsBase(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public virtual void Initialize()
        {
            NYSessionAnchoredVWAP = new DAVWAP(Strategy, "9:30am", "Eastern Standard Time");
        }

        public virtual void Update()
        {
            NYSessionAnchoredVWAP.Update();
        }

        public virtual void ToStringBuilder(StringBuilder sb)
        {

        }

        public virtual TimeWindows InitializeEntryTimeWindows()
        {
            TimeWindows tw = null;
            TimeParameters twp = OptParamsBase.GetTimeParameters();

            if (twp.TimeZone != TimeWindowTimeZone.None)
            {
                string anchorTime = twp.TWAnchorTime;
                string timeZoneId = twp.TimeZone.GetDisplayName();

                tw = new TimeWindows(Strategy, anchorTime, timeZoneId);

                if (twp.TWDuration1 > 0 && twp.TWOffset1 >= 0)
                {
                    tw.AddTimeBlock(
                        anchorOffsetStart: new TimeSpan(0, twp.TWOffset1, 0),
                        anchorOffsetEnd: new TimeSpan(0, twp.TWOffset1 + twp.TWDuration1, 0));
                }

                if (twp.TWDuration2 > 0 && twp.TWOffset2 >= 0)
                {
                    tw.AddTimeBlock(
                        anchorOffsetStart: new TimeSpan(0, twp.TWOffset2, 0),
                        anchorOffsetEnd: new TimeSpan(0, twp.TWOffset2 + twp.TWDuration2, 0));
                }
            }
            return tw;
        }
        #endregion

        #region VirtualMethods
        public virtual EMA GetFastEMA { get { return null; } }
        public virtual EMA GetSlowEMA { get { return null; } }
        public virtual ATR GetTrailingATR() { return null; }
        public virtual StopLossIndicators GetStopLossIndicators() { return null; }
        public virtual ChandelierGuardIndicators GetChandelierGuardIndicators() { return null; }
        public virtual AdaptiveTrailingStopIndicators GetAdaptiveTrailingStopIndicators() { return null; }
        public virtual TrendStructuralTrailingIndicators GetTrendStructuralTrailingIndicators() { return null; }
        public virtual BreakEvenIndicators GetBreakEvenIndicators() { return null; }
        #endregion
    }
}

using Newtonsoft.Json.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class SessionInfo
    {
        #region Properties
        public StratBase Strategy { get; set; }
        public DateTime StartTime { get; private set; } = DateTime.MinValue;
        public DateTime EndTime { get; private set; } = DateTime.MinValue;
        public DateTime FlattenTime { get; private set; } = DateTime.MinValue;
        public TimeSpan FlattenOnSessionCloseTimeSpan { get; private set; } = TimeSpan.FromMinutes(5);
        private int _flattenOnSessionCloseMinutes = 5;
        public int FlattenOnSessionCloseMinutes 
        {   
            get 
            {  
                return _flattenOnSessionCloseMinutes;
            }
            set 
            { 
                _flattenOnSessionCloseMinutes = value; 
                FlattenOnSessionCloseTimeSpan = TimeSpan.FromMinutes(value);
                UpdateFlattenTime();
            }
        }
        #endregion

        #region Constructors
        public SessionInfo(StratBase strategy)
        {
            Strategy = strategy;
        }
        #endregion

        #region Public Methods
        public void UpdateSession(DateTime time)
        {
            SessionIterator si = Strategy.SessionIterator;

            si.CalculateTradingDay(time, true);
            // for futures the sessionStart is the previous day
            // but a latter time than the sessionEnd;
            StartTime = si.ActualSessionBegin;
            EndTime = si.ActualSessionEnd;
            UpdateFlattenTime();
            Strategy.Logs.Debug(time, "SessionStart={0}  SessionEnd={1}  Flatten={2}", StartTime, EndTime, FlattenTime);
        }
        #endregion

        #region Private Methods
        private void UpdateFlattenTime()
        {
            if (EndTime > DateTime.MinValue) 
            {
                FlattenTime = EndTime - FlattenOnSessionCloseTimeSpan;
            }
        }
        #endregion
    }
}

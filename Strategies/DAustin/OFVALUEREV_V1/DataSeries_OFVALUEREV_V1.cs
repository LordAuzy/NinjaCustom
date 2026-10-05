using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OFVALUEREV_V1
{
    public class DataSeries_OFVALUEREV_V1
    {
        public enum Name
        {
            Primary,
            Daily,
            Tick,
            Minute,
            RthTick,
            RthMinute,
            RthPrimary
        }

        private int NextDataSeriesIndex = 0;
        public Dictionary<Name, int> Indices { get; } = new Dictionary<Name, int>();

        #region Constructors
        public DataSeries_OFVALUEREV_V1()
        {

        }
        #endregion

        #region publicMethods
        public void AddSeriesIndex(Name name)
        {
            Indices.Add(name, NextDataSeriesIndex);
            NextDataSeriesIndex++;
        }

        public int GetSeriesIndex(Name name)
        {
            return Indices[name];
        }
        #endregion
    }
}

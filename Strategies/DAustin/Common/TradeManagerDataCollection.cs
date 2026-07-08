using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.Common
{
    public class TradeManagerDataCollection
    {
        #region Properties
        public int FlattenCountShort { get; set; } = 0;
        public int FlattenCountLong { get; set; } = 0;
        public int MaxTimeInTradeCountShort { get; set; } = 0;
        public int MaxTimeInTradeCountLong { get; set; } = 0;
        #endregion

        #region Constructors
        public TradeManagerDataCollection() 
        { 
        
        }
        #endregion
    }
}

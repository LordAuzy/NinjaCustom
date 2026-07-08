using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class ATRRegimeParameters
    {
        #region Properties
        public bool EnableAtrRegimeFilter { get; set; }
        public int AtrRegimeFastPeriod { get; set; }
        public int AtrRegimeSlowPeriod { get; set; }
        public int AtrRegimeAtrPeriod { get; set; }
        public double MinAtrRegimeRatio { get; set; }
        public double MinAtrPercent { get; set; }
        #endregion

        #region Constructors
        public ATRRegimeParameters()
        {
        }

        public ATRRegimeParameters(ATRRegimeParameters source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            EnableAtrRegimeFilter = source.EnableAtrRegimeFilter;
            AtrRegimeFastPeriod = source.AtrRegimeFastPeriod;
            AtrRegimeSlowPeriod = source.AtrRegimeSlowPeriod;
            AtrRegimeAtrPeriod = source.AtrRegimeAtrPeriod;
            MinAtrRegimeRatio = source.MinAtrRegimeRatio;
            MinAtrPercent = source.MinAtrPercent;
        }
        #endregion

        public bool AreValid()
        {
            bool areValid = AtrRegimeAtrPeriod > 0 && AtrRegimeFastPeriod > 0 && AtrRegimeSlowPeriod > 0;

            return areValid;
        }
    }
}

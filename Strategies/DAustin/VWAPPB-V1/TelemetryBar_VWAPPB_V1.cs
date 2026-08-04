using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.VWAPPB_V1
{
    public class TelemetryBar_VWAPPB_V1 : TelemetryBarBase
    {
        #region static
        private static List<string> s_columnNames { get; } =
        [
            "VWAP",
            "VWAPSlope5",
            "DistanceFromVWAP",
            "EMAFast",
            "EMASlow",
            //"EMAFastSlope",
            //"EMASlowSlope",
            "EMASpread",
            "EMASpreadSlope5",
            "ATR",
            "ADX",
            "ADXSlope",
            "DIMinus",
            "DIPlus"
        #endregion
        ];


        #region Properties
        public double ATR { get; set; } = 0;
        public double VWAP { get; set; } = 0;
        public double VWAPSlope { get; set; } = 0;
        public double DistanceFromVWAP { get; set; } = 0;
        public double EMAFast { get; set; } = 0;
        public double EMASlow { get; set; } = 0;
        public double EMAFastSlope { get; set; } = 0;
        public double EMASlowSlope { get; set; } = 0;
        public double EMASpread { get; set; } = 0;
        public double EMASpreadSlope { get; set; } = 0;
        public double ADX { get; set; } = 0;
        public double ADXSlope { get; set; } = 0;
        public double DIMinus { get; set; } = 0;
        public double DIPlus { get; set; } = 0;
        public Indicators_VWAPPB_V1 IndicatorsVWAPPB_V1 { get { return Indicators as Indicators_VWAPPB_V1; } }
        #endregion

        #region Constructors
        public TelemetryBar_VWAPPB_V1() : base() 
        { 
        
        }

        public TelemetryBar_VWAPPB_V1(StratBase strat, IIndicators indicators) : base(strat, indicators)
        {

        }
        #endregion

        #region overrides
        public override List<string> GetColumnNames()
        {
            List<string> columnNames = base.GetColumnNames();
            columnNames.AddRange(s_columnNames);
            return columnNames;
        }

        public override List<string> GetRowData()
        {
            string doubleStringFormatter = "F2";
            List<string> rowData = base.GetRowData();

            rowData.Add(VWAP.ToString(doubleStringFormatter));
            rowData.Add(VWAPSlope.ToString(doubleStringFormatter));
            rowData.Add(DistanceFromVWAP.ToString(doubleStringFormatter));
            rowData.Add(EMAFast.ToString(doubleStringFormatter));
            rowData.Add(EMASlow.ToString(doubleStringFormatter));
            //rowData.Add(EMAFastSlope.ToString());
            //rowData.Add(EMASlowSlope.ToString());
            rowData.Add(EMASpread.ToString(doubleStringFormatter));
            rowData.Add(EMASpreadSlope.ToString(doubleStringFormatter));
            rowData.Add(ATR.ToString(doubleStringFormatter));
            rowData.Add(ADX.ToString(doubleStringFormatter));
            rowData.Add(ADXSlope.ToString(doubleStringFormatter));
            rowData.Add(DIMinus.ToString(doubleStringFormatter));
            rowData.Add(DIPlus.ToString(doubleStringFormatter));
            return rowData;
        }

        public override void CollectData()
        {
            Indicators_VWAPPB_V1 indicators = IndicatorsVWAPPB_V1;
            EMA fastEMA = indicators.Entry.FastEMA;
            EMA slowEMA = indicators.Entry.SlowEMA;
            ATR atr = indicators.Entry.ATR;
            DAVWAPIndicator AnchoredVWAP = indicators.Entry.AnchoredVWAP;

            base.CollectData();
            ATR = atr[0];
            VWAP = AnchoredVWAP[0];
            VWAPSlope = (AnchoredVWAP[0] - AnchoredVWAP[5]) / 5.0;
            DistanceFromVWAP = Strategy.Close[0] - AnchoredVWAP[0];
            EMAFast = fastEMA[0];
            EMASlow = slowEMA[0];
            EMAFastSlope = (fastEMA[0] - fastEMA[5]) / 5.0;
            EMASlowSlope = (slowEMA[0] - slowEMA[5]) / 5.0;
            EMASpread = EMAFast - EMASlow;
            EMASpreadSlope = ((fastEMA[0] - slowEMA[0]) - (fastEMA[5] - slowEMA[5])) / 5.0;
            ADX = indicators.Entry.DM[0];
            ADXSlope = (indicators.Entry.DM[0] - indicators.Entry.DM[5]) / 5.0;
            DIMinus = indicators.Entry.DM.DiMinus[0];
            DIPlus = indicators.Entry.DM.DiPlus[0];
        }
        #endregion
    }
}

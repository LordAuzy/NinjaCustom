using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class TelemetryBar_OPNDRV : TelemetryBarBase
    {
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
        public Indicators_OPNDRV IndicatorsOPNDRV { get { return Indicators as Indicators_OPNDRV; } }
        #endregion

        #region Constructors
        public TelemetryBar_OPNDRV() : base() 
        { 
        
        }

        public TelemetryBar_OPNDRV(StratBase strat, IIndicators indicators) : base(strat, indicators)
        {

        }
        #endregion

        #region overrides
        public override List<string> GetColumnNames()
        {
            List<string> columnNames = base.GetColumnNames();
            columnNames.AddRange(_columnNames);
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
            Indicators_OPNDRV indicators = IndicatorsOPNDRV;
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

        #region ICSVDataSource implementation
        private static readonly List<string> _columnNames = new List<string>
        {
            "VWAP",
            "VWAPSlope5",
            "DistanceFromVWAP",
            "EMAFast",
            "EMASlow",
            "EMASpread",
            "EMASpreadSlope5",
            "ATR",
            "ADX",
            "ADXSlope",
            "DIMinus",
            "DIPlus"
        };

        // we need to return a cloned list of column names
        // before the object has been instantiated, so we
        // can't always use the instance method GetColumnNames()
        public static List<string> ColumnNameList(List<string> columns)
        {
            // first get for base class
            List<string> columnNames = TelemetryBarBase.ColumnNameList(columns);

            // then this derived class
            columnNames.AddRange(_columnNames);

            return columnNames;
        }

        public int dataRowIndexOPNDRV = 0;

        public override void Rewind()
        {
            base.Rewind();
            dataRowIndexOPNDRV = 0;
        }

        public override List<string> NextDataRow(List<string> data)
        {
            List<string> dataRow = null;

            dataRowIndexOPNDRV++;
            // this data source only has one row of data,
            // so return null after the first row is returned
            if (dataRowIndexOPNDRV == 1)
            {
                dataRow = base.NextDataRow(data);

                if (dataRow != null)
                {
                    string doubleStringFormatter = "F2";

                    dataRow.Add(VWAP.ToString(doubleStringFormatter));
                    dataRow.Add(VWAPSlope.ToString(doubleStringFormatter));
                    dataRow.Add(DistanceFromVWAP.ToString(doubleStringFormatter));
                    dataRow.Add(EMAFast.ToString(doubleStringFormatter));
                    dataRow.Add(EMASlow.ToString(doubleStringFormatter));
                    dataRow.Add(EMASpread.ToString(doubleStringFormatter));
                    dataRow.Add(EMASpreadSlope.ToString(doubleStringFormatter));
                    dataRow.Add(ATR.ToString(doubleStringFormatter));
                    dataRow.Add(ADX.ToString(doubleStringFormatter));
                    dataRow.Add(ADXSlope.ToString(doubleStringFormatter));
                    dataRow.Add(DIMinus.ToString(doubleStringFormatter));
                    dataRow.Add(DIPlus.ToString(doubleStringFormatter));
                }
                else
                {
                    Strategy.Logs.Warn("base class did not return a row.");
                }
            }
            return dataRow;
        }

        public override List<string> GetColumnNames(List<string> columns)
        {
            // get the base names first
            List<string> columnNames = base.GetColumnNames(columns);

            // then add the names for this class
            columnNames.AddRange(_columnNames);

            return columnNames;
        }
        #endregion

    }
}

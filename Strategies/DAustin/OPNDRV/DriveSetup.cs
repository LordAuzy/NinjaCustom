using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class DriveSetup : ICSVDataSource
    {
        public MarketPosition Direction { get; set; }
        public DriveState Drive { get; set; }
        public int DriveCompletedBar { get; set; }
        public double VWAP { get; set; }
        public double VWAPSlopeATR { get; set; }
        public double VWAPDistanceATR { get; set; }
        public double EMASpreadATR { get; set; }
        public double CloseLocation { get; set; }

        public DriveSetup()
        {
        }

        public DriveSetup Clone()
        {
            return new DriveSetup
            {
                Direction = Direction,
                Drive = Drive?.Clone(),
                DriveCompletedBar = DriveCompletedBar,
                VWAP = VWAP,
                VWAPSlopeATR = VWAPSlopeATR,
                VWAPDistanceATR = VWAPDistanceATR,
                EMASpreadATR = EMASpreadATR,
                CloseLocation = CloseLocation
            };
        }

        #region ICSVDataSource Implementation
        int dataRowIndex = 0;
        private static readonly List<string> _columnNames = new List<string>
        {
            "DriveRangeATR",
            "DriveNetMoveATR",
            "DriveEfficiency",
            "DriveCloseLocation",
            "DriveVWAPDistanceATR",
            "DriveVWAPSlopeATR",
            "DriveEMASpreadATR"
        };

        public List<string> GetColumnNames(List<string> columns)
        {
            List<string> columnNames = columns;

            if (columnNames == null)
            {   // if list wasn't passed in, create a new list to return
                columnNames = new List<string>();
            }

            columnNames.AddRange(_columnNames);
            return columnNames;
        }

        public List<string> NextDataRow(List<string> data)
        {
            List<string> dataRow = null;

            dataRowIndex++;
            // this data source only has one row of data,
            // so return null after the first row is returned
            if (dataRowIndex == 1)
            {
                dataRow = data;

                if (dataRow == null)
                {   // if list wasn't passed in, create a new list to return
                    dataRow = new List<string>();
                }

                dataRow.Add(Drive.RangeATR.ToString("F2"));
                dataRow.Add(Drive.NetMoveAtr.ToString("F2"));
                dataRow.Add(Drive.Efficiency.ToString("F2"));
                dataRow.Add(CloseLocation.ToString("F2"));
                dataRow.Add(VWAPDistanceATR.ToString("F2"));
                dataRow.Add(VWAPSlopeATR.ToString("F2"));
                dataRow.Add(EMASpreadATR.ToString("F2"));
            }
            return dataRow;
        }

        public void Rewind()
        {
            dataRowIndex = 0;
        }
        #endregion
    }
}

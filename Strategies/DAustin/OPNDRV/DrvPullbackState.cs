using NinjaTrader.Cbi;
using NinjaTrader.Custom.DAustin.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    public class DrvPullbackState : ICSVDataSource
    {
        #region Properties

        // Frozen copy of the opening-drive setup that this pullback belongs to.
        public DriveSetup DriveSetup { get; set; }

        public double PullbackHigh { get; private set; } = double.MinValue;
        public double PullbackLow { get; private set; } = double.MaxValue;

        // Number of pullback bars processed.
        public int PullbackBars { get; private set; } = 0;

        // Maximum amount price has penetrated VWAP during the pullback.
        // Long:  penetration = VWAP - Low
        // Short: penetration = High - VWAP
        // Stored as a positive point value.
        public double MaxVWAPPenetration { get; private set; } = 0.0;

        // Maximum normalized bar range observed during the pullback.
        public double MaxPullbackBarRangeATR { get; private set; } = 0.0;

        // Entry-time snapshot values.
        public double EntryATR { get; private set; } = 0.0;
        public double EntryDistanceVWAPATR { get; private set; } = 0.0;

        public double MaxVWAPPenetrationATR
        {
            get
            {
                if (EntryATR <= 0)
                    return 0.0;

                return MaxVWAPPenetration / EntryATR;
            }
        }

        public double RetracementPct
        {
            get
            {
                if (DriveSetup == null ||
                    DriveSetup.Drive == null ||
                    DriveSetup.Drive.Range <= 0)
                {
                    return 0.0;
                }

                if (DriveSetup.Direction == MarketPosition.Long)
                {
                    if (PullbackLow == double.MaxValue)
                        return 0.0;

                    return (DriveSetup.Drive.High - PullbackLow) /
                           DriveSetup.Drive.Range;
                }

                if (DriveSetup.Direction == MarketPosition.Short)
                {
                    if (PullbackHigh == double.MinValue)
                        return 0.0;

                    return (PullbackHigh - DriveSetup.Drive.Low) /
                           DriveSetup.Drive.Range;
                }

                return 0.0;
            }
        }

        #endregion

        #region Constructors

        public DrvPullbackState()
        {
        }

        public DrvPullbackState(DriveSetup drv)
        {
            // Pullback state owns its own frozen copy of the drive setup.
            DriveSetup = drv?.Clone();
        }

        #endregion

        #region Public Methods

        public void IncrementBars()
        {
            PullbackBars++;
        }

        public void UpdateHigh(double high)
        {
            PullbackHigh = Math.Max(PullbackHigh, high);
        }

        public void UpdateLow(double low)
        {
            PullbackLow = Math.Min(PullbackLow, low);
        }

        public void UpdateVWAPPenetration(double penetration)
        {
            if (penetration <= 0)
                return;

            MaxVWAPPenetration =
                Math.Max(MaxVWAPPenetration, penetration);
        }

        public void UpdateBarRange(double barRange, double atr)
        {
            if (barRange < 0 || atr <= 0)
                return;

            double barRangeATR = barRange / atr;

            MaxPullbackBarRangeATR =
                Math.Max(MaxPullbackBarRangeATR, barRangeATR);
        }

        public void SetEntrySnapshot(
            double atr,
            double entryPrice,
            double vwap)
        {
            EntryATR = atr;

            if (atr <= 0)
            {
                EntryDistanceVWAPATR = 0.0;
                return;
            }

            EntryDistanceVWAPATR =
                Math.Abs(entryPrice - vwap) / atr;
        }

        public DrvPullbackState Clone()
        {
            return new DrvPullbackState
            {
                DriveSetup = DriveSetup?.Clone(),
                PullbackHigh = PullbackHigh,
                PullbackLow = PullbackLow,
                PullbackBars = PullbackBars,
                MaxVWAPPenetration = MaxVWAPPenetration,
                MaxPullbackBarRangeATR = MaxPullbackBarRangeATR,
                EntryATR = EntryATR,
                EntryDistanceVWAPATR = EntryDistanceVWAPATR
            };
        }

        public void Reset()
        {
            PullbackHigh = double.MinValue;
            PullbackLow = double.MaxValue;
            PullbackBars = 0;
            MaxVWAPPenetration = 0.0;
            MaxPullbackBarRangeATR = 0.0;
            EntryATR = 0.0;
            EntryDistanceVWAPATR = 0.0;
            Rewind();
        }

        #endregion

        #region ICSVDataSource
        private int dataRowIndex = 0;
        private static readonly List<string> _columnNames = new List<string>
        {
            "PullbackBars",
            "RetracementPct",
            "MaxVWAPPenetrationATR",
            "PullbackMaxBarRangeATR",
            "EntryDistanceVWAPATR"
        };

        public List<string> GetColumnNames(List<string> columns)
        {
            List<string> columnNames = DriveSetup.GetColumnNames(columns);
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
                dataRow = DriveSetup.NextDataRow(data);

                if (dataRow != null)
                {
                    dataRow.Add(PullbackBars.ToString(CultureInfo.InvariantCulture));
                    dataRow.Add(Format(RetracementPct));
                    dataRow.Add(Format(MaxVWAPPenetrationATR));
                    dataRow.Add(Format(MaxPullbackBarRangeATR));
                    dataRow.Add(Format(EntryDistanceVWAPATR));
                }
            }
            return dataRow;
        }

        public void Rewind()
        {
            DriveSetup.Rewind();
            dataRowIndex = 0;
        }
        #endregion

        #region Private Methods

        private static string Format(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        #endregion
    }
}

using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    // This class will let me set it's value as a dollar amount, ticks, or points.
    // I can also retrieve the value as a dollar amount, ticks, or points.
    // This will be useful for setting stop losses and profit targets.
    // the class holds it's value internally as ticks, but the public interface allows for
    // flexible setting and retrieval in any of the three units.
    //
    // Relationship between units:
    //   1 point  = 1 / TickSize ticks        (e.g. MNQ: TickSize=0.25 → 1 point = 4 ticks)
    //   1 tick   = TickSize points            (e.g. MNQ: 1 tick = 0.25 points)
    //   1 tick   = DollarsPerTick dollars     (e.g. MNQ: 1 tick = $0.50)
    //   1 point  = PointValue dollars         (e.g. MNQ: 1 point = $2.00)
    public class FlexibleValue
    {
        #region Fields
        private double _ticks = 0;
        #endregion

        #region Properties
        // Dollars per tick (e.g. MNQ = $0.50/tick, NQ = $5.00/tick)
        public double DollarsPerTick { get; private set; }

        // The size of one tick expressed in points (e.g. MNQ = 0.25 points/tick)
        public double TickSize { get; private set; }

        // The dollar value of one full point move (e.g. MNQ = $2.00/point)
        public double DollarsPerPoint { get; private set; }

        // The offset expressed as a number of ticks
        public double Ticks
        {
            get { return _ticks; }
            set { _ticks = value; }
        }

        // The offset expressed as a dollar amount
        public double Dollars
        {
            get { return _ticks * DollarsPerTick; }
            set { _ticks = (DollarsPerTick > 0) ? value / DollarsPerTick : 0; }
        }

        // The offset expressed as points (price units on the chart)
        public double Points
        {
            get { return (TickSize > 0) ? _ticks * TickSize : 0; }
            set { _ticks = (TickSize > 0) ? value / TickSize : 0; }
        }
        #endregion

        #region Constructors
        // Reads TickSize and PointValue directly from the instrument.
        // e.g. MNQ: TickSize=0.25, PointValue=$2.00 → DollarsPerTick=$0.50
        public FlexibleValue(Strategy strat)
        {
            // size of one tick in points (e.g. MNQ = 0.25 points/tick)
            TickSize = strat.Instrument.MasterInstrument.TickSize;
            // currency value of 1 point of movement
            DollarsPerPoint = strat.Instrument.MasterInstrument.PointValue;
            // currency value of 1 tick of movement
            DollarsPerTick = TickSize * DollarsPerPoint;

            if (TickSize <= 0 || DollarsPerPoint <= 0 || DollarsPerTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(strat), "Instrument TickSize/PointValue must be > 0.");
        }
        #endregion

        #region PublicMethods
        // Set the offset from a dollar amount
        public void SetFromDollars(double dollars)
        {
            Dollars = dollars;
        }

        // Set the offset from a tick count
        public void SetFromTicks(double ticks)
        {
            Ticks = ticks;
        }

        // Set the offset from a point value (price units on the chart)
        public void SetFromPoints(double points)
        {
            Points = points;
        }

        // Returns the dollar value of the offset
        public double ToDollars()
        {
            return Dollars;
        }

        // Returns the tick count of the offset as a double
        public double ToTicks()
        {
            return Ticks;
        }

        // Returns the tick count as an int — use this when calling
        // SetStopLoss() / SetProfitTarget() with CalculationMode.Ticks
        public int ToTicksInt()
        {
            if (double.IsNaN(_ticks) || double.IsInfinity(_ticks))
                throw new InvalidOperationException("Ticks is not a finite value.");

            var rounded = Math.Round(_ticks);
            if (rounded > int.MaxValue || rounded < int.MinValue)
                throw new OverflowException("Ticks is outside the range of Int32.");

            return Convert.ToInt32(rounded);
        }

        // Returns the offset as points (price units on the chart)
        public double ToPoints()
        {
            return Points;
        }

        public override string ToString()
        {
            return $"{Points:F4} points | {Ticks} ticks | ${Dollars:F2}";
        }
        #endregion

        #region StaticFactoryMethods
        // Create a FlexibleValue from a dollar amount
        public static FlexibleValue FromDollars(double dollars, Strategy strat)
        {
            FlexibleValue po = new FlexibleValue(strat);
            po.SetFromDollars(dollars);
            return po;
        }

        // Create a FlexibleValue from a tick count
        public static FlexibleValue FromTicks(double ticks, Strategy strat)
        {
            FlexibleValue po = new FlexibleValue(strat);
            po.SetFromTicks(ticks);
            return po;
        }

        // Create a FlexibleValue from a point value
        public static FlexibleValue FromPoints(double points, Strategy strat)
        {
            FlexibleValue po = new FlexibleValue(strat);
            po.SetFromPoints(points);
            return po;
        }
        #endregion
    }
}

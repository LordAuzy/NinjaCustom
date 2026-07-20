using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NinjaTrader.Custom.DAustin.Common
{
    public enum TradeEventType
    {
        Exec,
        Order,
        Unknown
    }

    public enum LoggingMode
    {
        Production,
        Debug,
        Trace
    }

    public enum DAOrderType
    {
        None = 0,
        Short = 1,
        Long = 2,
        LongStopMarket = 3,
        ShortStopMarket = 4
    }

    public enum EntryOrderType
    {
        Market = 0,
        StopMarket = 1
    }

    public enum EntryTriggerType
    {
        None = 0,
        ChatGPT = 1,
        Claude = 2,
        Grok = 3,
        Gemini = 4
    }

    public enum TradingStance
    {
        All,
        LongOnly,
        ShortOnly,
        None
    }

    public enum TimeWindowTimeZone
    {
        [Display(Name = "None")]
        None,

        [Display(Name = "Eastern Standard Time")]
        Eastern,

        [Display(Name = "Central Standard Time")]
        Central,

        [Display(Name = "Mountain Standard Time")]
        Mountain,

        [Display(Name = "Pacific Standard Time")]
        Pacific,
    }

    //
    //  Idle: The strategy is monitoring market conditions to decide if/when
    //  to enter a trade.No open positions or pending orders
    //
    //  EntryPending: An entry order has been submitted (e.g., EnterLong() or
    //  EnterShort()), but it's not yet filled. This handles partial fills
    //  or market delays.
    //
    //  InPosition: The position is open and filled. Initial trade management
    //  begins.
    //
    //  BreakEvenPending: Waiting to adjust the stop loss to break-even
    //  (e.g., move stop to entry price + commission). This is a transitional
    //  state to confirm the adjustment.
    //
    //  TrailingStop: The trade is profitable, and you're actively trailing
    //  the stop loss (e.g., based on ATR, parabolic SAR, or fixed steps).
    //
    //  ExitPending: An exit order has been submitted (e.g., ExitLong(), ExitShort()),
    //  but not yet filled. Handles market gaps or delays.
    //
    //  Exited: The trade is fully closed. Wrap up logging, reset variables,
    //  and prepare for the next cycle.
    //
    public enum TradeState
    {
        Idle,               // Monitoring for entry signals
        FillPending,        // Order submitted, waiting for fill
        InPosition,         // Position open, initial management
        BreakEvenPending,           // Waiting to move stop to break-even
        BreakEvenPending2Stage,   // Waiting to move stop to break-even in 2 stages
        StopMovePending,    // Waiting for stop modification to be acknowledged
        TrailingStop,       // Actively trailing the stop loss
        TrailingStopATRRatchet, // Trailing stop based on ATR ratchet
        TrailingStopChandelierGuard,        // Trailing stop based on Chandelier Guard
        TrailingStopAdaptive,   // Adaptive trailing stop with multi-stage profit locking
        TrailingStopVWAPMeanReversion,   // Trailing stop based on VWAP mean reversion logic
        TrailingStopTrendStructural,   // Trailing stop based on trend structure (e.g., higher highs/lower lows)
        ExitPending,        // Exit order submitted, waiting for fill
        Exited              // Trade closed, reset for next opportunity
    }

    public enum StrategyTypes
    {
        None,
        ORB,
        MeanReversion,
        Momentum921Cross,
        MomentumPriceTouch
    }

    public enum StopLossTrailingMode
    {
        Fixed,
        BreakEven,
        BreakEvenStaged,
        BreakEvenThenTrail,
        Trailing,
        TrailingATRRatchet,
        TrailingAdaptive,
        TrailingChandelierGuard,
        VWAPMeanReversion,
        TrendStructuralTrailing
    }

    public enum StopLossInitialPlacement
    {
        Distance,
        ATR,
        Hybrid
    }

    public enum EarlyExitMode
    {
        None,
        NoFollowThrough,
        MidpointFailure,
        RangeRotation,
        ATRFailure,
        Combined
    }

    public enum DADayOfWeek
    {
        None,
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    public enum DAMonth
    {
        None,
        January,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

    public enum VwapBandMode
    {
        Cumulative,
        Rolling
    }
}

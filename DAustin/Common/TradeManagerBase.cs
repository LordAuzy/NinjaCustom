using ActiproSoftware.Windows;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Data;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using NinjaTrader.NinjaScript.SuperDomColumns;
using SharpDX.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using NLog;
using NLog.Config;
using IB = NinjaTrader.Custom.DAustin.Common.IndicatorsBase;
using OB = NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.DAustin.Common.Reporting;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
namespace NinjaTrader.Custom.DAustin.Common
{
    public class TradeManagerBase : ITradeManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Logger _loggerTP = null;
        private bool _fullyInitialized = false;
        private Logger LoggerTP
        {
            get
            {
                if (_loggerTP == null || _fullyInitialized == false)
                {
                    (_loggerTP, _fullyInitialized) = Strategy.CreateLoggerWithBaseProps(logger);
                }
                return _loggerTP;
            }
        }

        public static NLog.Logger _tradeLogger = LogManager.GetLogger("TradeExecutionLogger");
        private Logger _tradeLoggerTP = null;
        private bool _tradeLoggerfullyInitialized = false;
        private Logger TradeLoggerTP
        {
            get
            {
                if (_tradeLoggerTP == null || _tradeLoggerfullyInitialized == false)
                {
                    (_tradeLoggerTP, _tradeLoggerfullyInitialized) = Strategy.CreateLoggerWithBaseProps(_tradeLogger);
                }
                return _tradeLoggerTP;
            }
        }

        #region Properties
        public StratBase Strategy { get; private set; }
        public IndicatorsBase Indicators { get; set; }
        public OptimizationParametersBase OptParams { get; set; }
        public List<TradeContext> TradeContexts { get; private set; } = new List<TradeContext>();
        public TimeSpan FlattenTOD { get; set; } = TimeSpan.Zero;
        public TradeManagerDataCollection TradeData { get; private set; } = new TradeManagerDataCollection();
        public ATRRegimeFilter ATRRegimeFilter { get; private set; }
        #endregion

        #region Constructors
        public TradeManagerBase(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public void OnDataLoaded()
        {
            ATRRegimeParameters rp = OptParams?.GetRegimeParameters();
            if (rp?.AreValid() == true && rp?.EnableAtrRegimeFilter == true)
            {
                ATRRegimeFilter = new ATRRegimeFilter(Strategy, rp);
            }
        }

        public double RoundToNearestValidTick(double rawPrice)
        {
            double tickSize = Strategy.TickSize;

            return Math.Round(rawPrice / tickSize) * tickSize;
        }

        public void AddTradeContext(TradeContext tc)
        {
            TradeContexts.Add(tc);
        }
        // ninjatrader documentation says I should set the SL and TP before the entry order.
        // the mode should be ticks. In that case the SL and TP are set relative to the 
        // fill price. That's what we want.
        public bool SubmitOrder(OrderTicket ot)
        {
            bool SLTPPlaced = false;
            bool entryPlaced = false;

            if (ot != null && ot.Type != DAOrderType.None)
            {
                TradeContext tc = new TradeContext();
                TradeContexts.Add(tc);
                tc.SetState(TradeState.Idle);
                tc.OrderTicket = ot;

                SLTPPlaced = ot.PlaceStopsAndTargets(tc);
                entryPlaced = ot.PlaceEntry(tc) != null;
            }
            return entryPlaced || SLTPPlaced;
        }

        public DateTime GetFlattenTODForDisplay()
        {
            //TODO: convert to data timezone;
            DateTime flattenDT = DateTime.Today.Add(FlattenTOD);
            return flattenDT;
        }

        private bool IsInFlattenTimeWindow(DateTime time)
        {
            bool IsInFlattenWindow = false;
            SessionIterator si = Strategy.SessionIterator;

            if (si.IsInSession(Strategy.Time[0], true, true))
            {   // we need to be in a session to possibly be in the
                // flatten timewindow
                // Calculate the current trading day
                si.GetNextSession(time, true);
                // Get the Actual Session Times
                DateTime sessionBegin = si.ActualSessionBegin;
                DateTime sessionEnd = si.ActualSessionEnd;
                DateTime flattenWindowStart = DateTime.MinValue;

                if (FlattenTOD == TimeSpan.Zero)
                {   // if this is zero the flatten window start defaults to 5 min before the session ends
                    flattenWindowStart = sessionEnd.AddMinutes(-5);
                }
                else
                {   // flatten window starts at the FlattenTOD and goes until the end of the session
                    flattenWindowStart = sessionEnd.Date + FlattenTOD;
                }
                IsInFlattenWindow = time >= flattenWindowStart && time <= sessionEnd;
            }
            return IsInFlattenWindow;
        }

        private void EnforceMaxTradeMinutes()
        {
            int MaxMinutesInTrade = OptParams.GetMaxMinutesInTrade();
            if (MaxMinutesInTrade > 0)
            {
                foreach (TradeContext tc in TradeContexts)
                {
                    if (    tc.State != TradeState.FillPending &&
                            tc.State != TradeState.ExitPending &&
                            tc.State != TradeState.Exited)
                    {
                        if (tc.EntryOrder != null)
                        {
                            TimeSpan timeInTrade = Strategy.Time[0] - tc.EntryOrder.Time;

                            if (timeInTrade.TotalMinutes > MaxMinutesInTrade)
                            {
                                tc.SetState(TradeState.ExitPending);
                                if (tc.OrderTicket.Type == DAOrderType.Short)
                                {
                                    Strategy.ExitShort("MaxMinutesInTradeExitShort", tc.EntryOrder.Name);
                                }
                                else if (tc.OrderTicket.Type == DAOrderType.Long)
                                {
                                    Strategy.ExitLong("MaxMinutesInTradeExitLong", tc.EntryOrder.Name);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FlattenStrategyPositions()
        {
            // if we are in the flatten time window then we want to cancel any pending orders
            if (Strategy.Position.MarketPosition != MarketPosition.Flat)
            {
                LoggerTP.Info("Flattening positions due to flatten time window.");
                foreach (TradeContext tc in TradeContexts)
                {
                    if (tc.EntryOrder != null)
                    {
                        if (tc.EntryOrder.IsLong)
                        {
                            Strategy.ExitLong("FlattenStrategyPositionsLong", tc.EntryOrder.Name);
                        }
                        else if (tc.EntryOrder.IsShort)
                        {
                            Strategy.ExitShort("FlattenStrategyPositionsShort", tc.EntryOrder.Name);
                        }
                    }
                }
            }
        }

        public virtual void OnBarUpdate()
        {
            LoggerTP.Trace(">"); ;
            if (Indicators != null)
            {
                Indicators.Update();
            }

            if (Strategy?.Bars?.IsFirstBarOfSession == true)
            {
                foreach (TradeContext tc in TradeContexts)
                {
                    tc.SessionReset();
                }
            }

            // Update highest/lowest tracking for MAE/MFE on ALL active trades
            foreach (TradeContext tc in TradeContexts)
            {
                if (tc.EntryOrder != null && tc.EntryOrder.OrderState == OrderState.Filled &&
                    tc.State != TradeState.Idle && tc.State != TradeState.FillPending && tc.State != TradeState.Exited)
                {
                    // Update for all trades in position
                    tc.HighestHighSinceEntry = Math.Max(tc.HighestHighSinceEntry, Strategy.High[0]);
                    tc.LowestLowSinceEntry = Math.Min(tc.LowestLowSinceEntry, Strategy.Low[0]);
                }
            }

            if (IsInFlattenTimeWindow(Strategy.Time[0]))
            {   // all trades get exited
                if (LoggerTP.IsTraceEnabled)
                {
                    // We log this at trace level because it can be helpful to see when we are in the flatten window
                    // and if we are correctly flattening all positions, but it is very verbose and would likely be
                    // too much information to include in debug logs.
                    //
                    //                    var simTime = Strategy.GetDataTimeForLogger();
                    //                    var log = logger.WithProperty("SimTime", simTime);

                    //                    log.Trace("Current time is within flatten window. Checking if there are any positions to flatten.");
                }
                FlattenStrategyPositions();
            }
            else
            {   // check for timeInTrade > MaxTradeMinutes
                EnforceMaxTradeMinutes();
            }

            if (Strategy.SessionIterator.IsInSession(Strategy.Time[0], true, true) && !IsInFlattenTimeWindow(Strategy.Time[0]))
            {
                foreach (TradeContext tc in TradeContexts)
                {
                    TradeState stateBefore = tc.State;
                    DispatchState(tc);

                    // only re-dispatch immediately for states where acting on the
                    // same bar matters (e.g. entry fill confirmed → start managing)
                    if (tc.State != stateBefore && 
                        (stateBefore == TradeState.FillPending || stateBefore == TradeState.StopMovePending))
                    {
                        DispatchState(tc);
                    }
                }

                // if any tradeContext transitioned to Exited then we want to immediately dispatch
                // those so they get reset and go back to their initial state.
                foreach (TradeContext tc in TradeContexts)
                {
                    if (tc.State == TradeState.Exited)
                    {
                        DispatchState(tc);
                    }
                }
            }
            LoggerTP.Trace("<"); ;
        }

        private void DispatchState(TradeContext tc)
        {
            switch (tc.State)
            {
                case TradeState.Idle: HandleEntrySignals(tc); break;
                case TradeState.FillPending: HandleFillPending(tc); break;
                case TradeState.InPosition:    HandleInPosition(tc); break;
                case TradeState.BreakEvenPending: HandleBreakEvenPending(tc); break;
                case TradeState.StopMovePending: HandleStopMovePending(tc); break;
                case TradeState.TrailingStopATRRatchet: HandleRatchetedATRTrailingStop(tc); break;
                case TradeState.TrailingStopChandelierGuard: HandleChandelierGuardTrailingStop(tc); break;
                case TradeState.TrailingStopTrendStructural: HandleTrendStructuralTrailingStop(tc); break;
                case TradeState.TrailingStop: HandleTrailingStop(tc); break;
                case TradeState.TrailingStopAdaptive: HandleAdaptiveTrailingStop(tc); break;
                case TradeState.TrailingStopVWAPMeanReversion: HandleMeanReversionExitPending(tc); break;
                case TradeState.ExitPending: HandleExitPending(tc); break;
                case TradeState.Exited: HandleExited(tc); break;
            }
        }

        public void HandleStopMovePending(TradeContext tc)
        {
            if (tc.OrderTicket == null)
            {
                tc.SetState(TradeState.Idle);
                return;
            }

            var next = tc.PendingNextState ?? TradeState.TrailingStop;

            // Wait until we have a working stop order reference again.
            // TradeContext.UpdateStop intentionally nulls StopOrder so we don't act on stale prices.
            if (tc.StopOrder != null && tc.StopOrder.OrderState == OrderState.Working)
            {
                // If we know the target price, confirm we're effectively at it (or within 1 tick).
                if (tc.PendingStopPrice > 0)
                {
                    double tick = Strategy.TickSize;
                    if (tick > 0 && Math.Abs(tc.StopOrder.StopPrice - tc.PendingStopPrice) > tick)
                        return;
                }

                tc.PendingNextState = null;
                tc.PendingStopPrice = 0;
                tc.PendingStopSubmittedTime = DateTime.MinValue;
                tc.SetState(next);
            }
        }

        public void HandleInPosition(TradeContext tc)
        {
            // no active management here. So just waiting for the trade to close
            // and advance to the next state in the OnExecutionUpdate.
        }

        public void HandleExitPending(TradeContext tc)
        {
            // we can monitor the exit order here if we want to do any partial exit management
            // or just wait for the OnExecutionUpdate to hit and then handle the full exit in there
        }

        public void HandleExited(TradeContext tc)
        {
            // we can do any logging or cleanup here before we reset the trade context and go back to looking for entry signals
            tc.Reset();
        }

        public void HandleEntrySignals(TradeContext tc)
        {
            if (Strategy.TradingLiveAccount() && Strategy.State != State.Realtime)
            {   // if we are trading a live account and we aren't in realtime yet,
                // then we don't want to evaluate entry signals - only trade realtime bars.
                // This is a safety check to prevent live orders from being submitted based
                // on historical bars during strategy initialization.
                return; 
            }

            if (tc.EntryConditionsEvaluator != null && Strategy.Position.MarketPosition == MarketPosition.Flat)
            {   // we can only check conditions for a trade if we are not in a trade already
                if (!IsEntryAllowedByAtrRegime())
                    return;

                tc.OrderTicket = tc.EntryConditionsEvaluator.Evaluate(tc);
                if (tc.OrderTicket != null)
                {
                    tc.AdvanceToNextState();
                    tc.OrderTicket.PlaceStopsAndTargets(tc);
                    Cbi.Order order = tc.OrderTicket.PlaceEntry(tc);

                    if (order == null)
                    {
                        tc.SetState(TradeState.Exited);
                    }
                }
            }
        }

        private bool IsEntryAllowedByAtrRegime()
        {
            bool allowed = true;

            if (ATRRegimeFilter != null)
            {
                allowed = ATRRegimeFilter.Passed(Strategy.Close[0]);
            }
            return allowed;
        }

        public void HandleFillPending(TradeContext tc)
        {
            OrderTicket ot = tc.OrderTicket;
            if (ot != null)
            {
                if (tc.EntrySet && tc.EntryOrder != null && tc.EntryOrder.Name == ot.SignalName)
                { 
                    if (tc.EntryOrder.OrderState == OrderState.Cancelled)
                    {
                        if (LoggerTP.IsDebugEnabled)
                        {
                            LoggerTP.Debug("EntryOrder Cancelled. SignalName");
                        }
                        tc.SetState(TradeState.Exited);
                    }
                    else if (tc.EntryOrder.OrderState == OrderState.Filled)
                    {
                        if (tc.SLSet == false || tc.StopOrder != null && tc.StopOrder.FromEntrySignal == ot.SignalName &&
                                tc.StopOrder.OrderState == OrderState.Working)
                        {
                            if (tc.TPSet == false || tc.LimitOrder != null && tc.LimitOrder.FromEntrySignal == ot.SignalName &&
                                    tc.LimitOrder.OrderState == OrderState.Working)
                            {
                                if (LoggerTP.IsDebugEnabled)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    if (tc.EntryOrder != null)
                                    {
                                        sb.AppendFormat("AverageFill={0}  ", tc.EntryOrder.AverageFillPrice);
                                    }
                                    if (tc.StopOrder != null)
                                    {
                                        sb.AppendFormat("StopPrice={0}  ", tc.StopOrder.StopPrice);
                                    }
                                    if (tc.LimitOrder != null)
                                    {
                                        sb.AppendFormat("Limit={0}  ", tc.LimitOrder.LimitPrice);
                                    }
                                    sb.AppendLine();

                                    LoggerTP.Debug(sb.ToString());
                                }

                                // initialize TradeContext variables when order filled
                                tc.HighestHighSinceEntry = tc.EntryOrder.AverageFillPrice;
                                tc.LowestLowSinceEntry = tc.EntryOrder.AverageFillPrice;
                                tc.StopMovedToBreakEven = false;
                                tc.AdvanceToNextState();
                            }
                        }
                    }
                    else if (tc.EntryOrder.OrderState == OrderState.Working)
                    {   // of we are in a working state and we have an expiration
                        // cancel if it is time.
                        if (tc.OrderTicket.StopExpiryBars > 0)
                        {
                            int startBarIndex = tc.OrderTicket.BarIndexEntered;
                            int barsBeforeExpire = tc.OrderTicket.StopExpiryBars;
                            int currentbarIndex = Strategy.CurrentBars[0];

                            if (currentbarIndex >= startBarIndex + barsBeforeExpire)
                            {
                                Strategy.CancelOrder(tc.EntryOrder);
                                tc.OrderTicket.StopExpiryBars = 0;
                            }
                        }
                    }
                }
            }
        }

        public void HandleBreakEvenPending(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null || tc.EntryDateTime == DateTime.MinValue)
            {
                return;
            }

            OB.BreakEvenParameters BEOptParams = OptParams.GetBreakEvenParameters();
            IB.BreakEvenIndicators BEIndicators = Indicators.GetBreakEvenIndicators();

            if (BEOptParams != null && BEIndicators != null)
            {
                double currentPrice = Strategy.Close[0];
                double entryPrice = tc.EntryOrder.AverageFillPrice;
                double initialRisk = tc.OrderTicket.Risk.Points;
                double rMultiple = (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket)
                    ? (currentPrice - entryPrice) / initialRisk
                    : (entryPrice - currentPrice) / initialRisk;

                // --- ATR regime ---
                ATR atr = BEIndicators.ATR;
                bool atrExpanding = atr[0] > atr[1];
                double triggerR;

                if (BEOptParams.UseATR)
                {
                    triggerR = atrExpanding ? BEOptParams.Expanding_R : BEOptParams.Contracting_R;
                }
                else
                {
                    triggerR = BEOptParams.R;
                }

                // --- Move to Break Even ---
                if (rMultiple >= triggerR)
                {
                    double newStopPrice = entryPrice;
                    int bufferMultiple = 2;
                    double buffer = Strategy.TickSize;

                    if (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket)
                        newStopPrice += (bufferMultiple * buffer);
                    else
                        newStopPrice -= (bufferMultiple * buffer);

                    tc.StopMovedToBreakEven = true;
                    tc.BeginStopMovePending(newStopPrice, tc.GetNextState());
                }
            }
        }

        private void MoveStopToBreakEven(TradeContext tc)
        {
            //Note: we always need to makesure our new price is on a valid tick boundary

            int breakEvenTicksBuffer = 4;
            double newStopPrice = 0;
            string fromEntrySignal = tc.OrderTicket.SignalName;

            if (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket)
            {
                newStopPrice = tc.EntryOrder.AverageFillPrice + (breakEvenTicksBuffer * Strategy.TickSize);
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short || tc.OrderTicket.Type == DAOrderType.ShortStopMarket)
            {
                newStopPrice = tc.EntryOrder.AverageFillPrice - (breakEvenTicksBuffer * Strategy.TickSize);
            }

            if (newStopPrice > 0)
            {
                tc.StopMovedToBreakEven = true;
                tc.BeginStopMovePending(newStopPrice, tc.GetNextState());
            }
        }

        public void HandleTrendStructuralTrailingStop(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
                return;

            OB.TrendStructuralTrailingStopParameters TSTOptParams = OptParams.GetTrendStructuralTrailingStopParameters();
            IB.TrendStructuralTrailingIndicators TSTIndicators = Indicators.GetTrendStructuralTrailingIndicators();
            ATR atr = TSTIndicators.ATR;
            EMA emaTrail = TSTIndicators.EMA;

            double currentPrice = Strategy.Close[0];
            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double initialRisk = tc.OrderTicket.Risk.Points;

            if (initialRisk <= 0)
                return;

            double currentR = 0;

            bool isLong = tc.OrderTicket.IsLong;
            bool isShort = tc.OrderTicket.IsShort;

            if (isLong)
            {
                currentR = (currentPrice - entryPrice) / initialRisk;
            }
            else if (isShort)
            {
                currentR = (entryPrice - currentPrice) / initialRisk;
            }

            // ============================================
            // DO NOTHING until activation threshold reached
            // ============================================

            if (currentR < TSTOptParams.ActivationR)
                return;

            double newStopPrice = 0;

            // ============================================
            // LONG TRAIL
            // ============================================

            if (isLong)
            {
                newStopPrice = emaTrail[0] - (atr[0] * TSTOptParams.ATRMultiplier);
                // never loosen stop
                if (newStopPrice <= tc.StopOrder.StopPrice)
                    return;
            }

            // ============================================
            // SHORT TRAIL
            // ============================================

            if (isShort)
            {
                newStopPrice = emaTrail[0] + (atr[0] * TSTOptParams.ATRMultiplier);
                // never loosen stop
                if (newStopPrice >= tc.StopOrder.StopPrice)
                    return;
            }

            // ============================================
            // Submit stop move
            // ============================================
            tc.BeginStopMovePending(newStopPrice, TradeState.TrailingStopTrendStructural);
        }

        public void HandleChandelierGuardTrailingStop(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
                return;

            IB.ChandelierGuardIndicators CGIndicators = Indicators.GetChandelierGuardIndicators();
            OB.ChandelierGuardStopParameters CGOptParams = OptParams.GetChandelierGuardStopParameters();
            ATR atr = CGIndicators.ATR;
            bool atrExpanding = atr[0] > atr[1];
            double newStopPrice = 0;
            double currentPrice = Strategy.Close[0];
            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double initialRisk = tc.OrderTicket.Risk.Points;
            double rMultiple = (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket) ? 
                        (currentPrice - entryPrice) / initialRisk
                        : (entryPrice - currentPrice) / initialRisk;

            if (!tc.StopMovedToBreakEven)
            {   // if we haven't moved to BE yet, then we check to see if it's time
                double rTrigger = atrExpanding ? CGOptParams.BE_Expanding_R : CGOptParams.BE_Contracting_R;

                if (rMultiple >= rTrigger)
                {
                    newStopPrice = entryPrice;
                    tc.StopMovedToBreakEven = true;
                }
            }
            else
            {   // --- Phase 3: Chandelier Trail --- already moved to be
                double atrMult = rMultiple >= CGOptParams.TightenTriggerR ? CGOptParams.TightATRMult : CGOptParams.ChandelierATRMult;

                if (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket)
                {
                    newStopPrice = tc.HighestHighSinceEntry - (atrMult * atr[0]);
                    newStopPrice = Math.Max(newStopPrice, entryPrice);
                    if (newStopPrice <= tc.StopOrder.StopPrice)
                    {   // we only move the stop if it's above the current stop price (for longs)
                        newStopPrice = 0;
                    }
                }
                else if (tc.OrderTicket.Type == DAOrderType.Short || tc.OrderTicket.Type == DAOrderType.ShortStopMarket)
                {
                    newStopPrice = tc.LowestLowSinceEntry + (atrMult * atr[0]);
                    newStopPrice = Math.Min(newStopPrice, entryPrice);
                    if (newStopPrice >= tc.StopOrder.StopPrice)
                    {   // we only move the stop if it's below the current stop price (for shorts)
                        newStopPrice = 0;
                    }
                }
            }

            if (newStopPrice > 0)
            {   // if we have a valid new stop price, then we submit the stop move
                tc.BeginStopMovePending(newStopPrice, TradeState.TrailingStopChandelierGuard);
            }
        }

        /// Ratcheted ATR-based trailing stop (Chandelier Exit variant).
        /// Uses the highest/lowest price since entry and an ATR-based offset to compute a stop that
        /// only ratchets in the direction of profit (never loosens).
        /// Conceptually:
        /// <item><description>Long: <c>HighestHighSinceEntry - ATR * Multiplier</c></description></item>
        /// <item><description>Short: <c>LowestLowSinceEntry + ATR * Multiplier</c></description></item>
        public void HandleRatchetedATRTrailingStop(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
                return;

            if (TryEarlyExit(tc))
                return;

            double multiplier = OptParams.GetTrailingATRMultiplier();
            ATR atr = Indicators.GetTrailingATR();
            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double currentPrice = Strategy.Close[0];
            double rPoints = tc.OrderTicket.Risk.Points;
            double tick = Strategy.TickSize;

            if (tc.OrderTicket.Type == DAOrderType.Long)
            {
                // 1. Break-even floor (NEW: prevents -1R losses)
                double profitR = (currentPrice - entryPrice) / rPoints;
                double minStopPrice = tc.StopOrder.StopPrice;
                
                if (profitR >= 1.0)
                {
                    minStopPrice = Math.Max(minStopPrice, entryPrice + (2 * tick));
                }

                // 2. Standard Chandelier logic
                if (tc.TrailStopPrice == 0)
                {
                    tc.TrailStopPrice = tc.HighestHighSinceEntry - (multiplier * atr[0]);
                    tc.TrailStopPrice = Math.Max(tc.TrailStopPrice, minStopPrice); // Respect break-even floor
                    tc.BeginStopMovePending(tc.TrailStopPrice, TradeState.TrailingStopATRRatchet);
                }
                else
                {
                    double candidateStop = tc.HighestHighSinceEntry - (multiplier * atr[0]);
                    
                    // 3. Optional: Tighten at high R (NEW: locks in big wins)
                    if (profitR >= 3.0)
                    {
                        candidateStop = Math.Max(candidateStop, entryPrice + (2.0 * rPoints));
                    }
                    
                    candidateStop = Math.Max(candidateStop, minStopPrice); // Always respect break-even
                    
                    if (candidateStop > tc.TrailStopPrice)
                    {
                        double roundedCandidateStop = RoundToNearestValidTick(candidateStop);
                        if (roundedCandidateStop > tc.TrailStopPrice)
                        {
                            tc.TrailStopPrice = roundedCandidateStop;
                            tc.BeginStopMovePending(tc.TrailStopPrice, TradeState.TrailingStopATRRatchet);
                        }
                    }
                }
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short)
            {
                // Same pattern for shorts...
                double profitR = (entryPrice - currentPrice) / rPoints;
                double maxStopPrice = tc.StopOrder.StopPrice;
                
                if (profitR >= 1.0)
                {
                    maxStopPrice = Math.Min(maxStopPrice, entryPrice - (2 * tick));
                }

                if (tc.TrailStopPrice == 0)
                {
                    tc.TrailStopPrice = tc.LowestLowSinceEntry + (multiplier * atr[0]);
                    tc.TrailStopPrice = Math.Min(tc.TrailStopPrice, maxStopPrice);
                    tc.BeginStopMovePending(tc.TrailStopPrice, TradeState.TrailingStopATRRatchet);
                }
                else
                {
                    double candidateStop = tc.LowestLowSinceEntry + (multiplier * atr[0]);
                    
                    if (profitR >= 3.0)
                    {
                        candidateStop = Math.Min(candidateStop, entryPrice - (2.0 * rPoints));
                    }
                    
                    candidateStop = Math.Min(candidateStop, maxStopPrice);
                    
                    if (candidateStop < tc.TrailStopPrice)
                    {
                        double roundedCandidateStop = RoundToNearestValidTick(candidateStop);
                        if (roundedCandidateStop < tc.TrailStopPrice)
                        {
                            tc.TrailStopPrice = roundedCandidateStop;
                            tc.BeginStopMovePending(tc.TrailStopPrice, TradeState.TrailingStopATRRatchet);
                        }
                    }
                }
            }
        }

        public void HandleMeanReversionExitPending(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
            {
                return;
            }

            double vwap = Indicators.NYSessionAnchoredVWAP.Value;
            double currentPrice = Strategy.Close[0];

            // =========================
            // 🎯 TARGET: VWAP
            // =========================
            if (tc.OrderTicket.Type == DAOrderType.Long)
            {
                if (currentPrice >= vwap)
                {
                    Strategy.ExitLong("VWAPMR_TP", tc.EntryOrder.Name);
                    tc.SetState(TradeState.ExitPending);
                }
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short)
            {
                if (currentPrice <= vwap)
                {
                    Strategy.ExitShort("VWAPMR_TP", tc.EntryOrder.Name);
                    tc.SetState(TradeState.ExitPending);
                }
            }
        }

        public void HandleTrailingStop(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
            {
                return;
            }

            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double currentPrice = Strategy.Close[0];
            double fastEMA = Indicators.GetFastEMA[0];
            double rPoints = tc.OrderTicket.Risk.Points;
            double tick = Strategy.TickSize;

            // Hold current stop default
            double newStopPrice = tc.StopOrder.StopPrice;

            // Safety check against zero division
            if (rPoints <= 0) return;

            if (tc.OrderTicket.Type == DAOrderType.Long)
            {
                double profitR = (currentPrice - entryPrice) / rPoints; // use Close[0] internally to avoid intra-bar whip-sawing stops

                // 1. Core Trail Logic - Trail behind the 21 EMA once we have deep profit
                if (profitR >= 2.0)
                {
                    // Past 2R: Keep the stop tucked safely beneath the slower 21 EMA to let big runners breathe
                    newStopPrice = Math.Max(newStopPrice, Indicators.GetFastEMA[0] - (4 * tick));
                }

                // 2. Break-Even Protection - Do this EARLY so we stop taking $960 losses
                if (profitR >= 0.8)
                {
                    // As soon as we get nearly 1R in profit, move stop to entry + 2 ticks so we never lose
                    newStopPrice = Math.Max(newStopPrice, entryPrice + (tick * 2));
                }

                // Apply Stop Order Modifications
                if (newStopPrice > tc.StopOrder.StopPrice)
                {
                    newStopPrice = RoundToNearestValidTick(newStopPrice);
                    Strategy.SetStopLoss(tc.EntryOrder.Name, CalculationMode.Price, newStopPrice, false);
                }

                // 3. Dynamic Trade Exit (The "Bailout") 
                // If we are at least decently profitable (Break-Even reached) and the trend officially
                // breaks the 9 EMA, get out.
                if (profitR > 0.8 && currentPrice < fastEMA && Strategy.Close[1] < Indicators.GetFastEMA[1])
                {
                    LoggerTP.Info($"[{tc.OrderTicket.SignalName}] Trail exit: 2 consecutive closes below 9 EMA.");
                    Strategy.ExitLong(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-TSExit", tc.OrderTicket.SignalName);
                    tc.SetState(TradeState.ExitPending);
                }
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short)
            {
                double profitR = (entryPrice - currentPrice) / rPoints;

                // 1. Core Trail Logic
                if (profitR >= 2.0)
                {
                    newStopPrice = Math.Min(newStopPrice, Indicators.GetSlowEMA[0] + (4 * tick));
                }

                // 2. Break-Even Protection 
                if (profitR >= 0.8)
                {
                    newStopPrice = Math.Min(newStopPrice, entryPrice - (tick * 2));
                }

                if (newStopPrice < tc.StopOrder.StopPrice)
                {
                    newStopPrice = RoundToNearestValidTick(newStopPrice);
                    Strategy.SetStopLoss(tc.EntryOrder.Name, CalculationMode.Price, newStopPrice, false);
                }

                // 3. Dynamic Trade Exit 
                // If we are at least decently profitable (Break-Even reached) and the trend officially
                // breaks the 9 EMA, get out.
                if (profitR > 0.8 && currentPrice > fastEMA && Strategy.Close[1] > Indicators.GetFastEMA[1])
                {
                    LoggerTP.Info($"[{tc.OrderTicket.SignalName}] Trail exit: 2 consecutive closes above 9 EMA.");
                    Strategy.ExitShort(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-TSExit", tc.OrderTicket.SignalName);
                    tc.SetState(TradeState.ExitPending);
                }
            }
        }
        public void HandleAdaptiveTrailingStop(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null) return;

            IB.AdaptiveTrailingStopIndicators ATSIndicators = Indicators.GetAdaptiveTrailingStopIndicators();
            OB.AdaptiveTrailingStopParameters ATSOptParams = OptParams.GetAdaptiveTrailingStopParameters();
            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double currentPrice = Strategy.Close[0];
            double rPoints = tc.OrderTicket.Risk.ToPoints();
            double tick = Strategy.TickSize;
            double atrSpreadMultiplier = ATSOptParams.ATRSpreadMultiplier;
            double fastEMAValue = ATSIndicators.FastEMA[0];
            double slowEMAValue = ATSIndicators.SlowEMA[0];
            double atrValue = ATSIndicators.ATR[0];
            double avgSpread = atrValue * atrSpreadMultiplier; // ATR is a good proxy for 'normal' spread

            if (rPoints <= 0) return;

            if (tc.OrderTicket.Type == DAOrderType.Long || tc.OrderTicket.Type == DAOrderType.LongStopMarket)
            {
                double emaSpread = currentPrice - fastEMAValue;
                double profitR = (currentPrice - entryPrice) / rPoints;
                double newStop = tc.StopOrder.StopPrice;

                // --- HARD TRAILING (The Floor) ---
                // If we hit 3.0R, never let it drop below 1.5R.
                if (profitR >= 3.0)
                {
                    newStop = Math.Max(newStop, entryPrice + (1.5 * rPoints));
                }
                // If we hit 2.0R, use the 21 EMA as the hard stop floor.
                else if (profitR >= 1.5)
                {
                    newStop = Math.Max(newStop, slowEMAValue - (5 * tick));
                }

                // 2. INSERT VOLATILITY STRETCH HERE
                if (profitR > 2.0 && emaSpread > avgSpread)
                {
                    // Lock in the spike using the previous bar's low
                    newStop = Math.Max(newStop, Strategy.Low[1] - tick);
                }

                // COMMIT THE STOP MOVE FIRST 
                // This ensures the exchange knows your new floor before we look at EMA exits.
                if (newStop > tc.StopOrder.StopPrice)
                {
                    tc.BeginStopMovePending(newStop, TradeState.TrailingStopAdaptive);
                }

                // --- DYNAMIC EXIT (The Leash) ---
                if (profitR > 1.2)
                {
                    bool exit = false;
                    // Early Phase: Room to breathe
                    if (profitR < 2.5)
                    {
                        if (currentPrice < slowEMAValue && Strategy.Close[1] < ATSIndicators.SlowEMA[1])
                        {
                            exit = true;
                        }
                    }
                    // Mid Phase: Standard Trend Management
                    else if (profitR < 4.5)
                    {
                        if (currentPrice < fastEMAValue && Strategy.Close[1] < ATSIndicators.FastEMA[1])
                        {
                            exit = true;
                        }
                    }
                    // Climax Phase: Aggressive Harvest
                    else
                    {
                        if (currentPrice < fastEMAValue)
                        {
                            exit = true;
                        }
                    }

                    if (exit)
                    {
                        Strategy.ExitLong(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-LFinal", tc.OrderTicket.SignalName);
                        return;
                    }
                }
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short || tc.OrderTicket.Type == DAOrderType.ShortStopMarket)
            {
                double emaSpreadShort = fastEMAValue - currentPrice;
                double profitR = (entryPrice - currentPrice) / rPoints;
                double newStop = tc.StopOrder.StopPrice;

                // --- HARD TRAILING (The Floor) ---
                if (profitR >= 2.5)
                {
                    newStop = Math.Min(newStop, entryPrice - (1.25 * rPoints));
                }
                else if (profitR >= 1.2)
                {
                    newStop = Math.Min(newStop, slowEMAValue + (5 * tick));
                }

                // 2. INSERT VOLATILITY STRETCH HERE
                if (profitR > 2.0 && emaSpreadShort > avgSpread)
                {
                    // Lock in the short spike using the previous bar's high
                    newStop = Math.Min(newStop, Strategy.High[1] + tick);
                }

                // COMMIT THE STOP MOVE FIRST 
                // This ensures the exchange knows your new floor before we look at EMA exits.
                if (newStop < tc.StopOrder.StopPrice)
                {
                    tc.BeginStopMovePending(newStop, TradeState.TrailingStopAdaptive);
                }

                // --- DYNAMIC EXIT (The Leash) ---
                if (profitR > 1.0)
                {
                    bool exit = false;
                    // Short Nursery: Use 21 EMA earlier because MNQ shorts snap back fast.
                    if (profitR < 2.0)
                    {
                        if (currentPrice > slowEMAValue && Strategy.Close[1] > ATSIndicators.SlowEMA[1])
                        {
                            exit = true;
                        }
                    }
                    else if (profitR < 3.5)
                    {
                        if (currentPrice > fastEMAValue && Strategy.Close[1] > ATSIndicators.FastEMA[1])
                        {
                            exit = true;
                        }
                    }
                    else
                    {
                        if (currentPrice > fastEMAValue)
                        {
                            exit = true;
                        }
                    }

                    if (exit)
                    {
                        Strategy.ExitShort(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-SFinal", tc.OrderTicket.SignalName);
                        return;
                    }
                }
            }
        }

        public void HandleAdaptiveTrailingStopWorking(TradeContext tc)
        {
            if (tc.EntryOrder == null || tc.StopOrder == null)
            {
                return;
            }

            if (TryEarlyExit(tc))
            {
                return;
            }

            double profitRForDynamicExit = 1.2;
            double entryPrice = tc.EntryOrder.AverageFillPrice;
            double currentPrice = Strategy.Close[0];
            double fastEMA = Indicators.GetFastEMA[0];
            double slowEMA = Indicators.GetSlowEMA[0];
            double atr = Indicators.GetTrailingATR()[0];
            double rPoints = tc.OrderTicket.ActualRiskAfterInitialStopPlacement().ToPoints();
            double tick = Strategy.TickSize;


            // Hold current stop default
            double newStopPrice = tc.StopOrder.StopPrice;
            double trailStartR = tc.OrderTicket.Type == DAOrderType.Long ? 2.0 : 1.7;
            double emaOffsetMult = tc.OrderTicket.Type == DAOrderType.Long ? 0.15 : 0.10;
            double dynamicOffset = Math.Max(2 * tick, atr * emaOffsetMult);

            // ATR-based Break-Even adjustments
            // ATR expanding  → BE at 0.8R
            // ATR flat       → BE at 1.0R
            // ATR contracting → BE at 1.1R(optional advanced tweak)
            // Safety check against zero division

            if (rPoints <= 0) return;

            if (tc.OrderTicket.Type == DAOrderType.Long)
            {
                double profitR = (currentPrice - entryPrice) / rPoints; // use Close[0] internally to avoid intra-bar whip-sawing stops

                if (profitR >= 1.0)
                    newStopPrice = Math.Max(newStopPrice, entryPrice + (2 * tick));

                // EMA trail
                if (profitR >= trailStartR)
                    newStopPrice = Math.Max(newStopPrice, slowEMA + dynamicOffset);

                // R tightening
                if (profitR >= 2.5)
                    newStopPrice = Math.Max(newStopPrice, entryPrice + (2.0 * rPoints));

                if (profitR >= 4.0)
                    newStopPrice = Math.Max(newStopPrice, entryPrice + (2.5 * rPoints));

                // Apply Stop Order Modifications
                if (newStopPrice > tc.StopOrder.StopPrice)
                {
                    tc.BeginStopMovePending(newStopPrice, TradeState.TrailingStopAdaptive);
                }
            }
            else if (tc.OrderTicket.Type == DAOrderType.Short)
            {
                double profitR = (entryPrice - currentPrice) / rPoints;

                if (profitR >= 1.0)
                    newStopPrice = Math.Min(newStopPrice, entryPrice - (2 * tick));

                // EMA trail
                if (profitR >= trailStartR)
                    newStopPrice = Math.Min(newStopPrice, slowEMA - dynamicOffset);

                // R tightening
                if (profitR >= 2.5)
                    newStopPrice = Math.Min(newStopPrice, entryPrice - (2.0 * rPoints));

                if (profitR >= 4.0)
                    newStopPrice = Math.Min(newStopPrice, entryPrice - (2.5 * rPoints));

                if (newStopPrice < tc.StopOrder.StopPrice)
                {
                    tc.BeginStopMovePending(newStopPrice, TradeState.TrailingStopAdaptive);
                }
            }
        }

        public virtual bool TryEarlyExit(TradeContext tc)
        {
            bool exitTriggered = false;

            return exitTriggered;
        }

        public void OnOrderUpdateLatestDave(
            Cbi.Order order,
            double limitPrice,
            double stopPrice,
            int quantity,
            int filled,
            double averageFillPrice,
            Cbi.OrderState orderState,
            DateTime time,
            Cbi.ErrorCode error,
            string comment)
        {
            var simTime = Strategy.GetDataTimeForLogger();
            var log = LoggerTP.WithProperty("SimTime", simTime);

            log.Trace(">");

            string paramsLogString = String.Format("Id: {0} | Name: {1} | State: {2} | Filled: {3}/{4} | Limit: {5} | Stop: {6} | AvgPrice: {7} | Error: {8} | Comment: {9} | UpdateTime: {10}",
                order.Id, order.Name, orderState, filled, quantity, limitPrice, stopPrice, averageFillPrice, error, comment ?? "None", time);

            // 1. DYNAMICALLY CHOOSE LOG LEVEL BASED ON ORDER STATE AND ERROR CODES
            if (orderState == Cbi.OrderState.Rejected)
            {
                log.Warn("CRITICAL: Order REJECTED by Broker/Engine! | " + paramsLogString);
            }
            else if (error != Cbi.ErrorCode.NoError)
            {
                log.Warn("Order Error Detected | " + paramsLogString);
            }
            else
            {
                log.Debug("Order Update Received | " + paramsLogString);
            }

            try
            {
                // Fail-safe check to prevent NullReferenceExceptions on the collection lookup
                if (order == null)
                {
                    log.Warn("Received a null order object.");
                }
                else 
                {
                    TradeContext tc = TradeContexts.Find(x =>
                        x.OrderTicket != null && (
                            // 1. Post-Bound: If a unique broker ID string exists, match it exactly
                            (!string.IsNullOrEmpty(x.OrderTicket.TransactionId) && x.OrderTicket.TransactionId == order.OrderId) ||
                            // 2. Pre-Bound: First flight fallback matching the custom signal name string
                            (string.IsNullOrEmpty(x.OrderTicket.TransactionId) && x.OrderTicket.SignalName == order.Name) ||
                            // 3. Exit Phase: Match brackets back to their parent entry signal string
                            (x.OrderTicket.SignalName == order.FromEntrySignal)
                        ));

                    if (tc == null)
                    {
                        // we can hit here when orders that aren't our SL or TP are executed. For example
                        // if we call exitlong or exitshort.
                        // Added order details to make this warning actually useful for debugging.
                        log.Info("Order '{0}' (FromEntry: '{1}' not found in TradeContext List. External exit or system order assumed.",
                            order.Name, order.FromEntrySignal);
                    }
                    else
                    {
                        // --- SECTION 1: ENTRY ORDER PROCESSING ---
                        if (tc.OrderTicket.SignalName == order.Name)
                        {
                            // ASYNC BRIDGE: If this is the first time we are seeing this order, 
                            // grab the unique engine transaction ID and lock it into our context!
                            if (String.IsNullOrEmpty(tc.OrderTicket.TransactionId))
                            {
                                // ninjatrader docs say not to use order.orderId for tracking but
                                // we're going to do it anyways. They will change on us when data
                                // switches from historic to realtime so for us that's OK.
                                tc.OrderTicket.TransactionId = order.OrderId;

                                log.Info("Async ID Bound | Signal '{0}' has been bound to Transaction ID: {1}",
                                    tc.OrderTicket.SignalName, order.OrderId);
                            }

                            log.Debug("Mapping Entry Order onto TradeContext: {0}", tc.OrderTicket.SignalName);
                            tc.EntryOrder = order; 
                        }
                        // --- SECTION 2: EXIT ORDER PROCESSING (SL or TP) ---
                        else if (tc.OrderTicket.SignalName == order.FromEntrySignal)
                        {
                            if (stopPrice != 0)
                            {
                                log.Debug("Mapping Stop Order onto TradeContext: {0}", tc.OrderTicket.SignalName);
                                tc.StopOrder = order;
                            }
                            else if (limitPrice != 0)
                            {
                                log.Debug("Mapping Limit Order onto TradeContext: {0}", tc.OrderTicket.SignalName);
                                tc.LimitOrder = order;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Pass the exception along with context so you know exactly which order broke the code
                log.Error(ex, "Exception occurred during OnOrderUpdate processing for Order: {0}", order?.Name ?? "UNKNOWN");
            }
            log.Trace("<");
        }

        public virtual void OnOrderUpdate(
            Cbi.Order order,
            double limitPrice,
            double stopPrice,
            int quantity,
            int filled,
            double averageFillPrice,
            Cbi.OrderState orderState,
            DateTime time,
            Cbi.ErrorCode error,
            string comment)
        {
            // Setup logger with SimTime baked in so all logs from here on will have it
            var simTime = Strategy.GetDataTimeForLogger();
            var log = LoggerTP.WithProperty("SimTime", simTime);

            log.Trace(">");

            // FIX 1: Move defensive null check to the absolute top to protect formatting parameters
            if (order == null)
            {
                log.Warn("Received a null order object during OnOrderUpdate wrapper routing.");
                log.Trace("<");
                return;
            }

            // FIX 2: Swapped out 'order.Id' (which returned -1) to 'order.OrderId' to log the actual broker identifier
            string paramsLogString = String.Format("OrderId: {0} | Name: {1} | State: {2} | Filled: {3}/{4} | Limit: {5} | Stop: {6} | AvgPrice: {7} | Error: {8} | Comment: {9} | UpdateTime: {10}",
                order.OrderId, order.Name, orderState, filled, quantity, limitPrice, stopPrice, averageFillPrice, error, comment ?? "None", time);

            // 1. DYNAMICALLY CHOOSE LOG LEVEL BASED ON ORDER STATE AND ERROR CODES
            if (orderState == Cbi.OrderState.Rejected)
            {
                log.Warn("CRITICAL: Order REJECTED by Broker/Engine! | " + paramsLogString);
            }
            else if (error != Cbi.ErrorCode.NoError)
            {
                log.Warn("Order Error Detected | " + paramsLogString);
            }
            else
            {
                log.Debug("Order Update Received | " + paramsLogString);
            }

            try
            {
                // 2. SEARCH FOR ACTIVE TRADE CONTEXT
                //TradeContext tc = TradeContexts.Find(x =>
                //    x.OrderTicket != null && (
                //        // Post-Bound Match: Real broker transaction ID strings align
                //        (!string.IsNullOrEmpty(x.OrderTicket.TransactionId) && x.OrderTicket.TransactionId == order.OrderId) ||
                //        // Pre-Bound Match: First flight fallback matching entry string literal names
                //        (string.IsNullOrEmpty(x.OrderTicket.TransactionId) && x.OrderTicket.SignalName == order.Name) ||
                //        // Exit Bracket Match: Target protection aligns back to its parent routing name
                //        (x.OrderTicket.SignalName == order.FromEntrySignal)
                //    ));
                string signalName = string.IsNullOrEmpty(order.FromEntrySignal)
                    ? order.Name
                    : order.FromEntrySignal;

                TradeContext tc = TradeContexts.Find(x =>
                    x.OrderTicket.SignalName == signalName);

                if (tc == null)
                {
                    // Fixed typo in string interpolation formatting (added matching right parenthesis)
                    log.Info("Order '{0}' (OrderId: '{1}' | FromEntry: '{2}') not found in TradeContext List. External exit or system order assumed.",
                        order.Name, order.OrderId, order.FromEntrySignal);
                    log.Trace("<");
                    return; // FIX 3: Immediate exit halts dead execution loops on unmanaged contexts
                }

                // these local vars just for clarity on how to determine
                // if it's an entry order OR an exit order;
                bool IsEntryOrder = tc.OrderTicket.SignalName == order.Name;
                bool IsExitOrder = tc.OrderTicket.SignalName == order.FromEntrySignal;

                tc.RecordOrder(time, order);
                // --- SECTION 1: ENTRY ORDER PROCESSING ---
                if (IsEntryOrder)
                {
                    // ASYNC BRIDGE: Lock down your transaction lookup criteria upon the very first flight
                    if (string.IsNullOrEmpty(tc.OrderTicket.TransactionId))
                    {
                        tc.OrderTicket.TransactionId = order.OrderId;

                        log.Info("Async ID Bound | Signal '{0}' has been bound to Transaction ID: {1}",
                            tc.OrderTicket.SignalName, order.OrderId);
                    }

                    log.Debug("Mapping Entry Order onto TradeContext: {0} [BrokerId: {1}]", tc.OrderTicket.SignalName, order.OrderId);
                    tc.EntryOrder = order;
                }
                // --- SECTION 2: EXIT ORDER PROCESSING (SL or TP) ---
                else if (IsExitOrder)
                {
                    if (stopPrice != 0)
                    {
                        log.Debug("Mapping Stop Order onto TradeContext for Entry: {0} [OrderId: {1}]", tc.OrderTicket.SignalName, order.OrderId);
                        tc.StopOrder = order;
                    }
                    else if (limitPrice != 0)
                    {
                        log.Debug("Mapping Limit Order onto TradeContext for Entry: {0} [OrderId: {1}]", tc.OrderTicket.SignalName, order.OrderId);
                        tc.LimitOrder = order;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception occurred during OnOrderUpdate processing for Order: {0} [OrderId: {1}]",
                    order.Name ?? "UNKNOWN", order.OrderId ?? "UNKNOWN");
            }

            log.Trace("<");
        }


        public virtual void OnExecutionUpdate(
            Cbi.Execution execution,
            string executionId,
            double price,
            int quantity,
            Cbi.MarketPosition marketPosition,
            string orderId,
            DateTime time)
        {
            var simTime = Strategy.GetDataTimeForLogger();
            var log = LoggerTP.WithProperty("SimTime", simTime);

            log.Trace(">");

            // FIX 1: Move defensive null validation to the absolute top to prevent property evaluation crashes
            if (execution == null || execution.Order == null)
            {
                log.Warn("Received a null execution or associated order object. ExecId: {0} | OrderId: {1}",
                    executionId ?? "UNKNOWN", orderId ?? "UNKNOWN");
                log.Trace("<");
                return;
            }

            // FIX 2: Corrected double 'SignalName' log header typo. Correctly indexed OrderId vs. SignalName.
            log.Debug("Execution Update Received | ExecId: {0} | OrderId: {1} | SignalName: {2} | Pos: {3} | Qty: {4} @ {5} | Time: {6}",
                executionId, orderId, execution.Name ?? "UNKNOWN", marketPosition, quantity, price, time);

            try
            {
                // 1. LOOKUP: Reconcile unique transaction ID for entries, or parent signal name for exit target brackets
                //TradeContext tc = TradeContexts.Find(x =>
                //    x.OrderTicket != null && (
                //        // Match unique broker execution string directly to your ticket tracking string
                //        (!string.IsNullOrEmpty(x.OrderTicket.TransactionId) && x.OrderTicket.TransactionId == orderId) ||

                //        // Match target exit brackets back to the parent entry signal string
                //        (x.OrderTicket.SignalName == execution.Order.FromEntrySignal)
                //    ));

                string signalName = string.IsNullOrEmpty(execution.Order.FromEntrySignal)
                    ? execution.Order.Name
                    : execution.Order.FromEntrySignal;

                TradeContext tc = TradeContexts.Find(x =>
                    x.OrderTicket.SignalName == signalName);

                if (tc == null)
                {
                    log.Info("Execution '{0}' (OrderId: '{1}' | FromEntry: '{2}') not found in TradeContext List. External exit assumed.",
                        execution.Name, orderId, execution.Order.FromEntrySignal);
                    log.Trace("<");
                    return; // FIX 4: Flattened control flow branch via immediate return routing
                }

                // these local vars just for clarity on how to determine
                // if it's an entry order OR an exit order;
                bool IsEntryOrder = tc.OrderTicket.SignalName == execution.Order.Name;
                bool IsExitOrder = tc.OrderTicket.SignalName == execution.Order.FromEntrySignal;

                // updating the trade context with the execution details for further processing
                tc.RecordExecution(time, execution);

                // --- SECTION 1: ENTRY FILL PROCESSING ---
                if (IsEntryOrder)
                {
                    tc.EntryOrder = execution.Order;
                    tc.EntryDateTime = time;

                    if (execution.Order.OrderState == OrderState.Filled)
                    {
                        log.Info("ENTRY FILLED | Ticket: {0} | Instrument: {1} | Account: {2} | Direction: {3} | Fill Price: {4}",
                        tc.OrderTicket.SignalName, execution.Instrument, execution.Account, execution.MarketPosition, execution.Order.AverageFillPrice);

                        // Increment session trade counter (1-based indexing for reporting)
                        tc.TradesTakenThisSession++;

                        // FIX 3: Set TradeId to TransactionId so your persistent round-trip logs use a unique key
                        tc.RoundTripData.TradeId = tc.OrderTicket.TransactionId;
                        tc.RoundTripData.Direction = execution.MarketPosition;
                        tc.RoundTripData.InitialRisk = tc.OrderTicket.Risk.Points;
                        tc.RoundTripData.Instrument = execution.Instrument;
                        tc.RoundTripData.Account = execution.Account;
                        tc.RoundTripData.SessionTradeNumber = tc.TradesTakenThisSession;

                        ExecutionLeg entryLeg = tc.RoundTripData.Entry;
                        entryLeg.DateTime = time;
                        entryLeg.SignalPrice = tc.OrderTicket.Price;
                        entryLeg.FillPrice = execution.Order.AverageFillPrice;
                        entryLeg.Quantity = execution.Order.Filled;
                        entryLeg.Commission = execution.Commission;
                    }
                    else
                    {
                        log.Debug("Entry Order State Changed | Ticket: {0} | Current State: {1}",
                            tc.OrderTicket.SignalName, execution.Order.OrderState);
                    }
                }
                // --- SECTION 2: EXIT FILL PROCESSING (SL or TP) ---
                else if (IsExitOrder)
                {
                    if (execution.Order.StopPrice != 0)
                    {
                        log.Debug("Mapping Stop Order onto TradeContext for Entry: {0} [OrderId: {1}]", tc.OrderTicket.SignalName, orderId);
                        tc.StopOrder = execution.Order;
                    }
                    else if (execution.Order.LimitPrice != 0)
                    {
                        log.Debug("Mapping Limit Order onto TradeContext for Entry: {0} [OrderId: {1}]", tc.OrderTicket.SignalName, orderId);
                        tc.LimitOrder = execution.Order;
                    }

                    if (tc.IsClosed)
                    {
                        log.Info("EXIT FILLED | From Entry: {0} | Exit Signal: {1} | Fill Price: {2} | Commission: {3}",
                            execution.Order.FromEntrySignal, execution.Name, execution.Order.AverageFillPrice, execution.Commission);

                        ExecutionLeg exitLeg = tc.RoundTripData.Exit;
                        exitLeg.DateTime = time;
                        exitLeg.FillPrice = execution.Order.AverageFillPrice;
                        exitLeg.Commission = execution.Commission;
                        exitLeg.Quantity = execution.Order.Filled;
                        exitLeg.Reason = execution.Name; // execution.Name holds the exact exit rule descriptor name string

                        // fillout highesthigh and lowestLow
                        tc.RoundTripData.HighestHighSinceEntry = tc.HighestHighSinceEntry;
                        tc.RoundTripData.LowestLowSinceEntry = tc.LowestLowSinceEntry;

                        // 1. Determine what the intended trigger price was based on the order type
                        if (execution.Order.OrderType == OrderType.StopMarket || execution.Order.OrderType == OrderType.StopLimit)
                        {
                            exitLeg.SignalPrice = execution.Order.StopPrice;
                        }
                        else if (execution.Order.OrderType == OrderType.Limit)
                        {
                            exitLeg.SignalPrice = execution.Order.LimitPrice;
                        }
                        else // Fallback for pure Market orders (like your MaxTime exits)
                        {
                            exitLeg.SignalPrice = price;
                        }

                        // --- SECTION 3: CONDITIONAL TRACKING & OVERRIDES ---
                        if (execution.Name == "MaxTradeMinutesExitLong")
                        {
                            TradeData.MaxTimeInTradeCountLong++;
                            exitLeg.Reason = "MaxTime";
                            log.Debug("Exit reason overridden to 'MaxTime' due to MaxTradeMinutesExitLong criteria met.");
                        }
                        else if (execution.Name == "MaxTradeMinutesExitShort")
                        {
                            TradeData.MaxTimeInTradeCountShort++;
                            exitLeg.Reason = "MaxTime";
                            log.Debug("Exit reason overridden to 'MaxTime' due to MaxTradeMinutesExitShort criteria met.");
                        }

                        log.Debug("Committing complete RoundTrip trade records to persistent log. Cycle cleanup incoming.");
                        WriteTradeToLog(tc.RoundTripData);
                        // Reset active context tracking object
                        tc.RoundTripData = null;
                    }
                    else
                    {
                        log.Debug("Exit Order State Changed | Parent Entry: {0} | Current State: {1}", execution.Order.FromEntrySignal, execution.Order.OrderState);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Exception encountered inside OnExecutionUpdate loop processing ExecId: {0} for OrderId: {1}",
                            executionId ?? "UNKNOWN", orderId ?? "UNKNOWN");
            }

            log.Trace("<");
        }

        private string AnalyticsSummaryLogString(
            int tableWidth,
            TradeSummaryData TSD)
        {
            var simTime = Strategy.GetDataTimeForLogger();
            var log = LoggerTP.WithProperty("SimTime", simTime);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(new string('=', tableWidth));
            sb.AppendFormat("TRADE ANALYTICS SUMMARY | ID: {0}", TSD.TradeId).AppendLine();
            sb.AppendLine(new string('=', tableWidth));
            sb.AppendFormat("Trade Duration:             {0:h\\:mm\\:ss\\.fff}", TSD.TradeDuration).AppendLine();
            sb.AppendFormat("Gross PnL:                  {0}${1:F2}", TSD.GrossPnl >= 0 ? "+" : "-", Math.Abs(TSD.GrossPnl)).AppendLine();
            sb.AppendFormat("Est. Commissions:           -${0:F2}", TSD.TotalCommissions).AppendLine();
            sb.AppendFormat("Net PnL:                    {0}${1:F2}", TSD.NetPnl >= 0 ? "+" : "-", Math.Abs(TSD.NetPnl)).AppendLine();
            sb.AppendFormat("Entry Network Latency:      {0}.{1} seconds", TSD.EntryLatency.Seconds, TSD.EntryLatency.Milliseconds).AppendLine();
            sb.AppendFormat("Exit Network Latency:       {0}.{1} seconds", TSD.ExitLatency.Seconds, TSD.ExitLatency.Milliseconds);

            /*
                        Print(string.Format("Execution Entry Slippage:   {0:F4} pts", entrySlip));
                        Print(string.Format("Execution Exit Slippage:    {0:F4} pts", exitSlip));
                        Print(string.Format("Total Slippage Friction:    -${0:F2}", slipUsd));
                        Print(string.Format("Gross PnL:                  {0}${1:F2}", gross >= 0 ? "+" : "-", Math.Abs(gross)));
                        Print(string.Format("Est. Commissions:           -${2:F2}", comms));
                        Print(string.Format("Net PnL:                    {0}${1:F2}", net >= 0 ? "+" : "-", Math.Abs(net)));
            */
            return sb.ToString();
        }

        public virtual void OnPositionUpdate(
             Position position,
             double averagePrice,
             int quantity,
             MarketPosition marketPosition)
        {
            var simTime = Strategy.GetDataTimeForLogger();
            var log = LoggerTP.WithProperty("SimTime", simTime);
            var tradeLogger = TradeLoggerTP.WithProperty("SimTime", simTime);

            if (marketPosition == MarketPosition.Flat)
            {
                if (Strategy.SystemPerformance.AllTrades.Count > 0)
                {
                    string logFormattedOrdersTable = string.Empty;
                    Cbi.Trade lastClosedTrade = Strategy.SystemPerformance.AllTrades[Strategy.SystemPerformance.AllTrades.Count - 1];
                    Cbi.Order order = lastClosedTrade?.Entry?.Order;
                    string signalName = string.IsNullOrEmpty(order?.FromEntrySignal) ? order?.Name : order?.FromEntrySignal;
                    TradeContext tc = TradeContexts.Find(x => x.OrderTicket.SignalName == signalName);
                    int tableWidth = 60;

                    if (tc != null)
                    {
                        TradeEventTableFormatter tetf = new TradeEventTableFormatter();
                        logFormattedOrdersTable = "Trade events:" + Environment.NewLine +
                                    tetf.Format(
                                        Strategy,
                                        tc.OrderTicket.BuildSummaryTradeString(),
                                        tc.TradeEvents);

                        tableWidth = tetf.TableWidth;

                        TradeSummaryData closedTradeSummaryData = TradeSummaryData.Create(
                            lastClosedTrade,
                            tc.TradeEvents,    
                            tc.HighestHighSinceEntry,
                            tc.LowestLowSinceEntry);

                        string summaryLogString = AnalyticsSummaryLogString(tableWidth, closedTradeSummaryData);
                        if (!String.IsNullOrEmpty(logFormattedOrdersTable))
                        {
                            summaryLogString = logFormattedOrdersTable + Environment.NewLine + summaryLogString + Environment.NewLine;
                        }
                        tradeLogger.Info(summaryLogString);
                    }


                    // 2. Financial Metrics
                    // for this to work we need to have setup a comissions template on
                    // the account and in the strategy setdefaults we need to set
                    // IncludeCommission = true;
                    //

                    // 4. Slippage Calculations (Points)
                    // Short Entry: Slippage = Stop Price - Filled Price (If Filled Price is lower, it's positive slippage/slippage friction)
                    // Long Entry:  Slippage = Filled Price - Stop Price
                    double entrySlippagePts = 0.0;
                    if (lastClosedTrade.Entry.Order.OrderType == OrderType.StopMarket)
                    {
                        //entrySlippagePts = lastClosedTrade.Entry.Order.Direction == OrderDirection.Sell
                        //    ? lastClosedTrade.Entry.Order.StopPrice - entryPx
                        //    : entryPx - lastClosedTrade.Entry.Order.StopPrice;
                    }

                    double exitSlippagePts = 0.0;
                    if (lastClosedTrade.Exit.Order.OrderType == OrderType.StopMarket)
                    {
                        //exitSlippagePts = lastClosedTrade.Exit.Order.Direction == OrderDirection.Buy
                        //    ? exitPx - lastClosedTrade.Exit.Order.StopPrice
                        //    : lastClosedTrade.Exit.Order.StopPrice - exitPx;
                    }
                    /*

                                        // Total Cash Lost strictly due to execution slippage
                                        double totalSlippageUsd = (entrySlippagePts + exitSlippagePts) * totalQty * Instrument.MasterInstrument.PointValue;

                                        // 5. Append Analytics to NinjaScript Output Log
                                        LogAnalyticsSummary(tradeId, tradeDuration, entryLatency, exitLatency, entrySlippagePts, exitSlippagePts, totalSlippageUsd, grossPnl, totalCommissions, netPnl);
                    */
                }

                LoggerTP.Info("OnPositionUpdate - MarketPosition is flat. Reseting all TradeContexts.");
                foreach (TradeContext tcr in TradeContexts)
                {
                    if (tcr.OrderTicket != null)
                    {
                        tcr.SetState(TradeState.Exited);
                    }
                }
            }
        }

        private void WriteTradeToLog(ClosedTrade tradeData)
        {
            CompletedTradeReportGenerator rptGen = new CompletedTradeReportGenerator(Strategy);            
            // Write trade data
            rptGen.LogCompletedTrade(tradeData);
        }

        public void OnOrderTrace(
            DateTime timestamp,
            string message)
        {
            if (LoggerTP.IsTraceEnabled)
            {
                LoggerTP.Trace("timestamp:{0}  message:{1}", timestamp, message);
            }
        }

        #endregion
    }
}
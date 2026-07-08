using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.DAustin.Common.Reporting;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NLog;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common
{
    public class TradeContext
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Logger _loggerTP = null;
        private bool _fullyInitialized = false;
        private Logger LoggerTP
        {
            get
            {
                if (_loggerTP == null || _fullyInitialized == false && EntryConditionsEvaluator != null)
                {
                    (_loggerTP, _fullyInitialized) = EntryConditionsEvaluator.Strategy.CreateLoggerWithBaseProps(_logger);
                }
                return _loggerTP;
            }
        }

        #region Properties
        public TradeState State { get { return StateList[TradeStateIndex]; } }
        public List<TradeState> StateList { get; set; } = null;
        private int _tradeStateIndex = 0;
        public int TradeStateIndex
        { 
            get {  return _tradeStateIndex; } 
            set
            {
                if (value < 0 || value >= StateList.Count)
                {
                    LoggerTP.Error($"Attempted to set invalid TradeStateIndex: {value}. StateList count: {StateList.Count}");
                    throw new ArgumentOutOfRangeException("TradeStateIndex", $"Value must be between 0 and {StateList.Count - 1}");
                }
                _tradeStateIndex = value;
                LoggerTP.Info($"TradeState set to {StateList[_tradeStateIndex]} (index={_tradeStateIndex})");
            }
        }

        public int EntryQuantityFilled { get; private set; } = 0;
        public int ExitQuantityFilled { get; private set; } = 0;
        public bool IsClosed => ExitQuantityFilled >= EntryQuantityFilled;
        public OrderTicket OrderTicket { get; set; }
        public Cbi.Order EntryOrder { get; set; } = null;
        public Cbi.Order StopOrder { get; set; } = null;
        public Cbi.Order LimitOrder { get; set; } = null;
        public DateTime EntryDateTime { get; set; } = DateTime.MinValue;
        public bool EntrySet { get; set; } = false;
        public bool SLSet { get; set; } = false;
        public bool TPSet { get; set; } = false;
        public double TrailStopPrice { get; set; } = 0;
        public double LastSetStopPrice { get; set; } = 0;
        public double HighestHighSinceEntry { get; set; } = 0;
        public double LowestLowSinceEntry { get; set; } = 0;
        public bool StopMovedToBreakEven { get; set; } = false;
        public int TradesTakenThisSession { get; set; } = 0;

        public TradeState? PendingNextState { get; set; } = null;
        public double PendingStopPrice { get; set; } = 0;
        public DateTime PendingStopSubmittedTime { get; set; } = DateTime.MinValue;

        public IEntryConditionsEvaluator EntryConditionsEvaluator { get; set; } = null;
        private ClosedTrade roundTripTradeData = null;
        public ClosedTrade RoundTripData 
        { 
            get
            {
                if (roundTripTradeData == null)
                {
                    roundTripTradeData = new ClosedTrade();
                    roundTripTradeData.StrategyVersion = EntryConditionsEvaluator.Strategy.StrategyVersion;
                }
                return roundTripTradeData;  
            } 
            set { roundTripTradeData = value; }
        }

        // Tracks how many R-multiples of profit have been locked in via step trailing.
        // 0 = at break-even, 1 = 1R locked, 2 = 2R locked, etc.
        // Stop only ever moves forward — never back.
        public int TrailStepReached { get; set; } = 0;

        public List<TradeEvent> TradeEvents = new List<TradeEvent>();
        #endregion

        #region Constructors
        public TradeContext() 
        {
            Array enumValuesArray = Enum.GetValues(typeof(TradeState));
            StateList = enumValuesArray.Cast<TradeState>().ToList();
        }

        public TradeContext(IEntryConditionsEvaluator ece): this()
        {
            EntryConditionsEvaluator = ece;
        }
        #endregion

        #region PublicMethods
        public double RoundToNearestValidTick(double rawPrice)
        {
            double tickSize = OrderTicket.Strategy.TickSize;

            return Math.Round(rawPrice / tickSize) * tickSize;
        }

        public bool StopIsInProfit()
        {
            bool stopIsInProfit = false;

            if (EntryOrder != null)
            {
                double stopPrice = LastSetStopPrice;
                if (StopOrder != null)
                {
                    stopPrice = StopOrder.StopPrice;
                }

                if (OrderTicket.Type == DAOrderType.Long)
                {
                    stopIsInProfit = stopPrice >= EntryOrder.AverageFillPrice;
                }
                else if (OrderTicket.Type == DAOrderType.Short)
                {
                    stopIsInProfit = stopPrice <= EntryOrder.AverageFillPrice;
                }
            }
            return stopIsInProfit;
        }

        public void UpdateStop(double newStopPrice)
        {
            StopOrder = null;
            newStopPrice = RoundToNearestValidTick(newStopPrice);
            LastSetStopPrice = newStopPrice;
            OrderTicket.Strategy.SetStopLoss(OrderTicket.SignalName, CalculationMode.Price, newStopPrice, false);
        }

        public void BeginStopMovePending(double newStopPrice, TradeState nextState)
        {
            newStopPrice = RoundToNearestValidTick(newStopPrice);
            PendingStopPrice = newStopPrice;
            PendingNextState = nextState;
            LoggerTP.Info($"PendingNextState set to {PendingNextState}");
            PendingStopSubmittedTime = OrderTicket?.Strategy?.Time != null ? OrderTicket.Strategy.Time[0] : DateTime.UtcNow;
            UpdateStop(newStopPrice);
            SetState(TradeState.StopMovePending);
        }

        public TradeEvent TradeEventFromOrder(DateTime time, Cbi.Order order)
        {
            TradeEvent te = null;

            if (order != null)
            {
                te = new TradeEvent
                {
                    Time = time,
                    EventType = TradeEventType.Order,
                    OrderId = order.OrderId,
                    ExecutionId = string.Empty,
                    OrderState = order.OrderState,
                    OrderAction = order.OrderAction,
                    OrderType = order.OrderType,
                    Quantity = order.Quantity,
                    FilledSoFar = order.Filled,  // Cumulative contracts filled for this order
                    FillPrice = 0,
                    AverageFillPrice = order.AverageFillPrice,
                    StopPrice = order.StopPrice,
                    LimitPrice = order.LimitPrice,
                    FromEntrySignal = order.FromEntrySignal,
                    Name = order.Name,
                };
            }
            return te;
        }

        public TradeEvent TradeEventFromExecution(
            DateTime time, 
            Execution execution)
        {
            //exe.quantity   - Contracts filled in THIS specific event
            //Order.Filled - Cumulative contracts filled for this order
            //Order.Quantity - Total contracts originally requested

            TradeEvent te = null;
            if (execution != null)
            {
                te = TradeEventFromOrder(time, execution.Order);
                if (te != null) 
                {
                    te.EventType = TradeEventType.Exec;
                    te.ExecutionId = execution.ExecutionId;
                    te.FillPrice = execution.Price;
                    te.FilledThisTime = execution.Quantity;
                }
            }
            return te;
        }

        public void AddToTradeEventList(DateTime time, Cbi.Order order)
        {
            TradeEvent te = TradeEventFromOrder(time, order);
            if (te == null)
            {
                LoggerTP.Warn($"TradeEventFromOrder returned null for order {order?.OrderId}");
                return;
            }
            else
            {
                te.SequenceNumber = TradeEvents.Count + 1;
                TradeEvents.Add(te);
            }
        }

        public void AddToTradeEventList(
            DateTime time, 
            Execution execution)
        {
            TradeEvent te = TradeEventFromExecution(time, execution);
            if (te == null)
            {
                LoggerTP.Warn($"TradeEventFromExecution returned null for execution {execution?.ExecutionId}");
                return;
            }
            else
            {
                te.SequenceNumber = TradeEvents.Count + 1;
                TradeEvents.Add(te);
            }
        }

        public int Quantity { get; set; } = 0;
        public bool IsEntry { get; set; } = false;
        public OrderState OrderState { get; set; } = OrderState.Unknown;
        public OrderType OrderType { get; set; } = Cbi.OrderType.Unknown;
        public OrderAction OrderAction { get; set; } = Cbi.OrderAction.SellShort;
        public string OrderId { get; set; } = string.Empty;
        public double StopPrice { get; set; } = 0;
        public double LimitPrice { get; set; } = 0;
        public string FromEntrySignal { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public void RecordOrder(DateTime orderUpdateTime, Cbi.Order order)
        {
            AddToTradeEventList(orderUpdateTime, order);
        }

        public void RecordExecution(
            DateTime time, 
            Execution execution)
        {
            if (IsEntryExecution(execution))
                EntryQuantityFilled += execution.Quantity;
            else
                ExitQuantityFilled += execution.Quantity;
            AddToTradeEventList(time, execution);
        }

        public bool IsEntryExecution(Execution execution)
        {
            return IsEntryOrder(execution.Order);
        }
        public bool IsEntryOrder(Cbi.Order order)
        {
            return order != null && string.IsNullOrEmpty(order.FromEntrySignal);
        }

        public bool IsExitExecution(Execution execution)
        {
            return IsExitOrder(execution.Order);
        }
        public bool IsExitOrder(Cbi.Order order)
        {
            return order != null && !string.IsNullOrEmpty(order.FromEntrySignal);
        }

        public TradeState AdvanceToNextState()
        {
            if (TradeStateIndex < StateList.Count - 1)
            {
                TradeStateIndex++;
            }
            return StateList[TradeStateIndex];
        }

        public TradeState GetNextState()
        {
            int newIndex = TradeStateIndex;
            if (newIndex < StateList.Count - 1)
            {
                newIndex++;
            }
            return StateList[newIndex];
        }

        public TradeState SetState(TradeState tradeState)
        {
            int index = StateList.IndexOf(tradeState);
            if (index >= 0)
            {
                TradeStateIndex = index;
            }
            return StateList[TradeStateIndex];
        }

        public virtual void SessionReset()
        {
            TradesTakenThisSession = 0;
            EntryConditionsEvaluator?.SessionReset();
        }

        public void Reset()
        {
            OrderTicket = null;
            SetState(TradeState.Idle);
            EntryOrder = null;
            StopOrder = null;
            LimitOrder = null;
            EntryDateTime = DateTime.MinValue;
            EntrySet = false;
            SLSet = false;
            TPSet = false;
            TrailStepReached = 0;
            TrailStopPrice = 0;
            HighestHighSinceEntry = 0;
            LowestLowSinceEntry = 0;
            StopMovedToBreakEven = false;
            EntryQuantityFilled = 0;
            ExitQuantityFilled = 0;
            TradeEvents.Clear();
            PendingNextState = null;
            PendingStopPrice = 0;
            PendingStopSubmittedTime = DateTime.MinValue;
        }
        #endregion
}
}

using NinjaTrader.Cbi;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Orders
{
    public class OrderTicket
    {
        #region Static
        private static int _signalNameIndex = 0;
        private static string _signalNamePrefix = "DAOT";
        #endregion

        #region Properties
        public int MaxTradeMinutes { get; set; } = 0; // 0 means no max trade time
        public StratBase Strategy { get; private set; }
        public DAOrderType Type { get; set; } = DAOrderType.None;
        public bool IsShort { get { return Type == DAOrderType.Short || Type == DAOrderType.ShortStopMarket; } }
        public bool IsLong { get { return Type == DAOrderType.Long || Type == DAOrderType.LongStopMarket; } }
        private string _contractsSetOrderId = string.Empty;
        private int _contracts = 0;
        public int Contracts 
        { 
            get
            {
                if (_contracts == 0 || String.IsNullOrEmpty(_contractsSetOrderId) || _contractsSetOrderId != SignalName)
                {
                    _contractsSetOrderId = SignalName;
                    _contracts = CalculateContractCount();
                }
                return _contracts;
            }
        }
        public double TPRValue { get; set; } = 0;
        public FlexibleValue TPOffset { get; set; } = null;
        public FlexibleValue SLOffset { get; set; } = null;
        private double _price = 0;
        public double Price 
        { 
            get { return _price; }
            set 
            { 
                _price = Strategy.Instrument.MasterInstrument.RoundToTickSize(value);
            }
        }
        public int StopExpiryBars { get; set; } = 0;
        public int BarIndexEntered { get; set; } = 0;
        public FlexibleValue Risk { get; set; } = null;
        public double AllowedRiskPercentOfAccount { get; set; } = 1;
        public string TransactionId { get; set; } = string.Empty;
        public string SignalNamePrefix { get; set; } = string.Empty;

        private string _signalName = string.Empty;
        public string SignalName 
        {  
            get
            {   // the SignalName will get generated when first asked for
                if (String.IsNullOrEmpty(_signalName))
                {
                    _signalName = GenerateSignalName();
                }
                return _signalName;
            }        
        }

        //legacy properties
        public double TPPrice { get; set; } = 0;
        public double SLPrice { get; set; } = 0;
        #endregion

        #region Constructors
        public OrderTicket(StratBase strat, string signalNamePrefix)
        {
            Strategy = strat;
            SignalNamePrefix = signalNamePrefix;
        }
        #endregion

        #region PrivateMethods
        private int CalculateContractCount()
        {
            double cashValue = Strategy.Account.Get(AccountItem.CashValue, Currency.UsDollar);
            double allowedRiskDollars = (cashValue * AllowedRiskPercentOfAccount) / 100;

            double riskDollars = Risk.Dollars;
            if (SLOffset != null)
            {   // use the SLOffset to calculate the contracts based on allowed risk in dollars.
                // This allows the strategy to adjust the stop loss dynamically.
                riskDollars = SLOffset.Dollars;
            }
            int contracts = (int)Math.Floor(allowedRiskDollars / riskDollars);
            return contracts;
        }
        #endregion

        #region PublicMethods
        public  string BuildSummaryTradeString()
        {
            string summaryTradeString = "";

            try
            {
                summaryTradeString = String.Format(
                    "Trade: {0}    Direction: {1}    Qty: {2}    Instrument: {3}",
                    SignalName,
                    IsLong ? "Long" : "Short",
                    _contracts,
                    Strategy?.Instrument?.MasterInstrument.Name);
            }

            catch (Exception ex)
            {

            }

            return summaryTradeString;
        }

        public FlexibleValue ActualRiskAfterInitialStopPlacement()
        {
            if (SLOffset != null)
            {   // if the SLOffset is set then use it to calculate the
                // actual risk after the initial stop placement.
                return SLOffset;
            }
            return Risk;
        }

        public string GenerateSignalName()
        {
            string sigName = string.Empty;

            if (String.IsNullOrEmpty(SignalNamePrefix))
            {
                sigName = _signalNamePrefix + "-" + _signalNameIndex.ToString("D4");
            }
            else
            {
                sigName = SignalNamePrefix + "-" + _signalNameIndex.ToString("D4");
            }

            _signalNameIndex++;

            return sigName;
        }

        public Order PlaceEntry(TradeContext tc)
        {
            int riskWholeTicks = Risk.ToTicksInt();
            Order order = null;

            if (Contracts > 0 && riskWholeTicks > 0 && Type != DAOrderType.None)
            {
                BarIndexEntered = Strategy.CurrentBar;
                if (Type == DAOrderType.Short)
                {
                    tc.EntrySet = true;
                    order = Strategy.EnterShort(Contracts, SignalName);
                }
                else if (Type == DAOrderType.Long)
                {
                    tc.EntrySet = true;
                    order = Strategy.EnterLong(Contracts, SignalName);
                }
                else if (Type == DAOrderType.LongStopMarket && Price > 0)
                {
                    tc.EntrySet = true;
                    if (StopExpiryBars > 0)
                    {   // manually call CancelOrder
                        order = Strategy.EnterLongStopMarket(
                            barsInProgressIndex: 0,
                            isLiveUntilCancelled: true,
                            quantity: Contracts,
                            stopPrice: Price, 
                            signalName: SignalName);
                    }
                    else
                    {   // this expires after 1 bar
                        order = Strategy.EnterLongStopMarket(Contracts, Price, SignalName);
                    }
                }
                else if (Type == DAOrderType.ShortStopMarket && Price > 0)
                {
                    tc.EntrySet = true;
                    if (StopExpiryBars > 0)
                    {   // manually call CancelOrder
                        order = Strategy.EnterShortStopMarket(
                            barsInProgressIndex: 0,
                            isLiveUntilCancelled: true,
                            quantity: Contracts,
                            stopPrice: Price,
                            signalName: SignalName);
                    }
                    else
                    {   // this expires after 1 bar
                        order = Strategy.EnterShortStopMarket(Contracts, Price, SignalName);
                    }
                }
            }
            return order;
        }

        public bool PlaceStopsAndTargets(TradeContext tc)
        {
            int riskWholeTicks = Risk.ToTicksInt();

            if (Type != DAOrderType.None && Contracts > 0 || riskWholeTicks > 0)
            {   // when setting ticks always use a positive value. Always set the entry after the
                // stop and target. When the entry is set it will automatically set the SLTP
                // accordingly. When the mode is ticks the SLTP ticks are the number of ticks
                // away from the fill pric e. So if you want a stop loss that is 10 ticks away
                // from the fill price you set the SLTicks to 10 and the mode to ticks.
                // The same goes for the profit target.
                int slWholeTicks = SLOffset != null ? SLOffset.ToTicksInt() : riskWholeTicks;
                tc.SLSet = true;
                Strategy.SetStopLoss(SignalName, CalculationMode.Ticks, slWholeTicks, false);

                if (TPOffset != null)
                {   // TPOffset takes precedence: take profit set independently of risk
                    tc.TPSet = true;
                    Strategy.SetProfitTarget(SignalName, CalculationMode.Ticks, TPOffset.ToTicksInt(), false);
                }
                else if (TPRValue > 0)
                {   // takeprofit is only set if the TPRValue is greater than 0. The TPRValue
                    // is the ratio of the profit target to the stop loss. So if the stop
                    // loss is 10 ticks and the TPRValue is 2 then the profit target will be 20 ticks.
                    tc.TPSet = true;
                    int TPTicks = (int)Math.Round(Risk.ToTicks() * TPRValue);
                    Strategy.SetProfitTarget(SignalName, CalculationMode.Ticks, TPTicks, false);
                }
            }
            return tc.SLSet || tc.TPSet;
        }

        #endregion
    }
}

using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.NinjaScript.Strategies.DAustin.PowerHour
{
    public class TradeTriggerChecker
    {
        #region Properties
        public StratBase Strategy { get; private set; }
        public TimeWindowPriceRange PowerHourPriceRange { get; set; }
        public DAPHIndicators Indicators { get; set; }
        public bool VWAPFilterEnabled { get; set; } = true;
        public bool PriorSessionFilterEnabled { get; set; } = false;
        #endregion

        #region constructors
        public TradeTriggerChecker(StratBase strat)
        {
            Strategy = strat;
        }
        #endregion

        #region PublicMethods
        public OrderTicket Triggered()
        {
            OrderTicket order = new OrderTicket(Strategy, "PHMom");

            // this callwill set the order direction of returns true
            if (    IsHighestOrLowestSoFar(order) && 
                    VolumeConfirm(order) && 
                    VWAPConfirm(order) &&
                    PriorSessionConfirm(order))
            {   // passed all the filters. Now set the rest 
                // of the orderInputParams
                CompleteOrderInputParams(order);
            }

            return order;
        }
        #endregion

        #region PrivateMethods
        private bool IsHighestOrLowestSoFar(OrderTicket order)
        {
            double currentPrice = Strategy.Close[0];

            order.Type = DAOrderType.None;
            if (currentPrice > PowerHourPriceRange.RangeHigh)
            {
                order.Type = DAOrderType.Long;
            }
            else if (currentPrice < PowerHourPriceRange.RangeLow) 
            {
                order.Type = DAOrderType.Short;
            }

            return order.Type != DAOrderType.None;
        }

        private bool PriorSessionConfirm(OrderTicket order)
        {
            if (PriorSessionFilterEnabled == true)
            {
                double yesterdayClose = Strategy.PriorDayOHLC().PriorClose[0];
                double yesterdayOpen = Strategy.PriorDayOHLC().PriorOpen[0];

                if (yesterdayClose > yesterdayOpen && order.Type == DAOrderType.Short)
                {
                    order.Type = DAOrderType.None;
                }
                else if (yesterdayClose < yesterdayOpen && order.Type == DAOrderType.Long)
                {
                    order.Type = DAOrderType.None;
                }
            }

            return order.Type != DAOrderType.None;
        }

        private bool VolumeConfirm(OrderTicket order)
        {
            double currentBarVolume = Strategy.Volume[0];
            bool volumeOk = currentBarVolume > Indicators.VolSMA20[0];

            if (volumeOk == false)
            {
                order.Type = DAOrderType.None;
            }

            return order.Type != DAOrderType.None;
        }

        private bool VWAPConfirm(OrderTicket order)
        {
            if (VWAPFilterEnabled == true)
            {
                double currentPrice = Strategy.Close[0];
                double vwap = Indicators.NYSessionAnchoredVWAP.Value;
                double d1 = Indicators.NYSessionAnchoredVWAP.History.Get(0);
                double d2 = Indicators.NYSessionAnchoredVWAP.History.Get(5);
                double vwapSlope = d1 - d2;
                DAOrderType currentOrderType = order.Type;

                // set to none and return to prev state if passes filter
                order.Type = DAOrderType.None;
                if (currentOrderType == DAOrderType.Long)
                {
                    if (currentPrice > vwap && vwapSlope > 0)
                    {
                        order.Type = DAOrderType.Long;
                    }
                }
                else if (currentOrderType == DAOrderType.Short)
                {
                    if (currentPrice < vwap && vwapSlope < 0)
                    {
                        order.Type = DAOrderType.Short;
                    }
                }
            }
            return order.Type != DAOrderType.None;
        }

        private void CompleteOrderInputParams(OrderTicket order)
        {
            //order.Contracts = 2;
            //order.SignalNamePrefix = "PHMom";
            //double currentPrice = Strategy.Close[0];
            //double TPRMultiple = 2.5;

            //if (order.Type == DAOrderType.Long)
            //{
            //    order.SLPrice = PowerHourPriceRange.RangeHigh - (2 * Strategy.TickSize);
            //    double risk = currentPrice - PowerHourPriceRange.RangeHigh;
            //    order.TPPrice = currentPrice + (risk * TPRMultiple);
            //}
            //else if (order.Type == DAOrderType.Short)
            //{
            //    order.SLPrice = PowerHourPriceRange.RangeLow + (2 * Strategy.TickSize);
            //    double risk = PowerHourPriceRange.RangeLow - currentPrice;
            //    order.TPPrice = currentPrice - (risk * TPRMultiple);
            //}
            //else
            //{   // if neither long or short don't execute
            //    order.Contracts = 0;
            //}
        }
        #endregion
    }
}

using ActiproSoftware.Text.Utility;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Indicators;
using NinjaTrader.Custom.Strategies.DAustin.OptimizationParameters;
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
using NinjaTrader.Custom.DAustin.Common;

namespace NinjaTrader.Custom.Strategies.DAustin.TradeManagers
{
    public class TradeManager_ORB : TradeManagerBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #region Properties
        public Indicators_ORB IndicatorsORB { get { return Indicators as Indicators_ORB; } }
        public OptimizationParameters_ORB OptParamsORB { get { return OptParams as OptimizationParameters_ORB; } }
        #endregion

        #region Constructors
        public TradeManager_ORB(StratBase strat) : base(strat) 
        { 
        
        }
        #endregion

        #region Overrides
        public override bool TryEarlyExit(TradeContext tc)
        {
            bool exitTriggered = false;
            EarlyExitMode eem = OptParams.GetEarlyExitMode();
            TimeWindowPriceRange tw = IndicatorsORB.OpeningRange;
            double range = tw.RangeHigh - tw.RangeLow;
            double ORMidpoint = tw.RangeHigh - (range * .5);
            double longRotationExit = tw.RangeHigh - (range * 0.75);
            double shortRotationExit = tw.RangeLow + (range * 0.75);
            bool stopInProfit = tc.StopIsInProfit();

            if (stopInProfit == false)
            {
                if (tc.OrderTicket.Type == DAOrderType.Long)
                {
                    if (!exitTriggered && (eem == EarlyExitMode.NoFollowThrough || eem == EarlyExitMode.Combined))
                    {
                        int barsSince = Strategy.BarsSinceEntryExecution(tc.EntryOrder.Name);
                        if (barsSince >= 1 && Strategy.High[0] <= Strategy.High[1])
                        {
                            Strategy.ExitLong("NoFollowThroughLong", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.MidpointFailure || eem == EarlyExitMode.Combined))
                    {
                        if (Strategy.Close[0] < ORMidpoint)
                        {
                            Strategy.ExitLong("MidFailLong", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.RangeRotation || eem == EarlyExitMode.Combined))
                    {
                        bool rotatingDown = Strategy.Close[0] < Strategy.Close[1] && Strategy.Close[1] < Strategy.Close[2];
                        if ((Strategy.Close[0] < longRotationExit) && rotatingDown)
                        {
                            Strategy.ExitLong("RotationExitLong", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.ATRFailure || eem == EarlyExitMode.Combined))
                    {
                        double mfe = (Strategy.High[0] - tc.EntryOrder.AverageFillPrice) / tc.OrderTicket.Risk.Points;
                        double mae = (tc.EntryOrder.AverageFillPrice - Strategy.Low[0]) / tc.OrderTicket.Risk.Points;

                        if (mfe < 0.3 && mae > 0.4)
                        {
                            Strategy.ExitLong("MomentumFailureLong", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }
                }
                else if (tc.OrderTicket.Type == DAOrderType.Short)
                {
                    if (!exitTriggered && (eem == EarlyExitMode.NoFollowThrough || eem == EarlyExitMode.Combined))
                    {
                        int barsSince = Strategy.BarsSinceEntryExecution(tc.EntryOrder.Name);
                        if (barsSince >= 1 && Strategy.Low[0] >= Strategy.Low[1])
                        {
                            Strategy.ExitShort("NoFollowThroughShort", tc.EntryOrder.Name);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.MidpointFailure || eem == EarlyExitMode.Combined))
                    {
                        if (Strategy.Close[0] > ORMidpoint)
                        {
                            Strategy.ExitShort("MidFailShort", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.RangeRotation || eem == EarlyExitMode.Combined))
                    {
                        bool rotatingUp = Strategy.Close[0] > Strategy.Close[1] && Strategy.Close[1] > Strategy.Close[2];
                        if ((Strategy.Close[0] > shortRotationExit) && rotatingUp)
                        {
                            Strategy.ExitShort("RotationExitShort", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }

                    if (!exitTriggered && (eem == EarlyExitMode.ATRFailure || eem == EarlyExitMode.Combined))
                    {
                        double mfe = (tc.EntryOrder.AverageFillPrice - Strategy.Low[0]) / tc.OrderTicket.Risk.Points;
                        double mae = (Strategy.High[0] - tc.EntryOrder.AverageFillPrice) / tc.OrderTicket.Risk.Points;

                        if (mfe < 0.3 && mae > 0.4)
                        {
                            Strategy.ExitShort("MomentumFailureShort", tc.OrderTicket.SignalName);
                            exitTriggered = true;
                        }
                    }
                }
            }
            else
            {   // we are already in profit. Only doing the EMABailout if so
                // the only early exit we do is the EMABailout
                //double profitRForDynamicExit = 1.2;
                double profitRForDynamicExit = OptParamsORB.ProfitRForDynamicExit;
                double entryPrice = tc.EntryOrder.AverageFillPrice;
                double currentPrice = Strategy.Close[0];
                double rPoints = tc.OrderTicket.Risk.ToPoints();

                if (tc.OrderTicket.Type == DAOrderType.Long)
                {
                    double profitR = (currentPrice - entryPrice) / rPoints; // use Close[0] internally to avoid intra-bar whip-sawing stops

                    // If we are at least decently profitable (Break-Even reached) and the trend officially
                    // breaks the 9 EMA, get out.
                    if (profitR > profitRForDynamicExit && currentPrice < Indicators.GetFastEMA[0] && Strategy.Close[1] < Indicators.GetFastEMA[1])
                    {
                        logger.Info($"[{tc.OrderTicket.SignalName}] Trail exit: 2 consecutive closes below 9 EMA.");
                        Strategy.ExitLong(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-TSExit", tc.OrderTicket.SignalName);
                        tc.SetState(TradeState.ExitPending);
                        exitTriggered= true;
                    }
                }
                else if (tc.OrderTicket.Type == DAOrderType.Short)
                {
                    double profitR = (entryPrice - currentPrice) / rPoints;

                    // If we are at least decently profitable (Break-Even reached) and the trend officially
                    // breaks the 9 EMA, get out.
                    if (profitR > profitRForDynamicExit && currentPrice > Indicators.GetFastEMA[0] && Strategy.Close[1] > Indicators.GetFastEMA[1])
                    {
                        logger.Info($"[{tc.OrderTicket.SignalName}] Trail exit: 2 consecutive closes above 9 EMA.");
                        Strategy.ExitShort(tc.StopOrder.Quantity, tc.OrderTicket.SignalName + "-TSExit", tc.OrderTicket.SignalName);
                        tc.SetState(TradeState.ExitPending);
                        exitTriggered = true;
                    }
                }
            }
            return exitTriggered;
        }
        #endregion
    }
}

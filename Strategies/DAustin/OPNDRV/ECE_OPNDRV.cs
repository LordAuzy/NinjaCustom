using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Windows;
using ActiproSoftware.Windows.Controls;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Calendars;
using NinjaTrader.Custom.DAustin.Common.Orders;
using NinjaTrader.Custom.DAustin.Common.ScheduleFilter;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.DAustin.Logging;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Gui.PropertiesTest;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.SuperDomColumns;
using NLog;
using NTRes.NinjaTrader.Gui.Tools.Account;
using Rules1;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static NinjaTrader.CQG.ProtoBuf.MarketDataSubscription.Types;
using static NinjaTrader.CQG.ProtoBuf.Quote.Types;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace NinjaTrader.Custom.Strategies.DAustin.OPNDRV
{
    [StrategyComponentId("ECE-OPNDRV")]
    public class ECE_OPNDRV : EntryConditionsEvaluatorBase
    {
        #region Properties
        public Indicators_OPNDRV IndicatorsOPNDRV { get { return Indicators as Indicators_OPNDRV; } }
        public OptimizationParameters_OPNDRV OptParamsOPNDRV { get { return OptParams as OptimizationParameters_OPNDRV; } }
        public ECE_OPNDRV_DataCollector DataCollector { get; private set; } = new ECE_OPNDRV_DataCollector();
        public OpeningDriveState DriveState { get; private set; }
        public DriveSetup DriveSetup { get; private set; } = null;
        public DrvPullbackState PullbackState { get; private set; } = null;
        #endregion

        #region constructors
        public ECE_OPNDRV(StratBase strat)
        {
            Strategy = strat;
            Initialize();
        }
        #endregion

        #region Overrides
        public override void SessionReset()
        {
            base.SessionReset();
            DriveState = OpeningDriveState.WaitingForDrive;
            DriveSetup = null;
            PullbackState = null;
        }
        public override OrderTicket Evaluate(TradeContext tradeContext)
        {
            OrderTicket orderTicket = null;
            OPNDRV_EntryParameters EntryOptParams = OptParamsOPNDRV.Entry;
            GeneralParameters GenOptParams = OptParamsOPNDRV.General;
            EMA fastEMA = IndicatorsOPNDRV.Entry.FastEMA;
            EMA slowEMA = IndicatorsOPNDRV.Entry.SlowEMA;
            DAVWAPIndicator VWAP = IndicatorsOPNDRV.Entry.AnchoredVWAP;
            double atr = IndicatorsOPNDRV.Entry.ATR[0];
            double currentPrice = Strategy.Close[0];

            IndicatorsOPNDRV.Entry.OpeningDrive.Update();

            if (EvaluateStopOutEarly(tradeContext) == true)
            {
                return null;
            }

            // -----------------------------------
            // Opening drive not finished yet
            // -----------------------------------
            if (DriveState == OpeningDriveState.WaitingForDrive)
            {
                if (!IndicatorsOPNDRV.Entry.OpeningDrive.IsComplete)
                {   // still waiting for opening drive to complete
                    return null;
                }
                EvaluateOpeningDrive();

                // EvaluateOpeningDrive either rejected the drive or created
                // DriveSetup/PullbackState. Do not treat the drive-completion
                // bar as a pullback bar.
                return null;
            }

            if (DriveState == OpeningDriveState.DoneForSession)
            {
                return null;
            }

            // -----------------------------------
            // Must be actively evaluating pullback
            // -----------------------------------
            if (DriveState != OpeningDriveState.WaitingForPullback)
            {
                return null;
            }

            // -----------------------------------
            // Bars elapsed since drive completion
            // -----------------------------------
            int barsAfterDrive = Strategy.CurrentBar - DriveSetup.DriveCompletedBar;
            if (barsAfterDrive <= 0)
            {   // shouldn't happen, but just in case
                return null;
            }

            // -----------------------------------
            // Update evolving pullback state
            // -----------------------------------
            PullbackState.IncrementBars();
            double barRange = Strategy.High[0] - Strategy.Low[0];
            PullbackState.UpdateBarRange(barRange, atr);

            double vwap = VWAP[0];

            if (DriveSetup.Direction == MarketPosition.Long)
            {
                PullbackState.UpdateLow(Strategy.Low[0]);
                double vwapPenetration = Math.Max(0, vwap - Strategy.Low[0]);
                PullbackState.UpdateVWAPPenetration(vwapPenetration);
            }
            else if (DriveSetup.Direction == MarketPosition.Short)
            {
                PullbackState.UpdateHigh(Strategy.High[0]);
                double vwapPenetration = Math.Max(0, Strategy.High[0] - vwap);
                PullbackState.UpdateVWAPPenetration(vwapPenetration);
            }

            if (barsAfterDrive < EntryOptParams.PullbackMinBars)
            {
                return null;
            }

            if (barsAfterDrive > EntryOptParams.PullbackMaxBars)
            {
                DriveState = OpeningDriveState.DoneForSession;
                return null;
            }

            // ===================================
            // LONG
            // ===================================
            if (DriveSetup.Direction == MarketPosition.Long)
            {
                // -----------------------------------------
                // Actual proposed stop-entry price
                // -----------------------------------------
                double entryPrice = Strategy.High[0] + Strategy.TickSize;

                // -----------------------------------------
                // Pullback qualification
                // -----------------------------------------
                double rtp = PullbackState.RetracementPct;

                bool retracementValid = rtp >= EntryOptParams.MinRetracementPct && rtp <= EntryOptParams.MaxRetracementPct;
                bool vwapValid = PullbackState.MaxVWAPPenetration <= EntryOptParams.MaxVWAPPenetrationATR * atr;
                bool trendValid = fastEMA[0] > slowEMA[0] && Strategy.Close[0] > vwap;
                bool controlledBar = barRange <= EntryOptParams.MaxPullbackBarRangeATR * atr;
                bool entryDistanceValid = (entryPrice - vwap) <= EntryOptParams.MaxEntryDistanceATR * atr;

                DataCollector.DriveSetupLongCount++;
                if (retracementValid) { DataCollector.LongRetracementValidCount++; }
                if (vwapValid) { DataCollector.LongVWAPValidCount++; }
                if (trendValid) { DataCollector.LongTrendValidCount++; }
                if (controlledBar) { DataCollector.LongControlledBarCount++; }
                if (entryDistanceValid) { DataCollector.LongEntryDistanceValidCount++; }

                // -----------------------------------------
                // Entry
                // -----------------------------------------
                if (retracementValid &&
                    vwapValid &&
                    trendValid &&
                    controlledBar &&
                    entryDistanceValid)
                {
                    DataCollector.DriveSetupLongTriggeredCount++;

                    double initialStop = PullbackState.PullbackLow - EntryOptParams.InitialStopATRBuffer * atr;
                    double risk = entryPrice - initialStop;

                    if (risk <= 0)
                    {
                        return null;
                    }

                    orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                    orderTicket.Type = DAOrderType.LongStopMarket;
                    orderTicket.Price = entryPrice;
                    orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);

                    if (EntryOptParams.OrderExpiryBars > 0)
                    {
                        orderTicket.StopExpiryBars = EntryOptParams.OrderExpiryBars;
                    }

                    PullbackState.SetEntrySnapshot(atr, entryPrice, vwap);
                    DriveState = OpeningDriveState.EntrySubmitted;
                }
            }
            else if (DriveSetup.Direction == MarketPosition.Short)
            {
                // -----------------------------------------
                // Actual proposed stop-entry price
                // -----------------------------------------
                double entryPrice = Strategy.Low[0] - Strategy.TickSize;

                // -----------------------------------------
                // Pullback qualification
                // -----------------------------------------
                double rtp = PullbackState.RetracementPct;

                bool retracementValid = rtp >= EntryOptParams.MinRetracementPct && rtp <= EntryOptParams.MaxRetracementPct;
                bool vwapValid = PullbackState.MaxVWAPPenetration <= EntryOptParams.MaxVWAPPenetrationATR * atr;
                bool trendValid = fastEMA[0] < slowEMA[0] && Strategy.Close[0] < vwap;
                bool controlledBar = barRange <= EntryOptParams.MaxPullbackBarRangeATR * atr;
                bool entryDistanceValid = (vwap - entryPrice) <= EntryOptParams.MaxEntryDistanceATR * atr;

                DataCollector.DriveSetupShortCount++;
                if (retracementValid) { DataCollector.ShortRetracementValidCount++; }
                if (vwapValid) { DataCollector.ShortVWAPValidCount++; }
                if (trendValid) { DataCollector.ShortTrendValidCount++; }
                if (controlledBar) { DataCollector.ShortControlledBarCount++; }
                if (entryDistanceValid) { DataCollector.ShortEntryDistanceValidCount++; }

                // -----------------------------------------
                // Entry
                // -----------------------------------------
                if (retracementValid &&
                    vwapValid &&
                    trendValid &&
                    controlledBar &&
                    entryDistanceValid)
                {
                    DataCollector.DriveSetupShortTriggeredCount++;

                    double initialStop = PullbackState.PullbackHigh + EntryOptParams.InitialStopATRBuffer * atr;
                    double risk = initialStop - entryPrice;

                    if (risk <= 0)
                    {
                        return null;
                    }

                    orderTicket = new OrderTicket(Strategy, OrderIdPrefix);
                    orderTicket.Type = DAOrderType.ShortStopMarket;
                    orderTicket.Price = entryPrice;
                    orderTicket.Risk = FlexibleValue.FromPoints(risk, Strategy);

                    if (EntryOptParams.OrderExpiryBars > 0)
                    {
                        orderTicket.StopExpiryBars = EntryOptParams.OrderExpiryBars;
                    }

                    PullbackState.SetEntrySnapshot(atr, entryPrice, vwap);
                    DriveState = OpeningDriveState.EntrySubmitted;
                }
            }

            // -----------------------------------
            // Risk sizing
            // -----------------------------------
            if (orderTicket != null)
            {
                TradeContext_OPNDRV tcOD = tradeContext as TradeContext_OPNDRV;
                tcOD.PullbackState = PullbackState.Clone();

                //double riskMultiplier = Indicators.SizingFilter.GetCurrentSizingMultiplier(Strategy.Time[0]);
                double riskMultiplier = 1;
                double riskPct = OptParamsOPNDRV.General.EquityRiskPercent;

                orderTicket.AllowedRiskPercentOfAccount = riskPct * riskMultiplier;
            }
            return orderTicket;
        }
        #endregion

        #region PublicMethods
        public void Reset()
        {
            Initialize();
        }

        public void Initialize()
        {

        }
        #endregion

        #region PrivateMethods
        private bool EvaluateStopOutEarly(TradeContext tradeContext)
        {
            OPNDRV_EntryParameters EntryOptParams = OptParamsOPNDRV.Entry;
            GeneralParameters GenOptParams = OptParamsOPNDRV.General;
            double atr = IndicatorsOPNDRV.Entry.ATR[0];


            if (FOMCCalendar.IsFOMCDay(Strategy.Time[0]))
            {
                Logs.Trace("Is FOMC Day");
                return true;
            }

            // for this strategy, we want to be especially strict about avoiding entries on NFP days
            // due to the high volatility and potential for slippage. Even if the entry conditions are met,
            // the risk of adverse price movements around the NFP release is significant. Therefore,
            // we will skip all entries on NFP days to protect the account from unexpected losses.
            if (NFPCalendar.IsNFPDay(Strategy.Time[0]))
            {
                Logs.Trace("Is NFP Day");
                return true;
            }

            if (tradeContext.TradesTakenThisSession >= GenOptParams.MaxTradesPerSession)
            {   // max trades per session reached
                Logs.Info($"Max trades per session reached: {tradeContext.TradesTakenThisSession}/{GenOptParams.MaxTradesPerSession}");
                return true;
            }

            if (IndicatorsOPNDRV.EntryTimeWindows != null && !IndicatorsOPNDRV .EntryTimeWindows.IsInTimeWindow())
            {   // not in an entry time window
                Logs.Trace("Not in entry time window");
                return true;
            }

            if (Strategy.CurrentBars[0] < Strategy.BarsRequiredToTrade)
            {   // in preload phase
                Logs.Trace("In preload phase");
                return true;
            }

            if (Strategy.CurrentBars[0] < Math.Max(EntryOptParams.ATRPeriod, EntryOptParams.SlowEMAPeriod))
            {   // not enough bars to calculate indicators
                Logs.Trace("Not enough bars to calculate indicators");
                return true;
            }

            if (atr <= 0)
            {
                return true;
            }

            return false;
        }

        private void EvaluateOpeningDrive()
        {
            OPNDRV_EntryParameters p = OptParamsOPNDRV.Entry;
            Indicators_OPNDRV.EntryIndicators i = IndicatorsOPNDRV.Entry;

            DriveState ds = ExtractDriveState();

            // --------------------------------------------
            // Sanity checks
            // --------------------------------------------

            if (ds.ATR <= 0 || ds.Range <= 0)
            {
                DriveState = OpeningDriveState.DoneForSession;
                return;
            }

            // --------------------------------------------
            // Basic opening-drive qualification
            // --------------------------------------------

            // NetMoveAtr is signed:
            //
            //   positive = upward opening displacement
            //   negative = downward opening displacement
            //
            // Efficiency is direction-neutral:
            //
            //   abs(Close - Open) / (High - Low)
            //
            // A value near 1 means the opening range was used
            // efficiently in one direction.
            //
            bool longDisplacement = ds.NetMoveAtr >= p.MinNetMoveATR;
            bool shortDisplacement = ds.NetMoveAtr <= -p.MinNetMoveATR;
            bool driveNotTooExtended = Math.Abs(ds.NetMoveAtr) <= p.MaxNetMoveATR;
            bool efficient = ds.Efficiency >= p.MinDriveEfficiency;

            // --------------------------------------------
            // Close location within opening-drive range
            // --------------------------------------------
            double closeLocation = (ds.Close - ds.Low) / ds.Range;
            // 0.0 = close at drive low
            // 1.0 = close at drive high
            bool longCloseLocation = closeLocation >= 1.0 - p.MaxCloseFromExtremePct;
            bool shortCloseLocation = closeLocation <= p.MaxCloseFromExtremePct;

            // --------------------------------------------
            // VWAP
            // --------------------------------------------
            double vwap = i.AnchoredVWAP[0];
            double signedVWAPDistanceATR = (ds.Close - vwap) / ds.ATR;
            double vwapSlope = i.AnchoredVWAP[0] - i.AnchoredVWAP[p.VWAPSlopeLookback];
            double vwapSlopeATR = vwapSlope / ds.ATR;
            bool longVWAP = signedVWAPDistanceATR >= p.MinVWAPDistanceATR && vwapSlopeATR >= p.MinVWAPSlopeATR;
            bool shortVWAP = signedVWAPDistanceATR <= -p.MinVWAPDistanceATR && vwapSlopeATR <= -p.MinVWAPSlopeATR;

            // --------------------------------------------
            // EMA structure
            // --------------------------------------------
            double emaSpread = i.FastEMA[0] - i.SlowEMA[0];
            double emaSpreadATR = emaSpread / ds.ATR;
            bool longEMA = emaSpreadATR >= p.MinEMASpreadATR;
            bool shortEMA = emaSpreadATR <= -p.MinEMASpreadATR;

            // --------------------------------------------
            // Final classification
            // --------------------------------------------
            bool longDrive =
                longDisplacement &&
                driveNotTooExtended &&
                efficient &&
                longCloseLocation &&
                longVWAP &&
                longEMA;

            bool shortDrive =
                shortDisplacement &&
                driveNotTooExtended &&
                efficient &&
                shortCloseLocation &&
                shortVWAP &&
                shortEMA;

            // --------------------------------------------
            // Freeze the qualified opening drive
            // --------------------------------------------
            MarketPosition driveDirection =
                longDrive ? MarketPosition.Long :
                shortDrive ? MarketPosition.Short :
                MarketPosition.Flat;

            if (driveDirection != MarketPosition.Flat)
            {
                DriveSetup = new DriveSetup
                {
                    Direction = driveDirection,
                    Drive = ds,
                    DriveCompletedBar = Strategy.CurrentBar,
                    VWAP = vwap,
                    VWAPSlopeATR = vwapSlopeATR,
                    VWAPDistanceATR = signedVWAPDistanceATR,
                    EMASpreadATR = emaSpreadATR,
                    CloseLocation = closeLocation
                };

                PullbackState = new DrvPullbackState(DriveSetup);
                DriveState = OpeningDriveState.WaitingForPullback;
            }
            else
            {
                DriveState = OpeningDriveState.DoneForSession;
            }
        }

        private DriveState ExtractDriveState()
        {
            DriveState driveState = new DriveState();

            driveState.High = IndicatorsOPNDRV.Entry.OpeningDrive.RangeHigh;
            driveState.Low = IndicatorsOPNDRV.Entry.OpeningDrive.RangeLow;
            driveState.Open = IndicatorsOPNDRV.Entry.OpeningDrive.RangeOpen;
            driveState.Close = IndicatorsOPNDRV.Entry.OpeningDrive.RangeClose;
            driveState.ATR = IndicatorsOPNDRV.Entry.ATR[0];

            return driveState;
        }
        #endregion

        #region VirtualMethods
        #endregion
    }
}

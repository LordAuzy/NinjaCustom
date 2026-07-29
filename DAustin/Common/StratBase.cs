#region Using declarations
using ActiproSoftware.Text.Languages.DotNet.Ast.Implementation;
using ActiproSoftware.Text.Parsing.LLParser.Implementation;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.Custom.DAustin.Common.Reporting;
using NinjaTrader.Custom.DAustin.Interfaces;
using NinjaTrader.Custom.Strategies.DAustin.Common;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.AccountData;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Strategies.DAustin.Mom_9_21_Cross;
using NLog;
using NLog.Config;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using System.Xml.Serialization;
using static NinjaTrader.CQG.ProtoBuf.Quote.Types;
using static NinjaTrader.Custom.DAustin.Common.OptimizationParametersBase;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
#endregion

namespace NinjaTrader.Custom.DAustin.Common
{
    public class StratBase : Strategy
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
                    (_loggerTP, _fullyInitialized) = CreateLoggerWithBaseProps(logger);
                }
                return _loggerTP;
            }
        }

        #region Properties
        [Browsable(false)]
        public SessionIterator SessionIterator { get; private set; }

        protected TradeManagerBase _tmb = null;
        [Browsable(false)]
        public virtual TradeManagerBase TradeManager
        {
            get
            {
                if (_tmb == null)
                {
                    _tmb = new TradeManagerBase(this);
                }
                return _tmb;
            }
        }

        [Browsable(false)]
        public virtual String StrategyVersion { get { return "0.0.0"; } }
            
        [Browsable(false)]
        public OptimizationParametersBase OptimizationParameters { get; set; } = null;

        private StratInputParams _sip = null;
        [Browsable(false)]
        public StratInputParams InputParams
        {
            get
            {
                if (_sip == null)
                {
                    _sip = new StratInputParams();
                }
                return _sip;
            }

            private set { _sip = value; }
        }

        [Browsable(false)]
        public Dictionary<string, IEntryConditionsEvaluator> EntryConditionsEvaluatortList { get; } = new Dictionary<string, IEntryConditionsEvaluator>();
        [Browsable(false)]
        public Dictionary<string, IOptimizationParameters> OptimizationParameterList { get; } = new Dictionary<string, IOptimizationParameters>();
        [Browsable(false)]
        public Dictionary<string, IIndicators> IndicatorsList { get; } = new Dictionary<string, IIndicators>();
        #endregion

        #region Overrides
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                SetNLogGDC();
                // failsafe defaults so if my strat code doesn't execute for some reason,
                // We'll close out of any open positions within 2 minutes of session close
                // to avoid overnight risk
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 120;
                IncludeCommission = true;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                SessionIterator = new SessionIterator(Bars);
            }
            else if (State == State.Terminated)
            {

            }
        }

        protected override void OnBarUpdate()
        {
            LoggerTP.Trace(">");
            TradeManager.OnBarUpdate();

            // Check if this is the final bar of the backtest
            if (State == State.Historical && CurrentBar == Count - 2)
            {
                // All trades are now closed and accounted for in backtest. This is really
                // the only place I know of to reliably get the final performance metrics
                // of the backtest. I found that when I try to read data in the
                // OnStateChange() method when State == State.Terminated, my objects have
                // been disposed and I can't get the performance metrics.
                // So, I'm doing it here in OnBarUpdate() on the last bar of the backtest.
                OnBacktestComplete();
            }
            LoggerTP.Trace("<");
        }

        protected override void OnOrderUpdate(
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
            TradeManager.OnOrderUpdate(order, limitPrice, stopPrice, quantity, filled, averageFillPrice, orderState, time, error, comment);
        }

        protected override void OnExecutionUpdate(
            Cbi.Execution execution,
            string executionId,
            double price,
            int quantity,
            Cbi.MarketPosition marketPosition,
            string orderId,
            DateTime time)
        {
            TradeManager.OnExecutionUpdate(execution, executionId, price, quantity, marketPosition, orderId, time);
        }

        protected override void OnPositionUpdate(
            Position position, 
            double averagePrice, 
            int quantity, 
            MarketPosition marketPosition)
        {
            TradeManager.OnPositionUpdate(position, averagePrice, quantity, marketPosition);
        }

        protected override void OnOrderTrace(
            DateTime timestamp,
            string message)
        {
            TradeManager.OnOrderTrace(timestamp, message);
        }

        #endregion

        #region PublicMethods
        public bool TradingLiveAccount()
        {
            // we just want to know if we are in a backtest or not
            // When in a backtest, Account.Name returns "Backtest".
            // In a live trading environment it returns the actual account name.
            return Account.Name != "Backtest";
        }

        public DateTime GetDataTimeForLogger()
        {
            DateTime dataTime = DateTime.Now;

            if (State >= State.DataLoaded && BarsArray != null && BarsInProgress < BarsArray.Length && CurrentBars[BarsInProgress] >= 0)
            {
                // Best option for Backtesting & Live trading: Captures the actual data time feed
                dataTime = Time[0];
            }
            else if (Connection.PlaybackConnection != null)
            {
                // Best fallback for visual Market Replay if data isn't fully ready yet
                dataTime = Connection.PlaybackConnection.Now;
            }
            else
            {
                // Fallback for UI threads, SetDefaults, and initial startup
                dataTime = Core.Globals.Now;
            }

            return dataTime;
        }

        protected virtual void OnBacktestComplete()
        {
            // This method can be overridden in child strats to perform any necessary
            // actions at the end of a backtest, such as logging final performance
            // metrics or exporting trade data.
        }

        public IEntryConditionsEvaluator GetEntryConditionsEvaluator(string key)
        {
            IEntryConditionsEvaluator ece = EntryConditionsEvaluatortList.ContainsKey(key) ? EntryConditionsEvaluatortList[key] : null;

            if (ece == null)
            {   // scan all loaded assemblies for a class decorated with [EvaluatorId(key)]
                try
                {
                    Assembly assy = Assembly.GetExecutingAssembly();
                    var types = assy.GetTypes().Where(type => typeof(IEntryConditionsEvaluator).IsAssignableFrom(type) &&
                            !type.IsAbstract && !type.IsAbstract);

                    foreach (var type in types)
                    {
                        StrategyComponentIdAttribute attr = (StrategyComponentIdAttribute)System.Attribute.GetCustomAttribute(type, typeof(StrategyComponentIdAttribute));

                        if (attr != null && attr.Id == key)
                        {
                            ece = (IEntryConditionsEvaluator)Activator.CreateInstance(type, this);
                            EntryConditionsEvaluatortList[key] = ece;
                            break;
                        }
                    }
                }

                catch (Exception ex)
                {
                    Print(ex);
                }
            }

            if (ece == null)
            {
                Print($"GetEntryConditionsEvaluator - no evaluator type found with Id '{key}'");
            }
            return ece;
        }

        public IOptimizationParameters GetOptimizationParameters(string key)
        {
            IOptimizationParameters optParams = OptimizationParameterList.ContainsKey(key) ? OptimizationParameterList[key] : null;

            if (optParams == null)
            {   // scan all loaded assemblies for a class decorated with [StrategyComponentId(key)]
                try
                {
                    Assembly assy = Assembly.GetExecutingAssembly();
                    var types = assy.GetTypes().Where(type => typeof(IOptimizationParameters).IsAssignableFrom(type) &&
                            !type.IsAbstract && !type.IsAbstract);

                    foreach (var type in types)
                    {
                        StrategyComponentIdAttribute attr = (StrategyComponentIdAttribute)System.Attribute.GetCustomAttribute(type, typeof(StrategyComponentIdAttribute));

                        if (attr != null && attr.Id == key)
                        {
                            optParams = (IOptimizationParameters)Activator.CreateInstance(type, this);
                            OptimizationParameterList[key] = optParams;
                            break;
                        }
                    }
                }

                catch (Exception ex)
                {
                    Print(ex);
                }
            }

            if (optParams == null)
            {
                Print($"GetOptimizationParameters - no optimization parameters type found with Id '{key}'");
            }
            return optParams;
        }

        public IIndicators GetIndicators(string key)
        {
            IIndicators indicators = IndicatorsList.ContainsKey(key) ? IndicatorsList[key] : null;

            if (indicators == null)
            {   // scan all loaded assemblies for a class decorated with [StrategyComponentId(key)]
                try
                {
                    Assembly assy = Assembly.GetExecutingAssembly();
                    var types = assy.GetTypes().Where(type => typeof(IIndicators).IsAssignableFrom(type) &&
                            !type.IsAbstract && !type.IsAbstract);

                    foreach (var type in types)
                    {
                        StrategyComponentIdAttribute attr =
                            (StrategyComponentIdAttribute)System.Attribute.GetCustomAttribute(type, typeof(StrategyComponentIdAttribute));

                        if (attr != null && attr.Id == key)
                        {
                            indicators = (IIndicators)Activator.CreateInstance(type, this);
                            IndicatorsList[key] = indicators;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print(ex);
                }
            }

            if (indicators == null)
            {
                Print($"GetIndicators - no indicators type found with Id '{key}'");
            }

            return indicators;
        }

        public (Logger lwp, bool fullyInitialized) CreateLoggerWithBaseProps(Logger baseLogger)
        {
            _fullyInitialized = true;

            // Add base properties to the logger
            string safeInstrumentName = "Unknown_Instrument";
            if (Instrument != null && Instrument.MasterInstrument != null) 
            {
                safeInstrumentName = Instrument.MasterInstrument.Name.Replace(" ", "_").Replace("/", "-");
            }
            else
            {
                _fullyInitialized = false;
            }

            string accountName = "BacktestAccount";
            if (Account != null && !string.IsNullOrEmpty(Account.Name))
            {
                accountName = Account.Name;
            }
            else
            {
                _fullyInitialized = false;
            }

            // defaulting to trace logging mode if we can't
            // get the logging mode from optimization parameters
            GeneralParameters GenP = OptimizationParameters?.GetGeneralParameters();
            LoggingMode loggingMode = GenP != null ? GenP.LoggingMode : LoggingMode.Trace;
            string NTOutputMinLevel = "Warn";
            string DiagnosticLogMinLevel = "Debug";
            string TraceLogMinLevel = "Trace";

            if (loggingMode == LoggingMode.Debug)
            {
                NTOutputMinLevel = "Warn";
                DiagnosticLogMinLevel = "Debug";
                TraceLogMinLevel = "Off";
            }
            else if (loggingMode == LoggingMode.Production)
            {
                NTOutputMinLevel = "Warn";
                DiagnosticLogMinLevel = "Info";
                TraceLogMinLevel = "Off";
            }

            if (loggingMode == LoggingMode.None || accountName == "Backtest")
            {
                // if we are in a backtest logging puts too much drag on the system
                return (LogManager.CreateNullLogger(), _fullyInitialized);
            }

            var log = baseLogger
                        .WithProperty("StrategyName", Name)
                        .WithProperty("InstrumentName", safeInstrumentName)
                        .WithProperty("AccountNumber", accountName)
                        .WithProperty("LoggingMode", loggingMode)
                        .WithProperty("NTOutputMinLevel", NTOutputMinLevel)
                        .WithProperty("DiagnosticLogMinLevel", DiagnosticLogMinLevel)
                        .WithProperty("TraceLogMinLevel", TraceLogMinLevel);

            return (log, _fullyInitialized);
        }
        #endregion

        #region PrivateMethods
        public void InitializeNLog()
        {
            return;

            string assemblyDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string nlogConfigPath = System.IO.Path.Combine(assemblyDir, "Strategies\\DAustin\\NLogConfigFiles\\NLog.config");
            // Initialize the configuration from a file path
            var config = new XmlLoggingConfiguration(nlogConfigPath);
            // Apply the configuration to the LogManager
            LogManager.Configuration = config;
        }

        public void SetNLogGDC()
        {
            // this is global and will be the same for all strats
            GlobalDiagnosticsContext.Set("TradeCSVSchemaVersion", CompletedTradeReportGenerator.TradeCSVSchemaVersion);
        }
        #endregion
    }
}

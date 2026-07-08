using ActiproSoftware.Windows.Controls.SyntaxEditor.EditActions;
using NinjaTrader.Custom.DAustin.Common;
using NinjaTrader.Custom.DAustin.Common.Orders;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Interfaces
{
    public interface IEntryConditionsEvaluator
    {
        string OrderIdPrefix { get; set; }
        StratBase Strategy { get; set; }
        int AllowedRiskPercentOfAccount { get; set; }
        IIndicators Indicators { get; set; }
        IOptimizationParameters OptParams { get; set; }
        OrderTicket Evaluate(TradeContext tradeContext);
        void Reset();
        void SessionReset();
    }
}

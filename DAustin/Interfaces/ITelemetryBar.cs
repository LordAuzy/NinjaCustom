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
    public interface ITelemetryBar
    {
        StratBase Strategy { get; set; }
        IIndicators Indicators { get; set; }
        List<string> GetColumnNames();
        List<string> GetRowData();
        void CollectData();
    }
}

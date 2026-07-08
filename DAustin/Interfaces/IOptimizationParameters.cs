using ActiproSoftware.Windows.Controls.SyntaxEditor.EditActions;
using NinjaTrader.Custom.DAustin.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Interfaces
{
    public interface IOptimizationParameters
    {
        StratBase Strategy { get; set; }
        void UpdateFromStrat();
    }
}
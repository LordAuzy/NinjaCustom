using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Interfaces
{
    public interface ICSVDataSource
    {
        List<string> GetColumnNames(List<string> columns);
        void Rewind();
        List<string> NextDataRow(List<string> data);
    }
}

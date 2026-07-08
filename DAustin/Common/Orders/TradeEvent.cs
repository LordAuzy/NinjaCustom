using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Orders
{
    public class TradeEvent
    {
        public int SequenceNumber { get; set; } = 0;
        public DateTime Time { get; set; } = DateTime.MinValue;
        public TradeEventType EventType { get; set; } = TradeEventType.Unknown;
        public string OrderId { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;
        public OrderState OrderState { get; set; } = OrderState.Unknown;
        public OrderAction OrderAction { get; set; } = Cbi.OrderAction.SellShort;
        public OrderType OrderType { get; set; } = Cbi.OrderType.Unknown;
        public int FilledSoFar { get; set; } = 0;
        public int FilledThisTime { get; set; } = 0;
        public int Quantity { get; set; } = 0;
        public double FillPrice { get; set; } = 0;
        public double AverageFillPrice { get; set; } = 0;
        public double StopPrice { get; set; }
        public double LimitPrice { get; set; }
        public string FromEntrySignal { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}


/* sample output
=============================================================================================================================================================
Trade: DAVWAPPB-0000    Direction: Long    Qty: 29    Instrument: MNQ SEP26
=============================================================================================================================================================

#  Time                  Event      OrderId      ExecId     State        Action  Type        Qty  Fill  AvgFill   StopPx    LimitPx   FromEntry      Name
-- --------------------  ---------  -----------  ---------  -----------  ------  ----------  ---  ----- --------  --------  --------  -------------  ----------------
 1  07:56:58.812         ORDER      NT-00000-56             Submitted    Buy     StopMarket   29                  26266.50             -              DAVWAPPB-0000
 2  07:56:58.818         ORDER      NT-00000-56             Accepted     Buy     StopMarket   29                  26266.50             -              DAVWAPPB-0000
 3  07:56:59.014         ORDER      NT-00000-56             Working      Buy     StopMarket   29                  26266.50             -              DAVWAPPB-0000

 4  07:57:00.103         ORDER      NT-00000-56             Filled       Buy     StopMarket   29        26266.50  26266.50             -              DAVWAPPB-0000
 5  07:57:00.103         EXEC       NT-00000-56  EX-00001   Filled       Buy     StopMarket   29 26266.50 26266.50 26266.50             -              DAVWAPPB-0000

 6  07:57:00.106         ORDER      NT-00001-56             Submitted    Sell    StopMarket   29                  26232.75             DAVWAPPB-0000  Stop loss
 7  07:57:00.109         ORDER      NT-00001-56             Accepted     Sell    StopMarket   29                  26232.75             DAVWAPPB-0000  Stop loss
 8  07:57:00.201         ORDER      NT-00001-56             Working      Sell    StopMarket   29                  26232.75             DAVWAPPB-0000  Stop loss

 9  08:07:13.587         ORDER      NT-00001-56             Filled       Sell    StopMarket   29        26232.75  26232.75             DAVWAPPB-0000  Stop loss
10  08:07:13.587         EXEC       NT-00001-56  EX-00002   Filled       Sell    StopMarket   29 26232.75 26232.75 26232.75             DAVWAPPB-0000  Stop loss  
*/


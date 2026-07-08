using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NinjaTrader.Custom.DAustin.Common.Orders;

namespace NinjaTrader.Custom.DAustin.Common.Reporting
{
    public class TradeEventTableFormatter
    {
        #region Properties
        public int TableWidth { get; private set; } = 0;

        #endregion

        #region Constructors
        public TradeEventTableFormatter() 
        { 
        
        }

        #endregion

        #region PublicMethods
        public string Format(
            StratBase strat,
            string tradeSummaryString,
            IEnumerable<TradeEvent> events)
        {
            if (events == null)
                return "<null>";

            if (events.Count() == 0)
                return "<No Trade Events>";

            string[] headers =
            {
            "#",
            "Time",
            "Event",
            "OrderId",
            "ExecutionId",
            "State",
            "Action",
            "Type",
            "Qty",
            "FldTT",
            "FldSF",
            "FillPx",
            "AvgFill",
            "StopPx",
            "LimitPx",
            "FromEntrySignal",
            "Name"
        };

            List<string[]> rows = new List<string[]>();

            int rowNum = 1;

            foreach (var e in events)
            {
                rows.Add(new[]
                {
                e.SequenceNumber.ToString(),
                e.Time == DateTime.MinValue ? "" : e.Time.ToString("HH:mm:ss.fff"),
                e.EventType.ToString(),
                e.OrderId ?? "",
                e.ExecutionId ?? "",
                e.OrderState.ToString(),
                e.OrderAction.ToString(),
                e.OrderType.ToString(),
                e.Quantity == 0 ? "" : e.Quantity.ToString(),
                e.FilledThisTime == 0 ? "" : e.FilledThisTime.ToString(),
                e.FilledSoFar == 0 ? "" : e.FilledSoFar.ToString(),
                e.FillPrice == 0 ? "" : e.FillPrice.ToString("0.#####"),
                e.AverageFillPrice == 0 ? "" : e.AverageFillPrice.ToString("0.#####"),
                e.StopPrice == 0 ? "" : e.StopPrice.ToString("0.#####"),
                e.LimitPrice == 0 ? "" : e.LimitPrice.ToString("0.#####"),
                e.FromEntrySignal ?? "",
                e.Name ?? ""
            });

                rowNum++;
            }

            int[] widths = new int[headers.Length];

            for (int i = 0; i < headers.Length; i++)
                widths[i] = headers[i].Length;

            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                    widths[i] = Math.Max(widths[i], row[i].Length);
            }

            TableWidth = widths.Sum()  // column widths
               + (headers.Length - 1) * 2; // spacing between columns

            var sb = new StringBuilder();

            // trade summary
            if (!String.IsNullOrEmpty(tradeSummaryString))
            {
                sb.AppendLine(new string('=', TableWidth));
                sb.AppendLine(tradeSummaryString);
                sb.AppendLine(new string('=', TableWidth));
            }

            // Header
            for (int i = 0; i < headers.Length; i++)
            {
                sb.Append(headers[i].PadRight(widths[i]));

                if (i < headers.Length - 1)
                    sb.Append("  ");
            }

            sb.AppendLine();

            // Divider
            for (int i = 0; i < headers.Length; i++)
            {
                sb.Append(new string('-', widths[i]));

                if (i < headers.Length - 1)
                    sb.Append("  ");
            }

            // Rows
            foreach (var row in rows)
            {
                sb.AppendLine();
                for (int i = 0; i < row.Length; i++)
                {
                    bool numeric =
                        i == 0 ||   // #
                        i == 8 ||   // Qty
                        i == 9 ||   // FldTT
                        i == 10 ||  // FldSF
                        i == 11 ||  // FillPx
                        i == 12 ||  // AvgFill
                        i == 13 ||  // StopPx
                        i == 14;    // LimitPx

                    if (numeric)
                        sb.Append(row[i].PadLeft(widths[i]));
                    else
                        sb.Append(row[i].PadRight(widths[i]));

                    if (i < row.Length - 1)
                        sb.Append("  ");
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}

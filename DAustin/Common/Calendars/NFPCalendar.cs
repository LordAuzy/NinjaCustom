using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.DAustin.Common.Calendars
{
    public static class NFPCalendar
    {
        // Actual NFP dates (when different from first Friday due to holidays)
        private static readonly HashSet<DateTime> NFPDates = new HashSet<DateTime>
        {
            // 2021
            new DateTime(2021, 1, 8),   // First Friday
            new DateTime(2021, 2, 5),
            new DateTime(2021, 3, 5),
            new DateTime(2021, 4, 2),
            new DateTime(2021, 5, 7),
            new DateTime(2021, 6, 4),
            new DateTime(2021, 7, 2),
            new DateTime(2021, 8, 6),
            new DateTime(2021, 9, 3),
            new DateTime(2021, 10, 8),
            new DateTime(2021, 11, 5),
            new DateTime(2021, 12, 3),

            // 2022
            new DateTime(2022, 1, 7),
            new DateTime(2022, 2, 4),
            new DateTime(2022, 3, 4),
            new DateTime(2022, 4, 1),
            new DateTime(2022, 5, 6),
            new DateTime(2022, 6, 3),
            new DateTime(2022, 7, 8),   // Exception: Second Friday due to July 4th holiday
            new DateTime(2022, 8, 5),
            new DateTime(2022, 9, 2),
            new DateTime(2022, 10, 7),
            new DateTime(2022, 11, 4),
            new DateTime(2022, 12, 2),

            // 2023
            new DateTime(2023, 1, 6),
            new DateTime(2023, 2, 3),
            new DateTime(2023, 3, 10),  // Exception: Second Friday due to data collection
            new DateTime(2023, 4, 7),
            new DateTime(2023, 5, 5),
            new DateTime(2023, 6, 2),
            new DateTime(2023, 7, 7),
            new DateTime(2023, 8, 4),
            new DateTime(2023, 9, 1),
            new DateTime(2023, 10, 6),
            new DateTime(2023, 11, 3),
            new DateTime(2023, 12, 8),  // Exception: Second Friday

            // 2024
            new DateTime(2024, 1, 5),
            new DateTime(2024, 2, 2),
            new DateTime(2024, 3, 8),   // Exception: Second Friday
            new DateTime(2024, 4, 5),
            new DateTime(2024, 5, 3),
            new DateTime(2024, 6, 7),
            new DateTime(2024, 7, 5),
            new DateTime(2024, 8, 2),
            new DateTime(2024, 9, 6),
            new DateTime(2024, 10, 4),
            new DateTime(2024, 11, 1),
            new DateTime(2024, 12, 6),

            // 2025
            new DateTime(2025, 1, 10),  // Exception: Second Friday
            new DateTime(2025, 2, 7),
            new DateTime(2025, 3, 7),
            new DateTime(2025, 4, 4),
            new DateTime(2025, 5, 2),
            new DateTime(2025, 6, 6),
            new DateTime(2025, 7, 3),   // Exception: Potential holiday adjustment
            new DateTime(2025, 8, 1),
            new DateTime(2025, 9, 5),
            new DateTime(2025, 10, 3),
            new DateTime(2025, 11, 7),
            new DateTime(2025, 12, 5),

            // 2026
            new DateTime(2026, 1, 9),   // Exception: Second Friday
            new DateTime(2026, 2, 6),
            new DateTime(2026, 3, 6),
            new DateTime(2026, 4, 3),
            new DateTime(2026, 5, 8),   // Exception: Second Friday
            new DateTime(2026, 6, 5),
            new DateTime(2026, 7, 2),
            new DateTime(2026, 8, 7),
            new DateTime(2026, 9, 4),
            new DateTime(2026, 10, 2),
            new DateTime(2026, 11, 6),
            new DateTime(2026, 12, 4),
        };

        /// <summary>
        /// Checks if the given date is an NFP (Non-Farm Payroll) day
        /// </summary>
        public static bool IsNFPDay(DateTime date)
        {
            return NFPDates.Contains(date.Date);
        }

        /// <summary>
        /// Checks if the given date is the calculated first Friday (without exceptions)
        /// </summary>
        public static bool IsFirstFriday(DateTime date)
        {
            if (date.DayOfWeek != DayOfWeek.Friday)
                return false;

            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int daysUntilFriday = ((int)DayOfWeek.Friday - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
            DateTime firstFriday = firstDayOfMonth.AddDays(daysUntilFriday);

            return date.Date == firstFriday.Date;
        }

        /// <summary>
        /// Gets the NFP date for a given month and year from the hardcoded list
        /// </summary>
        public static DateTime? GetNFPDate(int year, int month)
        {
            DateTime firstDayOfMonth = new DateTime(year, month, 1);
            DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            foreach (var nfpDate in NFPDates)
            {
                if (nfpDate >= firstDayOfMonth && nfpDate <= lastDayOfMonth)
                    return nfpDate;
            }

            return null; // Not found in list
        }

        /// <summary>
        /// Gets the next NFP date after the given date
        /// </summary>
        public static DateTime? GetNextNFPDate(DateTime afterDate)
        {
            DateTime? nextDate = null;
            foreach (var nfpDate in NFPDates)
            {
                if (nfpDate > afterDate.Date)
                {
                    if (!nextDate.HasValue || nfpDate < nextDate.Value)
                        nextDate = nfpDate;
                }
            }
            return nextDate;
        }

        /// <summary>
        /// Checks if the given date falls within NFP week (Sunday through Friday of NFP)
        /// </summary>
        public static bool IsNFPWeek(DateTime date)
        {
            DateTime startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek);
            for (int i = 0; i < 7; i++)
            {
                if (NFPDates.Contains(startOfWeek.AddDays(i)))
                    return true;
            }
            return false;
        }
    }
}
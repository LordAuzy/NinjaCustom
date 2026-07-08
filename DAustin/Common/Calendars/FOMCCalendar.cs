// Add this as a new file: Strategies\DAustin\Common\FOMCCalendar.cs
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.DAustin.Common.Calendars
{
    public static class FOMCCalendar
    {
        // FOMC meeting dates (scheduled decision announcement dates)
        private static readonly HashSet<DateTime> FOMCDates = new HashSet<DateTime>
        {
            // 2021
            new DateTime(2021, 1, 27),
            new DateTime(2021, 3, 17),
            new DateTime(2021, 4, 28),
            new DateTime(2021, 6, 16),
            new DateTime(2021, 7, 28),
            new DateTime(2021, 9, 22),
            new DateTime(2021, 11, 3),
            new DateTime(2021, 12, 15),

            // 2022
            new DateTime(2022, 1, 26),
            new DateTime(2022, 3, 16),
            new DateTime(2022, 5, 4),
            new DateTime(2022, 6, 15),
            new DateTime(2022, 7, 27),
            new DateTime(2022, 9, 21),
            new DateTime(2022, 11, 2),
            new DateTime(2022, 12, 14),

            // 2022
            new DateTime(2022, 1, 26),
            new DateTime(2022, 3, 16),
            new DateTime(2022, 5, 4),
            new DateTime(2022, 6, 15),
            new DateTime(2022, 7, 27),
            new DateTime(2022, 9, 21),
            new DateTime(2022, 11, 2),
            new DateTime(2022, 12, 14),

            // 2023
            new DateTime(2023, 2, 1),
            new DateTime(2023, 3, 22),
            new DateTime(2023, 5, 3),
            new DateTime(2023, 6, 14),
            new DateTime(2023, 7, 26),
            new DateTime(2023, 9, 20),
            new DateTime(2023, 11, 1),
            new DateTime(2023, 12, 13),

            // 2024
            new DateTime(2024, 1, 31),
            new DateTime(2024, 3, 20),
            new DateTime(2024, 5, 1),
            new DateTime(2024, 6, 12),
            new DateTime(2024, 7, 31),
            new DateTime(2024, 9, 18),
            new DateTime(2024, 11, 7),
            new DateTime(2024, 12, 18),
            
            // 2025
            new DateTime(2025, 1, 29),
            new DateTime(2025, 3, 19),
            new DateTime(2025, 5, 7),
            new DateTime(2025, 6, 18),
            new DateTime(2025, 7, 30),
            new DateTime(2025, 9, 17),
            new DateTime(2025, 11, 5),
            new DateTime(2025, 12, 17),
            
            // Add future years as needed
            new DateTime(2026, 1, 28),
            new DateTime(2026, 3, 18),
            new DateTime(2026, 4, 29),
            new DateTime(2026, 6, 17),
            new DateTime(2026, 7, 29),
            new DateTime(2026, 9, 16),
            new DateTime(2026, 10, 28),
            new DateTime(2026, 12, 9),
        };
        
        /// <summary>
        /// Checks if the given date is an FOMC announcement day
        /// </summary>
        public static bool IsFOMCDay(DateTime date)
        {
            return FOMCDates.Contains(date.Date);
        }
        
        /// <summary>
        /// Checks if the given date falls within an FOMC week
        /// </summary>
        public static bool IsFOMCWeek(DateTime date)
        {
            DateTime startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek);
            for (int i = 0; i < 7; i++)
            {
                if (FOMCDates.Contains(startOfWeek.AddDays(i)))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets the next FOMC date after the given date
        /// </summary>
        public static DateTime? GetNextFOMCDate(DateTime afterDate)
        {
            DateTime? nextDate = null;
            foreach (var fomcDate in FOMCDates)
            {
                if (fomcDate > afterDate.Date)
                {
                    if (!nextDate.HasValue || fomcDate < nextDate.Value)
                        nextDate = fomcDate;
                }
            }
            return nextDate;
        }
    }
}
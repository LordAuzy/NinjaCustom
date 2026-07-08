using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NinjaTrader.Custom.DAustin.Common
{
    /// <summary>
    /// Extension methods for enum types
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the Display Name attribute value from an enum value.
        /// Returns the enum's ToString() if no Display attribute is found.
        /// </summary>
        /// <param name="enumValue">The enum value</param>
        /// <returns>The Display Name or the enum value as string</returns>
        public static string GetDisplayName(this Enum enumValue)
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var displayAttribute = fieldInfo?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? enumValue.ToString();
        }
    }
}

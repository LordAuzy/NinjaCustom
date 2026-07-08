using System;

namespace NinjaTrader.Custom.DAustin.Common
{
    // Decorate any strategy component (e.g., IEntryConditionsEvaluator) with this attribute
    // to give it a discoverable Id without requiring instantiation.
    //
    // Usage:
    //   [StrategyComponentId("ECE-921")]
    //   public class EntryConditionsEvaluator921 : IEntryConditionsEvaluator { ... }
    //
    // Query without an instance:
    //   string id = StrategyComponentIdAttribute.GetId(typeof(EntryConditionsEvaluator921));
    //   string id = StrategyComponentIdAttribute.GetId<EntryConditionsEvaluator921>();

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class StrategyComponentIdAttribute : Attribute
    {
        #region Properties
        public string Id { get; }
        #endregion

        #region Constructors
        public StrategyComponentIdAttribute(string id)
        {
            Id = id;
        }
        #endregion

        #region StaticHelpers
        // Get the Id from a Type — returns null if attribute is not present
        public static string GetId(Type type)
        {
            StrategyComponentIdAttribute attr =
                (StrategyComponentIdAttribute)Attribute.GetCustomAttribute(type, typeof(StrategyComponentIdAttribute));
            return attr?.Id;
        }

        // Generic convenience overload — no instance needed
        public static string GetId<T>() where T : class
        {
            return GetId(typeof(T));
        }
        #endregion
    }
}
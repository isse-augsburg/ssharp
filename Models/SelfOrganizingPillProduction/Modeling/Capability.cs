using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public abstract class Capability
    {
        public static bool IsSatisfiable(IEnumerable<Capability> required, IEnumerable<Capability> available)
        {
            if (required.OfType<ProduceCapability>().Any() && !available.OfType<ProduceCapability>().Any())
                return false;
            if (required.OfType<ConsumeCapability>().Any() && !available.OfType<ConsumeCapability>().Any())
                return false;

            var requiredAmounts = GroupIngredientAmounts(required);
            var availableAmounts = GroupIngredientAmounts(available);

            foreach (IngredientType type in Enum.GetValues(typeof(IngredientType)))
                if (requiredAmounts.ContainsKey(type)
                    && (!availableAmounts.ContainsKey(type) || availableAmounts[type] < requiredAmounts[type]))
                    return false;

            return true;
        }

        private static Dictionary<IngredientType, int> GroupIngredientAmounts(IEnumerable<Capability> capabilities)
        {
            return capabilities.OfType<Ingredient>()
                .GroupBy(ingredient => ingredient.Type, ingredient => (int)ingredient.Amount)
                .ToDictionary(group => group.Key, group => group.Sum());
        }
    }

    /// <summary>
    /// Represents the loading of empty pill containers on the conveyor belt.
    /// </summary>
    public sealed class ProduceCapability : Capability
    {
        public override bool Equals(object obj)
        {
            return obj is ProduceCapability;
        }

        public override int GetHashCode()
        {
            return 17;
        }
    }

    /// <summary>
    /// Represents the removal of pill containers from the conveyor belt, labeling and palletization.
    /// </summary>
    public sealed class ConsumeCapability : Capability
    {
        public override bool Equals(object obj)
        {
            return obj is ConsumeCapability;
        }

        public override int GetHashCode()
        {
            return 31;
        }
    }

    /// <summary>
    /// Represents the addition of a specified amount of a certain ingredient to the container.
    /// </summary>
    public class Ingredient : Capability
    {
        public Ingredient(IngredientType type, uint amount)
        {
            Type = type;
            Amount = amount;
        }

        public IngredientType Type { get; }

        public uint Amount { get; }

        public override bool Equals(object obj)
        {
            var other = obj as Ingredient;
            if (other != null)
                return other.Type == Type && other.Amount == Amount;
            return false;
        }

        public override int GetHashCode()
        {
            return (int)Type + 57 * (int)Amount;
        }
    }
}

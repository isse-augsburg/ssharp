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
    public class ProduceCapability : Capability
    {
    }

    /// <summary>
    /// Represents the removal of pill containers from the conveyor belt, labeling and palletization.
    /// </summary>
    public class ConsumeCapability : Capability
    {
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
    }
}

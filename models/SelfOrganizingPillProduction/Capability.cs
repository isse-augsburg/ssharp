using System.Linq;

namespace SelfOrganizingPillProduction
{
    public abstract class Capability
    {
        public abstract bool IsSatisfied(Capability[] availableCapabilities);
    }

    /// <summary>
    /// Represents the loading of empty pill containers on the conveyor belt.
    /// </summary>
    public class ProduceCapability : Capability
    {
        private ProduceCapability() { }

        public static readonly ProduceCapability Instance = new ProduceCapability();

        public override bool IsSatisfied(Capability[] availableCapabilities) => availableCapabilities.Contains(Instance);
    }

    /// <summary>
    /// Represents the removal of pill containers from the conveyor belt, labeling and palletization.
    /// </summary>
    public class ConsumeCapability : Capability
    {
        private ConsumeCapability() { }

        public static readonly ConsumeCapability Instance = new ConsumeCapability();

        public override bool IsSatisfied(Capability[] availableCapabilities) => availableCapabilities.Contains(Instance);
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

        public override bool IsSatisfied(Capability[] availableCapabilities)
        {
            return availableCapabilities
                .OfType<Ingredient>()
                .Where(ingredient => ingredient.Type == Type)
                .Any(ingredient => ingredient.Amount >= Amount);
        }
    }
}

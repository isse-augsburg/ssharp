using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
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

        // Singleton pattern is of limited use, since there will still be multiple
        // instances due to serialization and deserialization.
        public static readonly ProduceCapability Instance = new ProduceCapability();

        public override bool IsSatisfied(Capability[] availableCapabilities)
            => availableCapabilities.Any(cap => cap is ProduceCapability);
    }

    /// <summary>
    /// Represents the removal of pill containers from the conveyor belt, labeling and palletization.
    /// </summary>
    public class ConsumeCapability : Capability
    {
        private ConsumeCapability() { }

        // Singleton pattern is of limited use, since there will still be multiple
        // instances due to serialization and deserialization.
        public static readonly ConsumeCapability Instance = new ConsumeCapability();

        public override bool IsSatisfied(Capability[] availableCapabilities)
            => availableCapabilities.Any(cap => cap is ConsumeCapability);
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

        public uint Amount { get; internal set; }

        public override bool IsSatisfied(Capability[] availableCapabilities)
            => availableCapabilities.OfType<Ingredient>()
                .Any(ingredient => ingredient.Type == Type && ingredient.Amount >= Amount);
    }
}

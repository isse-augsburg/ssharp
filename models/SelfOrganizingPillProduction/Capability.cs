namespace SelfOrganizingPillProduction
{
    abstract class Capability
    {
    }

    /// <summary>
    /// Represents the loading of empty pill containers on the conveyor belt.
    /// </summary>
    class ProduceCapability : Capability
    {
        private ProduceCapability() { }

        public static readonly ProduceCapability Instance = new ProduceCapability();
    }

    /// <summary>
    /// Represents the removal of pill containers from the conveyor belt, labeling and palletization.
    /// </summary>
    class ConsumeCapability : Capability
    {
        private ConsumeCapability() { }

        public static readonly ConsumeCapability Instance = new ConsumeCapability();
    }

    /// <summary>
    /// Represents the addition of a specified amount of a certain ingredient to the container.
    /// </summary>
    class Ingredient : Capability
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

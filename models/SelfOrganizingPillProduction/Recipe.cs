using System.Collections.Generic;
using System.Linq;

namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// Describes how a container should be processed.
    /// </summary>
    class Recipe
    {
        /// <summary>
        /// Creates a new recipe with the specified sequence of ingredients.
        /// </summary>
        /// <param name="ingredients">The sequence of ingredients to add to the containers.</param>
        /// <param name="amount">The number of containers to produce for this recipe.</param>
        public Recipe(Ingredient[] ingredients, uint amount)
        {
            ActiveContainers = new List<PillContainer>((int)amount);
            Amount = amount;
            RequiredCapabilities = new Capability[] { ProduceCapability.Instance }
                .Concat(ingredients)
                .Concat(new[] { ConsumeCapability.Instance })
                .ToArray();
        }

        /// <summary>
        /// A list of all containers processed according to this recipe that are currently in the system.
        /// </summary>
        public List<PillContainer> ActiveContainers { get; }

        /// <summary>
        /// The sequence of capabilities defining this recipe.
        /// </summary>
        public Capability[] RequiredCapabilities { get;  }

        /// <summary>
        /// The total number of containers to be produced for this recipe.
        /// </summary>
        public uint Amount { get; }
    }
}

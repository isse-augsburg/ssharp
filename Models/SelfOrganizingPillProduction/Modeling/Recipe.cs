using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// Describes how a container should be processed.
    /// </summary>
    public class Recipe
    {
        /// <summary>
        /// Creates a new recipe with the specified sequence of ingredients.
        /// </summary>
        /// <param name="ingredients">The sequence of ingredients to add to the containers.</param>
        /// <param name="amount">The number of containers to produce for this recipe.</param>
        public Recipe(Ingredient[] ingredients, uint amount)
        {
            activeContainers = new List<PillContainer>((int)amount);
            Amount = amount;

            RequiredCapabilities = new Capability[] { ProduceCapability.Instance }
                .Concat(ingredients)
                .Concat(new[] { ConsumeCapability.Instance })
                .ToArray();
        }

        private uint producedAmount = 0u;

        private readonly List<PillContainer> activeContainers;

        /// <summary>
        /// Adds a <paramref name="container"/> to the recipe's active containers.
        /// This is called when processing of the container starts.
        /// </summary>
        public void AddContainer(PillContainer container)
        {
            producedAmount++;
            activeContainers.Add(container);
        }

        /// <summary>
        /// Notifies the recipe that the given <paramref name="container"/> was
        /// completely processed and has left the production system.
        /// </summary>
        public void RemoveContainer(PillContainer container)
        {
            activeContainers.Remove(container);
        }

        /// <summary>
        /// True if the specified <see cref="Amount"/> of containers was produced
        /// and completely processed for the recipe.
        /// </summary>
        public bool ProcessingComplete => activeContainers.Count == 0 && producedAmount == Amount;

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

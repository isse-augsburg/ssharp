using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A pill container which is filled with different ingredients.
    /// </summary>
    public class PillContainer : Component
    {
        private Recipe recipe = null;

        /// <summary>
        /// The recipe according to which the container is processed.
        /// </summary>
        public Recipe Recipe => recipe;

        /// <summary>
        /// The capabilities already applied to the container.
        /// </summary>
        public IEnumerable<Capability> State =>
            Recipe?.RequiredCapabilities.Take(statePrefixLength) ?? Enumerable.Empty<Capability>();

        /// <summary>
        ///  How many of the <see cref="Recipe"/>'s <see cref="Recipe.RequiredCapabilities"/>
        ///  were already applied to this container.
        /// </summary>
        private int statePrefixLength = 0;

        /// <summary>
        /// Tells the container it was loaded on the conveyor belt.
        /// </summary>
        /// <param name="recipe">The recipe according to which it will henceforth be processed.</param>
        public void OnLoaded(Recipe recipe)
        {
            if (this.recipe != null)
                throw new InvalidOperationException("Container already belongs to a recipe");
            this.recipe = recipe;
            statePrefixLength++; // first capability will always be ProduceCapability
        }

        /// <summary>
        /// Adds an ingredient to the container.
        /// </summary>
        /// <param name="ingredient"></param>
        public void AddIngredient(Ingredient ingredient)
        {
            if (statePrefixLength >= Recipe.RequiredCapabilities.Length)
                throw new InvalidOperationException("PillContainer is already fully processed.");
            if (!ingredient.Equals(Recipe.RequiredCapabilities[statePrefixLength]))
                throw new InvalidOperationException("Added the wrong ingredient to PillContainer.");

            statePrefixLength++;
        }
    }
}

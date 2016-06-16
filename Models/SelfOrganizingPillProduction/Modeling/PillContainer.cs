using System;
using System.Collections.Generic;
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
        [Hidden(HideElements = true)]
        public List<Capability> State { get; } = new List<Capability>(Model.MaximumRecipeLength);

        /// <summary>
        /// Tells the container it was loaded on the conveyor belt.
        /// </summary>
        /// <param name="recipe">The recipe according to which it will henceforth be processed.</param>
        public void OnLoaded(Recipe recipe)
        {
            if (this.recipe != null)
                throw new InvalidOperationException("Container already belongs to a recipe");
            this.recipe = recipe;
            State.Add(recipe.RequiredCapabilities[0]); // will always be ProduceCapability
        }

        /// <summary>
        /// Adds an ingredient to the container.
        /// </summary>
        /// <param name="ingredient"></param>
        public void AddIngredient(Ingredient ingredient)
        {
            State.Add(ingredient);
        }
    }
}

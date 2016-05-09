using System.Collections.Generic;
using SafetySharp.Modeling;

namespace SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A pill container which is filled with different ingredients.
    /// </summary>
    public class PillContainer : Component
    {
        public PillContainer(Recipe recipe)
        {
            Recipe = recipe;
            State = new List<Capability>(recipe.RequiredCapabilities.Length);
        }

        /// <summary>
        /// The recipe according to which the container is processed.
        /// </summary>
        public Recipe Recipe { get; }

        /// <summary>
        /// The capabilities already applied to the container.
        /// </summary>
        public List<Capability> State { get; }

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

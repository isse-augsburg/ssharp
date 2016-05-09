using System;
using System.Collections.Generic;
using System.Linq;

namespace SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that adds ingredients to the containers.
    /// </summary>
    public class ParticulateDispenser : Station
    {
        private readonly Dictionary<IngredientType, uint> availableIngredients = new Dictionary<IngredientType, uint>();

        public override Capability[] AvailableCapabilities
        {
            get
            {
                return availableIngredients.Select(kv => new Ingredient(type: kv.Key, amount: kv.Value)).ToArray();
            }
        }

        protected override void ExecuteRole(Role role)
        {
            foreach (var capability in role.CapabilitiesToApply)
            {
                var ingredient = capability as Ingredient;
                if (ingredient == null)
                    throw new InvalidOperationException($"Invalid capability in ParticulateDispenser: {capability}");
                if (!availableIngredients.ContainsKey(ingredient.Type) || availableIngredients[ingredient.Type] < ingredient.Amount)
                    throw new InvalidOperationException($"Insufficient amount available of ingredient {ingredient.Type}");

                availableIngredients[ingredient.Type] -= ingredient.Amount;
                Container.AddIngredient(ingredient);
            }
        }
    }
}

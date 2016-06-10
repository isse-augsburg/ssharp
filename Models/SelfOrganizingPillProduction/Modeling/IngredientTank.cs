using System;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public partial class ParticulateDispenser
    {
        private class IngredientTank : Component
        {
            public readonly Fault TankDepleted = new PermanentFault();

            public IngredientTank(string stationName, IngredientType ingredientType)
            {
                IngredientType = ingredientType;
                FaultHelper.PrefixFaultNames(this, $"{stationName}.{ingredientType}");
            }

            public IngredientType IngredientType { get; }

            public virtual uint Amount { get; set; } = 0u;

            public Ingredient Capability => new Ingredient(IngredientType, Amount);

            public virtual void Dispense(PillContainer container, Ingredient ingredient)
            {
                if (ingredient.Type != IngredientType)
                    throw new InvalidOperationException("Incorrect ingredient requested");
                if (Amount < ingredient.Amount)
                    throw new InvalidOperationException($"Insufficient amount available of ingredient {ingredient.Type}");

                Amount -= ingredient.Amount;
                container.AddIngredient(ingredient);
            }

            [FaultEffect(Fault = nameof(TankDepleted))]
            class TankDepletedEffect : IngredientTank
            {
                public TankDepletedEffect(string stationName, IngredientType ingredientType)
                    : base(stationName, ingredientType) { }

                public override uint Amount => 0u;
            }
        }
    }
}

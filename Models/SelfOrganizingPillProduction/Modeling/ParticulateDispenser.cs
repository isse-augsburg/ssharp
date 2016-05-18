using System;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that adds ingredients to the containers.
    /// </summary>
    public partial class ParticulateDispenser : Station
    {
        public readonly Fault DispenserDefect = new PermanentFault();

        public ParticulateDispenser() : base()
        {
            DispenserDefect.Name = $"{name}.DispenserDefect";
        }

        public readonly IngredientStorage Storage = new IngredientStorage();

        public override Capability[] AvailableCapabilities => Storage.Capabilities;

        protected override void ExecuteRole(Role role)
        {
            foreach (var capability in role.CapabilitiesToApply)
            {
                var ingredient = capability as Ingredient;
                if (ingredient == null)
                    throw new InvalidOperationException($"Invalid capability in ParticulateDispenser: {capability}");
                if (Storage[ingredient.Type] < ingredient.Amount)
                    throw new InvalidOperationException($"Insufficient amount available of ingredient {ingredient.Type}");

                Storage[ingredient.Type] -= ingredient.Amount;
                Container.AddIngredient(ingredient);
            }
        }

        [FaultEffect(Fault = nameof(DispenserDefect))]
        public class DispenserDefectEffect : ParticulateDispenser
        {
            public override Capability[] AvailableCapabilities => new Capability[0];
        }
    }
}

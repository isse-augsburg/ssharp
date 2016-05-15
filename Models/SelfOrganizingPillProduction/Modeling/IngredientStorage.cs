using System;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public partial class ParticulateDispenser
    {
        public class IngredientStorage
        {
            private readonly Ingredient[] ingredients;

            public IngredientStorage()
            {
                var types = Enum.GetValues(typeof(IngredientType));
                ingredients = new Ingredient[types.Length];

                // Assumes IngredientType enum values are 0..types.Length-1
                foreach (IngredientType type in types)
                {
                    ingredients[(int)type] = new Ingredient(type, 0);
                }
            }

            /// <summary>
            /// Get and sets the currently stored amount of a certain ingredient type.
            /// </summary>
            public uint this[IngredientType type]
            {
                get { return ingredients[(int)type].Amount; }
                set { ingredients[(int)type].Amount = value; }
            }

            /// <summary>
            /// The capabilities representing the stored ingredient amounts.
            /// </summary>
            public Capability[] Capabilities => ingredients;
        }
    }
}

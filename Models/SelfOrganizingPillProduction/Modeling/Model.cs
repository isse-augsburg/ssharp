using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public class Model : ModelBase
    {
        public const int MaximumRecipeLength = 30;
        public const int ContainerStorageSize = 30;
        public const int MaximumRoleCount = 100;
        public const int MaximumResourceCount = 30;
        public const uint InitialIngredientAmount = 100u;

        public Model(Station[] stations, ObserverController obsContr)
        {
            Stations = stations;
            foreach (var station in stations)
            {
                station.ObserverController = obsContr;
            }
            ObserverController = obsContr;
        }

        [Root(RootKind.Controller)]
        public Station[] Stations { get; }

        [Root(RootKind.Controller)]
        public ObserverController ObserverController { get; }

        public void ScheduleProduction(Recipe recipe)
        {
            ObserverController.ScheduleConfiguration(recipe);
        }

        public static Model NoRedundancyCircularModel()
        {
            // create 3 stations
            var dispenser = new ParticulateDispenser();
            var stations = new Station[]
            {
                new ContainerLoader(),
                dispenser,
                new PalletisationStation()
            };

            dispenser.SetStoredAmount(IngredientType.BlueParticulate, 50u);
            dispenser.SetStoredAmount(IngredientType.RedParticulate, 50u);
            dispenser.SetStoredAmount(IngredientType.YellowParticulate, 50u);

            // connect them to a circle
            for (int i = 0; i < stations.Length; ++i)
            {
                var next = stations[(i + 1) % stations.Length];
                stations[i].Outputs.Add(next);
                next.Inputs.Add(stations[i]);
            }

            var model = new Model(stations, new FastObserverController(stations));

            var recipe = new Recipe(ingredients: new[] {
                new Ingredient(IngredientType.BlueParticulate, 12),
                new Ingredient(IngredientType.RedParticulate, 4),
                new Ingredient(IngredientType.YellowParticulate, 5)
            }, amount: 3);
            model.ScheduleProduction(recipe);

            return model;
        }
    }
}

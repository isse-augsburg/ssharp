using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public class Model : ModelBase
    {
        public const int MaximumRecipeLength = 30;
        public const int ContainerStorageSize = 30;
        public const int MaximumRoleCount = 30;
        public const int MaximumResourceCount = 30;

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

        public static Model NoRedundancyCircularModel()
        {
            // create 3 stations
            var producer = new ContainerLoader();
            var dispenser = new ParticulateDispenser();
            var stations = new Station[]
            {
                producer,
                dispenser,
                new PalletisationStation()
            };

            dispenser.Storage[IngredientType.BlueParticulate] = 50;
            dispenser.Storage[IngredientType.RedParticulate] = 50;
            dispenser.Storage[IngredientType.YellowParticulate] = 50;

            // connect them to a circle
            for (int i = 0; i < stations.Length; ++i)
            {
                var next = stations[(i + 1) % stations.Length];
                stations[i].Outputs.Add(next);
                next.Inputs.Add(stations[i]);
            }

            var recipe = new Recipe(new[] {
                new Ingredient(IngredientType.BlueParticulate, 12),
                new Ingredient(IngredientType.RedParticulate, 4),
                new Ingredient(IngredientType.YellowParticulate, 5)
            }, 3);
            producer.AcceptProductionRequest(recipe);

            return new Model(stations, new MiniZincObserverController(stations));
        }
    }
}

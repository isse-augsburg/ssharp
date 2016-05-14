using SafetySharp.Analysis;
using SafetySharp.Modeling;

namespace SelfOrganizingPillProduction.Modeling
{
    public class Model : ModelBase
    {
        public const int MaximumRecipeLength = 30;
        public const int ContainerStorageSize = 30;
        public const int MaximumRoleCount = 30;
        public const int MaximumResourceCount = 30;

        public Model(Station[] stations)
        {
            Stations = stations;
            foreach (var station in stations)
            {
                station.Model = this;
            }
        }

        [Root(RootKind.Controller)]
        public Station[] Stations { get; }

        public static Model NoRedundancyCircularModel()
        {
            // create 3 stations
            var stations = new Station[]
            {
                new ContainerLoader(),
                new ParticulateDispenser(),
                new PalletisationStation()
            };

            // connect them to a circle
            for (int i = 0; i < stations.Length; ++i)
            {
                var next = stations[(i + 1) % stations.Length];
                stations[i].Outputs.Add(next);
                next.Inputs.Add(stations[i]);
            }

            return new Model(stations);
        }
    }
}

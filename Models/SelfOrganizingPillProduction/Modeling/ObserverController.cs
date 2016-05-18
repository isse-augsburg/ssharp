using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public abstract class ObserverController : Component
    {
        [Hidden(HideElements = true)]
        protected readonly Station[] stations;

        protected Station[] AvailableStations => stations.Where(s => s.IsAlive).ToArray();

        public bool Unsatisfiable { get; protected set; }

        public readonly ObjectPool<Role> RolePool = new ObjectPool<Role>(Model.MaximumRoleCount);

        public ObserverController(params Station[] stations)
        {
            this.stations = stations;
        }

        private readonly Queue<Recipe> scheduledRecipes = new Queue<Recipe>();

        public void ScheduleConfiguration(Recipe recipe)
        {
            scheduledRecipes.Enqueue(recipe);
        }

        public override void Update()
        {
            if (scheduledRecipes.Count > 0)
                Configure(scheduledRecipes.Dequeue());
        }

        public abstract void Configure(Recipe recipe);
    }
}

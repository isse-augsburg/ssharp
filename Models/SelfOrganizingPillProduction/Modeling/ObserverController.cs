using System.Collections.Generic;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public abstract class ObserverController : Component
    {
        [Hidden(HideElements = true)]
        protected readonly Station[] stations;

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

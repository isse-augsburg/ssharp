using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    public abstract class ObserverController : Component
    {
        /// <summary>
        /// The list of all stations controlled by this instance.
        /// </summary>
        [Hidden(HideElements = true)]
        protected readonly Station[] stations;

        /// <summary>
        /// The list of currently functioning stations controlled by this instance.
        /// </summary>
        protected Station[] AvailableStations => stations.Where(s => s.IsAlive).ToArray();

        /// <summary>
        /// Set to true if a (re-)configuration is not possible.
        /// </summary>
        public bool Unsatisfiable { get; protected set; }

        /// <summary>
        /// Since S# doesn't allow creation of new <see cref="Role"/> instances during
        /// model checking, they are collected here and reused.
        /// </summary>
        public readonly ObjectPool<Role> RolePool = new ObjectPool<Role>(Model.MaximumRoleCount);

        /// <summary>
        /// Create a new instance controlling the given <see cref="stations"/>.
        /// </summary>
        public ObserverController(params Station[] stations)
        {
            this.stations = stations;
        }

        // recipes scheduled for initial configuration
        private readonly Queue<Recipe> scheduledRecipes = new Queue<Recipe>();

        /// <summary>
        /// Schedules a <paramref name="recipe"/> for initial configuration once simulation
        /// or model checking starts. Typically called during model setup.
        /// </summary>
        public void ScheduleConfiguration(Recipe recipe)
        {
            scheduledRecipes.Enqueue(recipe);
        }

        public override void Update()
        {
            if (scheduledRecipes.Count > 0)
                Configure(scheduledRecipes.Dequeue());
        }

        /// <summary>
        /// (Re-)Configures a <paramref name="recipe"/>.
        /// </summary>
        public abstract void Configure(Recipe recipe);

        /// <summary>
        /// Removes all configuration for the given <paramref name="recipe"/> from all stations
        /// under this instance's control.
        /// </summary>
        protected void RemoveObsoleteConfiguration(Recipe recipe)
        {
            foreach (var station in AvailableStations)
            {
                var obsoleteRoles = (from role in station.AllocatedRoles where role.Recipe == recipe select role)
                    .ToArray();
                foreach (var role in obsoleteRoles)
                {
                    station.AllocatedRoles.Remove(role);
                }
                RolePool.Return(obsoleteRoles);
            }
        }

        /// <summary>
        /// Get a fresh role from the <see cref="RolePool"/>.
        /// </summary>
        /// <param name="recipe">The recipe the role will belong to.</param>
        /// <param name="input">The station the role declares as input port. May be null.</param>
        /// <param name="state">The state of a resource before it is processed by the role. May be null.</param>
        /// <returns>A <see cref="Role"/> instance with the given data.</returns>
        protected Role GetRole(Recipe recipe, Station input, IEnumerable<Capability> state)
        {
            var role = RolePool.Allocate();

            role.CapabilitiesToApply.Clear();

            // update precondition
            role.PreCondition.Recipe = recipe;
            role.PreCondition.Port = input;
            role.PreCondition.State.Clear();
            if (state != null)
                role.PreCondition.State.AddRange(state);

            // update postcondition
            role.PostCondition.Recipe = recipe;
            role.PostCondition.Port = null;
            role.PostCondition.State.Clear();
            role.PostCondition.State.AddRange(role.PreCondition.State);

            return role;
        }
    }
}

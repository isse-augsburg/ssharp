using SafetySharp.Modeling;

namespace SelfOrganizingPillProduction.Modeling
{
    public abstract class ObserverController : Component
    {
        protected readonly Station[] stations;

        public bool Unsatisfiable { get; protected set; }

        public readonly ObjectPool<Role> RolePool = new ObjectPool<Role>(Model.MaximumRoleCount);

        public ObserverController(params Station[] stations)
        {
            this.stations = stations;
        }

        public abstract void Configure(Recipe recipe);
    }
}

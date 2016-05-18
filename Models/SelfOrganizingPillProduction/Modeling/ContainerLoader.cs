using SafetySharp.Modeling;
using System;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that loads containers on the conveyor belt.
    /// </summary>
    public class ContainerLoader : Station
    {
        public readonly Fault NoContainersLeft = new PermanentFault();

        private static readonly Capability[] produceCapabilities = new[] { new ProduceCapability() };
        private static readonly Capability[] emptyCapabilities = new Capability[0];

        public override Capability[] AvailableCapabilities =>
            containerStorage.Count > 0 ? produceCapabilities : emptyCapabilities;

        private readonly ObjectPool<PillContainer> containerStorage = new ObjectPool<PillContainer>(Model.ContainerStorageSize);

        protected override void ExecuteRole(Role role)
        {
            // This is only called if the current Container comes from another station.
            // The only valid role is thus an empty one (no CapabilitiesToApply) and represents
            // a simple forwarding to the next station.
            if (role.CapabilitiesToApply.Count > 0)
                throw new InvalidOperationException("Unsupported capability configuration in ContainerLoader");
        }

        public override void Update()
        {
            // Handle resource requests if any. This is required to allow forwarding
            // of containers to the next station.
            base.Update();

            // No accepted resource requests and no previous resource,
            // so produce resources instead.
            var role = ChooseProductionRole();
            if (Container == null && role != null)
            {
                var recipe = role.Recipe;

                // role.capabilitiesToApply will always be { ProduceCapability }
                Container = containerStorage.Allocate();
                Container.OnLoaded(recipe);
                recipe.AddContainer(Container);

                // assume role.PostCondition.Port != null
                role.PostCondition.Port.ResourceReady(source: this, condition: role.PostCondition);
            }
        }

        private Role ChooseProductionRole()
        {
            return AllocatedRoles.FirstOrDefault(role =>
                role.PreCondition.Port == null && role.Recipe.RemainingAmount > 0
            );
        }

        [FaultEffect(Fault = nameof(NoContainersLeft))]
        public class NoContainersLeftEffect : ContainerLoader
        {
            public override Capability[] AvailableCapabilities => emptyCapabilities;
        }
    }
}

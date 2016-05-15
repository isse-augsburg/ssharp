using SafetySharp.Modeling;
using System;
using System.Collections.Generic;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that loads containers on the conveyor belt.
    /// </summary>
    public class ContainerLoader : Station
    {
        private static readonly Capability[] produceCapabilities = new[] { new ProduceCapability() };
        private static readonly Capability[] emptyCapabilities = new Capability[0];

        public override Capability[] AvailableCapabilities =>
            containerStorage.Count > 0 ? produceCapabilities : emptyCapabilities;

        private readonly ObjectPool<PillContainer> containerStorage = new ObjectPool<PillContainer>(Model.ContainerStorageSize);

        // Elements are only added during setup. During runtime, elements are only removed.
        private readonly List<ProductionRequest> productionRequests = new List<ProductionRequest>();

        protected override void ExecuteRole(Role role)
        {
            // This is only called if the current Container comes from another station.
            // The only valid role is thus an empty one (no CapabilitiesToApply) and represents
            // a simple forwarding to the next station.
            if (role.CapabilitiesToApply.Count > 0)
                throw new InvalidOperationException("Unsupported capability configuration in ContainerLoader");
        }

        /// <summary>
        /// Starts production of containers according to the given <paramref name="recipe"/>.
        /// </summary>
        public void AcceptProductionRequest(Recipe recipe)
        {
            productionRequests.Add(new ProductionRequest(recipe));
        }

        public override void Update()
        {
            // Handle resource requests if any. This is required to allow forwarding
            // of containers to the next station.
            base.Update();

            // No accepted resource requests and no previous resource,
            // so produce resources instead.
            if (Container == null && productionRequests.Count > 0)
            {
                var request = productionRequests[0];
                var recipe = request.Recipe;

                if (!request.IsConfigured)
                {
                    ObserverController.Configure(recipe);
                    request.IsConfigured = true;
                }

                var role = ChooseRole(source: null, condition: request.InitialCondition);

                // role.capabilitiesToApply will always be { ProduceCapability }
                Container = containerStorage.Allocate();
                Container.OnLoaded(recipe);
                recipe.AddContainer(Container);

                // assume role.PostCondition.Port != null
                role.PostCondition.Port.ResourceReady(source: this, condition: role.PostCondition);

                request.RemainingAmount--; // update count
                if (request.RemainingAmount <= 0) // request was completed
                {
                    productionRequests.Remove(request);
                }
            }
        }

        private class ProductionRequest
        {
            public ProductionRequest(Recipe recipe)
            {
                Recipe = recipe;
                RemainingAmount = recipe.Amount;

                // necessary here because it cannot be created later during model checking
                InitialCondition = new Condition { Recipe = recipe, Port = null };
            }

            public Recipe Recipe { get; }

            public uint RemainingAmount { get; set; }

            public Condition InitialCondition { get; }

            public bool IsConfigured { get; set; }
        }
    }
}

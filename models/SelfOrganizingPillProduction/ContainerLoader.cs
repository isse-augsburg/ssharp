using System;
using System.Collections.Generic;
using System.Linq;

namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// A production station that loads containers on the conveyor belt.
    /// </summary>
    class ContainerLoader : Station
    {
        public override Capability[] AvailableCapabilities { get; } = new[] { ProduceCapability.Instance };

        private readonly List<Tuple<Recipe, uint>> productionRequests = new List<Tuple<Recipe, uint>>(); // TODO: initial capacity

        protected override void ExecuteRole(Role role)
        {
            // This is only called if the current Container comes from another station.
            // The only valid role is thus an empty one (no CapabilitiesToApply) and represents
            // a simple forwarding to the next station.
            if (role.CapabilitiesToApply.Length > 0)
                throw new InvalidOperationException("Unsupported capability configuration in ContainerLoader");
        }

        /// <summary>
        /// Starts production of containers according to the given <paramref name="recipe"/>.
        /// </summary>
        public void AcceptProductionRequest(Recipe recipe)
        {
            productionRequests.Add(new Tuple<Recipe, uint>(recipe, recipe.Amount));
            // TODO: start configuration
            throw new NotImplementedException();
        }

        public override void Update()
        {
            // Handle resource requests if any. This is required to allow forwarding of containers to the next station.
            base.Update();

            // No accepted resource requests, so produce resources instead.
            var request = ChooseProductionRequest();
            if (Container == null && request != null)
            {
                var recipe = request.Item1;
                var remainingAmount = request.Item2;

                var role = ChooseRole(new Condition { Recipe = recipe, State = new Capability[0] });

                // role.capabilitiesToApply will always be { ProduceCapability }
                Container = new PillContainer(recipe);
                recipe.ActiveContainers.Add(Container);

                // assume role.PostCondition.Port != null
                role.PostCondition.Port.ResourceReady(source: this, condition: role.PostCondition);

                remainingAmount--;
                if (remainingAmount > 0) // update count
                {
                    // TODO: improve performance (no index search / use mutable class instead of Tuple)
                    productionRequests[productionRequests.IndexOf(request)] = new Tuple<Recipe, uint>(recipe, remainingAmount);
                }
                else // request was completed
                {
                    productionRequests.Remove(request);
                }
            }
        }

        private Tuple<Recipe, uint> ChooseProductionRequest()
        {
            // prioritization & filtering possible here
            return (from request in productionRequests
                    let recipe = request.Item1
                    where !lockedRecipes.Contains(recipe)
                    select request)
                .FirstOrDefault();
        }
    }
}

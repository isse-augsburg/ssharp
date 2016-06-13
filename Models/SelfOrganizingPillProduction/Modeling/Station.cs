using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that modifies containers.
    /// </summary>
    public abstract class Station : Component
    {
        public readonly Fault CompleteStationFailure = new PermanentFault();

        /// <summary>
        /// The resource currently located at the station.
        /// </summary>
        public PillContainer Container { get; protected set; }

        /// <summary>
        /// The list of station that can send containers.
        /// </summary>
        [Hidden(HideElements = true)]
        public List<Station> Inputs { get; } = new List<Station>();

        /// <summary>
        /// The list of stations processed containers can be sent to.
        /// </summary>
        [Hidden(HideElements = true)]
        public List<Station> Outputs { get; } = new List<Station>();

        /// <summary>
        /// The roles the station must apply to containers.
        /// </summary>
        public List<Role> AllocatedRoles { get; } = new List<Role>(Model.MaximumRoleCount);

        /// <summary>
        /// The capabilities the station has.
        /// </summary>
        public abstract Capability[] AvailableCapabilities { get; }

        public virtual bool IsAlive => true;

        [Hidden]
        internal ObserverController ObserverController { get; set; }

        private readonly List<ResourceRequest> resourceRequests = new List<ResourceRequest>(Model.MaximumResourceCount);

        private static int instanceCounter = 0;
        protected readonly string name;

        public Station()
        {
            name = $"Station#{++instanceCounter}";
            FaultHelper.PrefixFaultNames(this, name);
        }

        public override void Update()
        {
            CheckConfigurationConsistency();

            // see Fig. 7, How To Design and Implement Self-Organising Resource-Flow Systems (simplified version)
            if (Container == null && resourceRequests.Count > 0)
            {
                var request = resourceRequests[0];
                var role = ChooseRole(request.Source, request.Condition);
                if (role != null)
                {
                    Container = request.Source.TransferResource();
                    resourceRequests.Remove(request);

                    ExecuteRole(role);
                    role.PostCondition.Port?.ResourceReady(source: this, condition: role.PostCondition);
                }
            }
        }

        protected Role ChooseRole(Station source, Condition condition)
        {
            return AllocatedRoles.FirstOrDefault(
                role => role.PreCondition.Matches(condition) && role.PreCondition.Port == source
            ); // there should be at most one such role
        }

        /// <summary>
        /// Informs the station a container is available.
        /// </summary>
        /// <param name="source">The station currently holding the container.</param>
        /// <param name="condition">The container's current condition.</param>
        public void ResourceReady(Station source, Condition condition)
        {
            var request = new ResourceRequest(source, condition);
            if (!resourceRequests.Contains(request))
                resourceRequests.Add(request);
        }

        public void BeforeReconfiguration(Recipe recipe)
        {
            // if the current resource's recipe was reconfigured, drop it from production
            //
            // TODO: try to fix it, i.e. check if the reconfiguration even affected
            // currentRole.PostCondition.Port or removed currentRole, and if so,
            // where to send the resource next
            if (Container != null && Container.Recipe == recipe)
            {
                Container.Recipe.DropContainer(Container);
                Container = null;
            }

            resourceRequests.RemoveAll(request => request.Condition.Recipe == recipe);
        }

        /// <summary>
        /// Instructs the station to hand over its current container to the caller.
        /// </summary>
        /// <returns>The station's current container.</returns>
        public PillContainer TransferResource()
        {
            if (Container == null)
                throw new InvalidOperationException("No container available");
            var resource = Container;
            Container = null;
            return resource;
        }

        /// <summary>
        /// Checks if all required capabilities are available and
        /// the output station is alive, and reconfigures recipes
        /// for which this is not the case.
        /// </summary>
        protected void CheckConfigurationConsistency()
        {
            var inconsistentRecipes = (from role in AllocatedRoles
                                       where !Capability.IsSatisfiable(role.CapabilitiesToApply, AvailableCapabilities)
                                           || !(role.PostCondition.Port?.IsAlive ?? true)
                                       select role.Recipe)
                                   .Distinct()
                                   // in an array, as AllocatedRoles may be modified by reconfiguration below
                                   .ToArray();

            if (inconsistentRecipes.Length > 0)
                ObserverController.Configure(inconsistentRecipes);
        }

        /// <summary>
        /// Removes all configuration related to a recipe and propagates
        /// this change to neighbouring stations.
        /// </summary>
        /// <param name="recipe"></param>
        protected void RemoveRecipeConfigurations(Recipe recipe)
        {
            var obsoleteRoles = (from role in AllocatedRoles where role.Recipe == recipe select role)
                .ToArray(); // collect roles before underlying collection is modified
            var affectedNeighbours = (from role in obsoleteRoles select role.PreCondition.Port)
                .Concat(from role in obsoleteRoles select role.PostCondition.Port)
                .Distinct()
                .Where(neighbour => neighbour != null);

            foreach (var role in obsoleteRoles)
            {
                AllocatedRoles.Remove(role);
            }
            ObserverController.RolePool.Return(obsoleteRoles);

            foreach (var neighbour in affectedNeighbours)
            {
                neighbour.RemoveRecipeConfigurations(recipe);
            }
        }

        /// <summary>
        /// Executes the specified role on the current <see cref="Container"/>.
        /// When this method is called, <see cref="Container"/> must not be null.
        /// </summary>
        protected abstract void ExecuteRole(Role role);

        private struct ResourceRequest
        {
            public ResourceRequest(Station source, Condition condition)
            {
                Source = source;
                Condition = condition;
            }

            public Station Source { get; }
            public Condition Condition { get; }
        }

        // S# seems not to support abstract fault effects,
        // thus this is duplicated in each concrete subclass.
        /*[FaultEffect(Fault = nameof(CompleteStationFailureEffect))]
        public abstract class CompleteStationFailureEffect : Station
        {
            public override bool IsAlive => false;

            public override void Update() { }
        }*/
    }
}

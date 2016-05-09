using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Modeling;

namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// A production station that modifies containers.
    /// </summary>
    abstract class Station : Component
    {
        /// <summary>
        /// The resource currently located at the station.
        /// </summary>
        public PillContainer Container { get; protected set; }

        /// <summary>
        /// The list of station that can send containers.
        /// </summary>
        public List<Station> Inputs { get; } = new List<Station>();

        /// <summary>
        /// The list of stations processed containers can be sent to.
        /// </summary>
        public List<Station> Outputs { get; } = new List<Station>();

        /// <summary>
        /// The roles the station must apply to containers.
        /// </summary>
        protected List<Role> AllocatedRoles { get; } = new List<Role>(); // TODO: initial capacity

        /// <summary>
        /// The capabilities the station has.
        /// </summary>
        public abstract Capability[] AvailableCapabilities { get; }

        private readonly List<Tuple<Station, Condition>> resourceRequests = new List<Tuple<Station, Condition>>(); // TODO: initial capacity
        protected readonly ISet<Recipe> lockedRecipes = new HashSet<Recipe>(); // TODO: initial capacity

        public override void Update()
        {
            CheckCapabilityConsistency();

            // see Fig. 7, How To Design and Implement Self-Organising Resource-Flow Systems (simplified version)
            var request = ChooseResourceRequest();
            if (Container == null && request != null)
            {
                var agent = request.Item1;
                var condition = request.Item2;

                var role = ChooseRole(condition);
                if (role != null)
                {
                    Container = agent.TransferResource();
                    resourceRequests.RemoveAt(0);

                    ExecuteRole(role);
                    role.PostCondition.Port?.ResourceReady(source: this, condition: role.PostCondition);
                }
            }
        }

        protected Tuple<Station, Condition> ChooseResourceRequest()
        {
            // prioritization & filtering possible here
            return (from request in resourceRequests
                    let condition = request.Item2
                    where !lockedRecipes.Contains(condition.Recipe)
                    select request)
                .FirstOrDefault();
        }

        protected Role ChooseRole(Condition preCondition)
        {
            // TODO: deadlock avoidance?
            return (from Role role in AllocatedRoles
                    where role.PreCondition.Recipe == preCondition.Recipe
                        && role.PreCondition.State.SequenceEqual(preCondition.State)
                    select role)
               .FirstOrDefault(); // there should only ever be zero or one
        }

        /// <summary>
        /// Informs the station a container is available.
        /// </summary>
        /// <param name="source">The station currently holding the container.</param>
        /// <param name="condition">The container's current condition.</param>
        public void ResourceReady(Station source, Condition condition)
        {
            resourceRequests.Add(new Tuple<Station, Condition>(source, condition));
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
        /// Checks if all required capabilities are available and locks recipes for which this is not the case.
        /// </summary>
        protected void CheckCapabilityConsistency()
        {
            foreach (var role in AllocatedRoles)
            {
                if (!role.CapabilitiesToApply.All(capability => capability.IsSatisfied(AvailableCapabilities)))
                {
                    lockedRecipes.Add(role.Recipe);
                    // TODO: trigger reconfiguration
                }
            }
        }

        /// <summary>
        /// Executes the specified role on the current <see cref="Container"/>.
        /// When this method is called, <see cref="Container"/> must not be null.
        /// </summary>
        protected abstract void ExecuteRole(Role role);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// Describes a sequence of capabilities a specific station should apply to a container.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// The condition of the container before the role is executed.
        /// </summary>
        public Condition PreCondition { get; } = new Condition();

        /// <summary>
        /// The condition of the container after the role is executed.
        /// </summary>
        public Condition PostCondition { get; } = new Condition();

        /// <summary>
        /// The capabilities to apply.
        /// </summary>
        public IEnumerable<Capability> CapabilitiesToApply =>
            Recipe.RequiredCapabilities.Skip(capabilitiesToApplyStart).Take(capabilitiesToApplyCount);

        private int capabilitiesToApplyStart = 0;
        private int capabilitiesToApplyCount = 0;

        /// <summary>
        /// Resets the capabilities applied by the role (takes <see cref="PreCondition"/> into account).
        /// </summary>
        public void ResetCapabilitiesToApply()
        {
            capabilitiesToApplyStart = PreCondition.State.Count();
            capabilitiesToApplyCount = 0;
        }

        /// <summary>
        /// Returns true if the role contains any capabilities to be applied.
        /// </summary>
        public bool HasCapabilitiesToApply()
        {
            return capabilitiesToApplyCount > 0;
        }

        /// <summary>
        /// Adds the given <paramref name="capability"/> to the role's capabilities to be applied.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the given capability is not the
        /// next required capability.</exception>
        public void AddCapabilityToApply(Capability capability)
        {
            if (capabilitiesToApplyStart + capabilitiesToApplyCount >= Recipe.RequiredCapabilities.Length)
                throw new InvalidOperationException("All required capabilities already applied.");
            if (!capability.Equals(Recipe.RequiredCapabilities[capabilitiesToApplyStart + capabilitiesToApplyCount]))
                throw new InvalidOperationException("Cannot apply capability that is not required.");

            capabilitiesToApplyCount++;
        }

        /// <summary>
        /// The recipe the role belongs to.
        /// </summary>
        public Recipe Recipe => PreCondition.Recipe ?? PostCondition.Recipe;
    }
}

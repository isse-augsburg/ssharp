using System.Collections.Generic;

namespace SelfOrganizingPillProduction.Modeling
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
        public List<Capability> CapabilitiesToApply { get; } = new List<Capability>(Model.MaximumRecipeLength);

        /// <summary>
        /// The recipe the role belongs to.
        /// </summary>
        public Recipe Recipe => PreCondition.Recipe ?? PostCondition.Recipe;
    }
}

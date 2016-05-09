namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// Describes a sequence of capabilities a specific station should apply to a container.
    /// </summary>
    public class Role
    {
        /// <summary>
        /// The condition of the container before the role is executed.
        /// </summary>
        public Condition PreCondition { get; set; }

        /// <summary>
        /// The condition of the container after the role is executed.
        /// </summary>
        public Condition PostCondition { get; set; }

        /// <summary>
        /// The capabilities to apply.
        /// </summary>
        public Capability[] CapabilitiesToApply { get; set; }

        /// <summary>
        /// The recipe the role belongs to.
        /// </summary>
        public Recipe Recipe => (PreCondition ?? PostCondition).Recipe;
    }
}

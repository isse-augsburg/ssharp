namespace SelfOrganizingPillProduction
{
    /// <summary>
    /// Describes the condition of a pill container before or after a role's capabilities are applied to it.
    /// </summary>
    class Condition
    {
        /// <summary>
        /// The station the container is received from or sent to.
        /// </summary>
        public Station Port { get; set; }

        /// <summary>
        /// The capabilities already applied to the container.
        /// </summary>
        public Capability[] State { get; set; }

        /// <summary>
        /// A reference to the container's recipe.
        /// </summary>
        public Recipe Recipe { get; set; }
    }
}

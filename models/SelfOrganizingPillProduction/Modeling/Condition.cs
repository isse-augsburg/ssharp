using System.Linq;

namespace SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// Describes the condition of a pill container before or after a role's capabilities are applied to it.
    /// </summary>
    public class Condition
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

        /// <summary>
        /// Compares two conditions, ignoring the ports
        /// </summary>
        public bool Matches(Condition other)
        {
            return other != null
                && Recipe == other.Recipe
                && State.SequenceEqual(other.State);
        }
    }
}

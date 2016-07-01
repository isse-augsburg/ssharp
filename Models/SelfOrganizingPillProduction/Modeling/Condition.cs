using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// Describes the condition of a pill container before or after a role's capabilities are applied to it.
    /// </summary>
    public struct Condition
    {
        /// <summary>
        /// The station the container is received from or sent to.
        /// </summary>
        public Station Port { get; set; }

        /// <summary>
        /// The capabilities already applied to the container.
        /// </summary>
        public IEnumerable<Capability> State =>
            Recipe?.RequiredCapabilities.Take(statePrefixLength) ?? Enumerable.Empty<Capability>();

        /// <summary>
        ///  How many of the <see cref="Recipe"/>'s <see cref="Recipe.RequiredCapabilities"/>
        ///  were already applied to <see cref="PillContainer"/>s in this condition.
        /// </summary>
        private int statePrefixLength;

        /// <summary>
        /// A reference to the container's recipe.
        /// </summary>
        public Recipe Recipe { get; set; }

        /// <summary>
        /// Resets the condition's <see cref="State"/>.
        /// </summary>
        public void ResetState()
        {
            statePrefixLength = 0;
        }

        /// <summary>
        /// Copies the <see cref="State"/> from another <see cref="Condition"/>.
        /// </summary>
        /// <param name="other">The condition whose state should be copied.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="other"/>
        /// belongs to a different <see cref="Recipe"/>.
        /// </exception>
        public void CopyStateFrom(Condition other)
        {
            if (other.Recipe != Recipe)
                throw new InvalidOperationException();
            statePrefixLength = other.statePrefixLength;
        }

        /// <summary>
        /// Appends the given <see cref="Capability"/> to the <see cref="State"/>.
        /// </summary>
        /// <param name="capability">The capability to append.</param>
        /// <exception cref="InvalidOperationException">Thrown if the capability does not match
        /// the <see cref="Recipe"/>'s next required capability.</exception>
        public void AppendToState(Capability capability)
        {
            if (statePrefixLength >= Recipe.RequiredCapabilities.Length)
                throw new InvalidOperationException("Condition already has maximum state.");
            if (Recipe.RequiredCapabilities[statePrefixLength] != capability)
                throw new InvalidOperationException("Capability must be next required capability.");

            statePrefixLength++;
        }

        /// <summary>
        /// Compares two conditions, ignoring the ports
        /// </summary>
        public bool Matches(Condition other)
        {
            return Recipe == other.Recipe
                && statePrefixLength == other.statePrefixLength;
        }
    }
}

// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.CaseStudies.PillProduction.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	///   Describes the condition of a pill container before or after a role's capabilities are applied to it.
	/// </summary>
	public struct Condition
	{
		/// <summary>
		///   The station the container is received from or sent to.
		/// </summary>
		public Station Port { get; set; }

		/// <summary>
		///   The capabilities already applied to the container.
		/// </summary>
		public IEnumerable<Capability> State =>
			Recipe?.RequiredCapabilities.Take(_statePrefixLength) ?? Enumerable.Empty<Capability>();

		/// <summary>
		///   How many of the <see cref="Recipe" />'s <see cref="Modeling.Recipe.RequiredCapabilities" />
		///   were already applied to <see cref="PillContainer" />s in this condition.
		/// </summary>
		private int _statePrefixLength;

		/// <summary>
		///   A reference to the container's recipe.
		/// </summary>
		public Recipe Recipe { get; set; }

		/// <summary>
		///   Resets the condition's <see cref="State" />.
		/// </summary>
		public void ResetState()
		{
			_statePrefixLength = 0;
		}

		/// <summary>
		///   Copies the <see cref="State" /> from another <see cref="Condition" />.
		/// </summary>
		/// <param name="other">The condition whose state should be copied.</param>
		/// <exception cref="InvalidOperationException">
		///   Thrown if <paramref name="other" />
		///   belongs to a different <see cref="Recipe" />.
		/// </exception>
		public void CopyStateFrom(Condition other)
		{
			if (other.Recipe != Recipe)
				throw new InvalidOperationException();
			_statePrefixLength = other._statePrefixLength;
		}

		/// <summary>
		///   Appends the given <see cref="Capability" /> to the <see cref="State" />.
		/// </summary>
		/// <param name="capability">The capability to append.</param>
		/// <exception cref="InvalidOperationException">
		///   Thrown if the capability does not match
		///   the <see cref="Recipe" />'s next required capability.
		/// </exception>
		public void AppendToState(Capability capability)
		{
			if (_statePrefixLength >= Recipe.RequiredCapabilities.Length)
				throw new InvalidOperationException("Condition already has maximum state.");
			if (Recipe.RequiredCapabilities[_statePrefixLength] != capability)
				throw new InvalidOperationException("Capability must be next required capability.");

			_statePrefixLength++;
		}

		/// <summary>
		///   Compares two conditions, ignoring the ports
		/// </summary>
		public bool Matches(Condition other)
		{
			return Recipe == other.Recipe
				   && _statePrefixLength == other._statePrefixLength;
		}
	}
}
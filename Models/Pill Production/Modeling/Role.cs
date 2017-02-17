// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	///   Describes a sequence of capabilities a specific station should apply to a container.
	/// </summary>
	public struct Role
	{
		/// <summary>
		///   The condition of the container before the role is executed.
		/// </summary>
		public Condition PreCondition;

		/// <summary>
		///   The condition of the container after the role is executed.
		/// </summary>
		public Condition PostCondition;

		/// <summary>
		///   The capabilities to apply.
		/// </summary>
		public IEnumerable<Capability> CapabilitiesToApply =>
			Recipe.RequiredCapabilities.Skip(_capabilitiesToApplyStart).Take(_capabilitiesToApplyCount);

		private int _capabilitiesToApplyStart;
		private int _capabilitiesToApplyCount;

		/// <summary>
		///   Resets the capabilities applied by the role (takes <see cref="PreCondition" /> into account).
		/// </summary>
		public void ResetCapabilitiesToApply()
		{
			_capabilitiesToApplyStart = PreCondition.State.Count();
			_capabilitiesToApplyCount = 0;
		}

		/// <summary>
		///   Returns true if the role contains any capabilities to be applied.
		/// </summary>
		public bool HasCapabilitiesToApply()
		{
			return _capabilitiesToApplyCount > 0;
		}

		/// <summary>
		///   Adds the given <paramref name="capability" /> to the role's capabilities to be applied.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///   Thrown if the given capability is not the
		///   next required capability.
		/// </exception>
		public void AddCapabilityToApply(Capability capability)
		{
			if (_capabilitiesToApplyStart + _capabilitiesToApplyCount >= Recipe.RequiredCapabilities.Length)
				throw new InvalidOperationException("All required capabilities already applied.");
			if (!capability.Equals(Recipe.RequiredCapabilities[_capabilitiesToApplyStart + _capabilitiesToApplyCount]))
				throw new InvalidOperationException("Cannot apply capability that is not required.");

			_capabilitiesToApplyCount++;
		}

		/// <summary>
		///   The recipe the role belongs to.
		/// </summary>
		public Recipe Recipe => PreCondition.Recipe ?? PostCondition.Recipe;
	}
}
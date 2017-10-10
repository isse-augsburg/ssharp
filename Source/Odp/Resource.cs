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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using Modeling;

	/// <summary>
	///  Represents a resource that is being produced by the self-organizing system.
	///  A resource is produced by a <see cref="BaseAgent"/>, and then moves from agent to agent through the system,
	///  while capabilities (see <see cref="ICapability"/>) are applied to it according to an <see cref="ITask"/>.
	///  When the resource is completely processed, it is consumed.
	/// </summary>
	public abstract class Resource : Component
	{
		/// <summary>
		///  The <see cref="ITask"/> according to which the resource is processed.
		/// </summary>
		/// <remarks>For resources in production, this must never by <c>null</c>.</remarks>
		public ITask Task { get; protected set; }

		/// <summary>
		///  The number of capabilities already applied to the resource.
		/// </summary>
		private byte _statePrefixLength = 0;

		/// <summary>
		///  The sequence of capabilities already applied to the resource.
		/// </summary>
		[NotNull]
		public IEnumerable<ICapability> State =>
			Task?.RequiredCapabilities.Take(_statePrefixLength) ?? Enumerable.Empty<ICapability>();

		/// <summary>
		///  Indicates if all capabilities required by the <see cref="Task"/> have already been applied.
		/// </summary>
	    public bool IsComplete => _statePrefixLength == Task.RequiredCapabilities.Length;

		/// <summary>
		///  Informs the resource that the given <paramref name="capability"/> has been applied to it.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if the resource is already complete, or the wrong capability is applied.</exception>
	    public void OnCapabilityApplied([NotNull] ICapability capability)
		{
			if (capability == null)
				throw new ArgumentNullException(nameof(capability));

			if (_statePrefixLength >= Task.RequiredCapabilities.Length)
				throw new InvalidOperationException("resource is already fully processed");
			if (!capability.Equals(Task.RequiredCapabilities[_statePrefixLength]))
				throw new InvalidOperationException("wrong capability applied to resource");

			_statePrefixLength++;
		}
	}
}

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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

	public interface IConfigurationFinder
	{
		/// <summary>
		///   Finds a configuration for the given <paramref name="taskFragment"/> using the given <paramref name="availableAgents"/>.
		/// </summary>
		/// <param name="taskFragment">The task fragment for which a configuration shall be found.</param>
		/// <param name="availableAgents">The agents that may partake in the configuration.</param>
		/// <param name="isProducer">A predicate that identifies which <paramref name="availableAgents"/> may occur as first agent in the resource flow.</param>
		/// <param name="isConsumer">A predicate that identifies which <paramref name="availableAgents"/> may occur as last agent in the resource flow.</param>
		/// <returns>
		///   A struct containing a capability distribution (a sequence of agents with the capabilities in the given <paramref name="taskFragment"/>)
		///   and a matching resource flow (a sequence of connected agents).
		///   Or null, if no configuration is found.
		/// </returns>
		[NotNull]
		Task<Configuration?> Find(TaskFragment taskFragment, [NotNull, ItemNotNull] ISet<BaseAgent> availableAgents, [NotNull] Predicate<BaseAgent> isProducer, [NotNull] Predicate<BaseAgent> isConsumer);
	}

	/// <summary>
	///   Describes a configuration as returned by <see cref="IConfigurationFinder"/>.
	/// </summary>
	public struct Configuration
	{
		[NotNull, ItemNotNull]
		public BaseAgent[] Distribution { get; }

		[NotNull, ItemNotNull]
		public BaseAgent[] ResourceFlow { get; }

		public Configuration([NotNull, ItemNotNull] BaseAgent[] distribution, [NotNull, ItemNotNull] BaseAgent[] resourceFlow)
		{
			if (distribution == null)
				throw new ArgumentNullException(nameof(distribution));
			if (resourceFlow == null)
				throw new ArgumentNullException(nameof(resourceFlow));

#if DEBUG
			if (distribution.Contains(null))
				throw new ArgumentException("Partially defined distribution", nameof(distribution));
			if (resourceFlow.Contains(null))
				throw new ArgumentException("Partially defined resource flow", nameof(resourceFlow));
			for (int i = 0, j = 0; i < distribution.Length; ++i)
			{
				while (j < resourceFlow.Length && resourceFlow[j] != distribution[i])
					j++;
				if (j >= resourceFlow.Length)
					throw new ArgumentException("Resource flow does not match distribution");
			}
#endif

			Distribution = distribution;
			ResourceFlow = resourceFlow;
		}
	}
}

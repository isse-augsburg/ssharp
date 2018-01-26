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
	using System.Linq;

	/// <summary>
	///   A <see cref="IConfigurationFinder"/> that follows the resource flow and returns the solution with the shortest path from source to sink.
	/// </summary>
	public class OptimalConfigurationFinder : FastConfigurationFinder
	{
		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="preferCapabilityAccumulation">Whether or not it is preferred that one agent applies multiple capabilities.</param>
		public OptimalConfigurationFinder(bool preferCapabilityAccumulation)
			: base(preferCapabilityAccumulation)
		{
		}

		protected override int[] FindDistribution(TaskFragment taskFragment, BaseAgent[] availableAgents)
		{
			var emptyPath = new int[taskFragment.Length];
			if (taskFragment.Length == 0)
				return emptyPath;

			var identifiers = Enumerable.Range(0, availableAgents.Length).ToArray();
			var paths = (from agent in identifiers
						 where _precedingProducer[agent] != -1 && CanSatisfyNext(taskFragment, availableAgents, emptyPath, 0, agent)
						 select new[] { agent }).ToArray();

			if (paths.Length == 0)
				return null;

			for (var i = 1; i < taskFragment.Length; ++i)
			{
				paths = (from path in paths
						 from agent in identifiers
						 let last = path[i - 1]
						 // if agent is reachable from the previous path
						 where _pathMatrix[last, agent] != -1
							   // and agent can satisfy the next required capability
							   && CanSatisfyNext(taskFragment, availableAgents, path, i, agent)
						 select path.Concat(new[] { agent }).ToArray()
				).ToArray();

				if (paths.Length == 0)
					return null;
			}

			return paths
				.Where(path => _succedingConsumer[path[path.Length - 1]] != -1)
				.MinBy(PathCosts);
		}

		protected virtual int PathCosts(int[] path)
		{
			return path.Zip(path.Skip(1), (from, to) => _costMatrix[from, to]).Sum()
				+ _costMatrix[_precedingProducer[path[0]], path[0]]
				+ _costMatrix[path[path.Length - 1], _succedingConsumer[path[path.Length - 1]]];
		}
	}
}

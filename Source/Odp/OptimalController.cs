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
	using System.Collections.Generic;
	using System.Linq;

	public class OptimalController<TAgent> : FastController<TAgent>
		where TAgent : BaseAgent<TAgent>
	{
		public OptimalController(TAgent[] agents) : base(agents) { }

		protected override int[] FindAgentPath(ITask task)
		{
			var identifiers = Enumerable.Range(0, _availableAgents.Length).ToArray();

			var emptyPath = new int[task.RequiredCapabilities.Length];
			var paths = (from agent in identifiers
						 where CanSatisfyNext(task, emptyPath, 0, agent)
						 select new[] { agent }).ToArray();

			if (paths.Length == 0)
				return null;

			for (int i = 1; i < task.RequiredCapabilities.Length; ++i)
			{
				paths = (from path in paths
						 from agent in identifiers
						 let last = path[i - 1]
						 // if agent is reachable from the previous path
						 where _pathMatrix[last, agent] != -1
							// and agent can satisfy the next required capability
							&& CanSatisfyNext(task, path, i, agent)
						 select path.Concat(new[] { agent }).ToArray()
						).ToArray();

				if (paths.Length == 0)
					return null;
			}

			return MinimumPath(paths);
		}

		protected virtual int PathCosts(int[] path)
		{
			return path.Zip(path.Skip(1), (from, to) => _costMatrix[from, to]).Sum();
		}

		private int[] MinimumPath(IEnumerable<int[]> paths)
		{
			var minPath = paths.First();
			int minCost = PathCosts(minPath);

			foreach (var path in paths)
			{
				int cost = PathCosts(path);
				if (cost < minCost)
				{
					minCost = cost;
					minPath = path;
				}
			}

			return minPath;
		}
	}
}
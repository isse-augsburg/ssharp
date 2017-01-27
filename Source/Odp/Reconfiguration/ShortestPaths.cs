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

using System.Collections.Generic;

namespace SafetySharp.Odp.Reconfiguration
{
	using System;

	internal class ShortestPaths<T>
	{
		private readonly Dictionary<T, int> _distance = new Dictionary<T, int>();
		private readonly Dictionary<T, T> _previous = new Dictionary<T, T>();

		public T Source { get; }

		private ShortestPaths(T source)
		{
			Source = source;
			_distance[source] = 0;
			_previous[source] = source;
		}

		private void UpdateEdge(T node, T neighbour, int edgeWeight)
		{
			if (!_distance.ContainsKey(node))
				return;
			var newDistance = _distance[node] + edgeWeight;
			if (!_distance.ContainsKey(neighbour) || newDistance < _distance[neighbour])
			{
				_distance[neighbour] = newDistance;
				_previous[neighbour] = node;
			}
		}

		public T[] GetPathFromSource(T destination)
		{
			if (!_distance.ContainsKey(destination))
				return null;

			var path = new T[_distance[destination]];
			var current = destination;
			for (var i = path.Length - 1; i >= 0; --i)
			{
				path[i] = current;
				current = _previous[current];
			}
			return path;
		}

		public int GetDistance(T node)
		{
			if (!_distance.ContainsKey(node))
				return -1;
			return _distance[node];
		}

		public bool IsReachable(T destination)
		{
			return _distance.ContainsKey(destination);
		}

		public static ShortestPaths<T> Compute(T source, Func<T, IEnumerable<T>> getSuccessors, Func<T, T, int> edgeWeight)
		{
			var visited = new HashSet<T>();
			var knownAgents = new HashSet<T>() { source }; // TODO: replace by minHeap
			var shortestPaths = new ShortestPaths<T>(source);

			do
			{
				var current = knownAgents.MinBy(shortestPaths.GetDistance); // TODO: use minheap operation
				foreach (var neighbour in getSuccessors(current))
				{
					// already found shortest path to neighbour
					if (visited.Contains(neighbour))
						continue;

					shortestPaths.UpdateEdge(current, neighbour, edgeWeight(current, neighbour));
					knownAgents.Add(neighbour);
				}
				visited.Add(current);
			} while (knownAgents.Count > 0);

			return shortestPaths;
		}
	}
}

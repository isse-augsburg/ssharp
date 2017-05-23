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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
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

			var path = new List<T>(_distance[destination]);
			for (var current = destination; !Source.Equals(current); current = _previous[current])
				path.Add(current);
			path.Add(Source);

			path.Reverse();
			return path.ToArray();
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
			var shortestPaths = new ShortestPaths<T>(source);
			var visited = new HashSet<T>();
			var knownAgents = new Heap(source, shortestPaths.GetDistance);

			do
			{
				var current = knownAgents.DeleteMin();
				foreach (var neighbour in getSuccessors(current))
				{
					// already found shortest path to neighbour
					if (visited.Contains(neighbour))
						continue;

					shortestPaths.UpdateEdge(current, neighbour, edgeWeight(current, neighbour));
					knownAgents.InsertOrUpdate(neighbour);
				}
				visited.Add(current);
			} while (!knownAgents.IsEmpty);

			return shortestPaths;
		}

		private class Heap
		{
			private readonly List<T> _heap = new List<T>();
			private readonly Func<T, int> _weight;
			private readonly Dictionary<T, int> _position = new Dictionary<T, int>();

			public Heap(T source, Func<T, int> weight)
			{
				_heap.Add(source);
				_position.Add(source, 0);
				_weight = weight;
			}

			public bool IsEmpty => _heap.Count == 0;

			public T DeleteMin()
			{
				var min = _heap[0];

				_heap[0] = _heap[_heap.Count - 1];
				_heap.RemoveAt(_heap.Count - 1);
				_position.Remove(min);

				var node = 0;
				var child = 2 * node + 1;
				var nodeWeight = _weight(_heap[node]);

				while (child < _heap.Count)
				{
					var childWeight = _weight(_heap[child]);

					if (child + 1 < _heap.Count)
					{
						var rightChildWeight = _weight(_heap[child + 1]);
						if (childWeight < rightChildWeight)
						{
							child++;
							childWeight = rightChildWeight;
						}
					}
					if (nodeWeight < childWeight)
					{
						Swap(node, child);

						node = child;
						nodeWeight = childWeight;
						child = 2 * node + 1;
					}
				}

				return min;
			}

			public void InsertOrUpdate(T element)
			{
				if (!_position.ContainsKey(element))
				{
					_heap.Add(element);
					_position[element] = _heap.Count - 1;
				}
				Decrease(_position[element]);
			}

			private void Decrease(int node)
			{
				int parent = node / 2, nodeWeight = _weight(_heap[node]), parentWeight = _weight(_heap[parent]);
				while (nodeWeight < parentWeight)
				{
					Swap(parent, node);

					node = parent;
					nodeWeight = parentWeight;
					parent = node / 2;
					parentWeight = _weight(_heap[parent]);
				}
			}

			private void Swap(int a, int b)
			{
				var tmp = _heap[a];
				_heap[a] = _heap[b];
				_heap[b] = tmp;

				var tmpPos = _position[_heap[a]];
				_position[_heap[a]] = _position[_heap[b]];
				_position[_heap[b]] = tmpPos;
			}
		}
	}
}

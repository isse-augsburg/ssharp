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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Runtime
{
	using Utilities;




	public struct Edge<TEdgeData>
	{
		public int Source;
		public int Target;
		public TEdgeData Data;

		public Edge(int source, int target, TEdgeData data)
		{
			Source = source;
			Target = target;
			Data = data;
		}
	}

	internal abstract class BidirectionalGraphDirectNodeAccess<TEdgeData>
	{
		public abstract IEnumerable<Edge<TEdgeData>> OutEdges(int vertex);
		public abstract IEnumerable<Edge<TEdgeData>> InEdges(int vertex);

		public Dictionary<int, bool> GetAncestors(Dictionary<int, bool> toNodes, Func<int, bool> ignoreNodeFunc = null, Func<Edge<TEdgeData>, bool> ignoreEdgeFunc = null)
		{
			// standard behavior: do not ignore node or edge
			// node in toNodes are their own ancestors, if they are not ignored by ignoreNodeFunc
			// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
			var nodesAdded = new Dictionary<int, bool>();
			var nodesToTraverse = new Stack<int>();
			foreach (var node in toNodes)
			{
				nodesToTraverse.Push(node.Key);
			}

			while (nodesToTraverse.Count > 0)
			{
				var currentNode = nodesToTraverse.Pop();
				var isIgnored = (ignoreNodeFunc != null && ignoreNodeFunc(currentNode));
				var alreadyDiscovered = nodesAdded.ContainsKey(currentNode);
				if (!(isIgnored || alreadyDiscovered))
				{
					nodesAdded.Add(currentNode, true);
					foreach (var inEdge in InEdges(currentNode))
					{
						if (ignoreEdgeFunc == null || !ignoreEdgeFunc(inEdge))
							nodesToTraverse.Push(inEdge.Source);
					}
				}
			}
			return nodesAdded;
		}
	}


	internal sealed class BidirectionalGraph<TEdgeData> : BidirectionalGraphDirectNodeAccess<TEdgeData>
	{

		private Dictionary<int, List<Edge<TEdgeData>>> _outEdges = new Dictionary<int, List<Edge<TEdgeData>>>();
		private Dictionary<int, List<Edge<TEdgeData>>> _inEdges = new Dictionary<int, List<Edge<TEdgeData>>>();

		public override IEnumerable<Edge<TEdgeData>> OutEdges(int vertex) => _outEdges[vertex];
		public override IEnumerable<Edge<TEdgeData>> InEdges(int vertex) => _inEdges[vertex];

		public List<Edge<TEdgeData>> GetOrCreateOutEdges(int vertex)
		{
			if (_outEdges.ContainsKey(vertex))
			{
				return _outEdges[vertex];
			}
			var dictionary = new List<Edge<TEdgeData>>();
			_outEdges.Add(vertex, dictionary);
			return dictionary;
		}

		public List<Edge<TEdgeData>> GetOrCreateInEdges(int vertex)
		{
			if (_inEdges.ContainsKey(vertex))
			{
				return _inEdges[vertex];
			}
			var dictionary = new List<Edge<TEdgeData>>();
			_inEdges.Add(vertex, dictionary);
			return dictionary;
		}

		public void AddVerticesAndEdge(Edge<TEdgeData> edge)
		{
			GetOrCreateOutEdges(edge.Source).Add(edge);
			GetOrCreateInEdges(edge.Target).Add(edge);
			//Ensure that data structures are initialized even for states without incoming/outgoing edges
			GetOrCreateInEdges(edge.Source);
			GetOrCreateOutEdges(edge.Target);
		}


	}

	internal sealed class BidirectionalGraphSubViewDecorator<TEdgeData> : BidirectionalGraphDirectNodeAccess<TEdgeData>
	{
		private BidirectionalGraphDirectNodeAccess<TEdgeData> _baseGraph;
		private Func<int, bool> _ignoreNodeFunc;
		private Func<Edge<TEdgeData>, bool> _ignoreEdgeFunc;

		public override IEnumerable<Edge<TEdgeData>> OutEdges(int vertex)
		{
			if (_ignoreNodeFunc == null && _ignoreEdgeFunc == null)
			{
				return _baseGraph.OutEdges(vertex);
			}
			if (_ignoreNodeFunc != null && _ignoreEdgeFunc == null && !_ignoreNodeFunc(vertex))
			{
				return _baseGraph.OutEdges(vertex);
			}
			if (_ignoreNodeFunc != null && _ignoreNodeFunc(vertex))
			{
				return new Edge<TEdgeData>[0];
			}
			return FilteredOutEdges(vertex);
		}

		private IEnumerable<Edge<TEdgeData>> FilteredOutEdges(int vertex)
		{
			// only use inside OutEdges(int vertex)
			foreach (var edge in _baseGraph.OutEdges(vertex))
			{
				if (!_ignoreNodeFunc(edge.Target) && !_ignoreEdgeFunc(edge))
				{
					yield return edge;
				}
			}
		}

		public override IEnumerable<Edge<TEdgeData>> InEdges(int vertex)
		{
			if (_ignoreNodeFunc == null && _ignoreEdgeFunc == null)
			{
				return _baseGraph.InEdges(vertex);
			}
			if (_ignoreNodeFunc != null && _ignoreEdgeFunc == null && !_ignoreNodeFunc(vertex))
			{
				return _baseGraph.InEdges(vertex);
			}
			if (_ignoreNodeFunc != null && _ignoreNodeFunc(vertex))
			{
				return new Edge<TEdgeData>[0];
			}
			return FilteredInEdges(vertex);
		}

		private IEnumerable<Edge<TEdgeData>> FilteredInEdges(int vertex)
		{
			// only use inside InEdges(int vertex)
			foreach (var edge in _baseGraph.InEdges(vertex))
			{
				if (!_ignoreNodeFunc(edge.Target) && !_ignoreEdgeFunc(edge))
				{
					yield return edge;
				}
			}
		}

		public BidirectionalGraphSubViewDecorator(BidirectionalGraphDirectNodeAccess<TEdgeData> baseGraph, Func<int, bool> ignoreNodeFunc = null, Func<Edge<TEdgeData>, bool> ignoreEdgeFunc = null)
		{
			_baseGraph = baseGraph;
			_ignoreNodeFunc = ignoreNodeFunc;
			_ignoreEdgeFunc = ignoreEdgeFunc;
		}
	}
}

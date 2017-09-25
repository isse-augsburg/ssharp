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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISSE.SafetyChecking.GenericDataStructures
{
	public struct Edge<TEdgeData>
	{
		public long Source;
		public long Target;
		public TEdgeData Data;

		public Edge(long source, long target, TEdgeData data)
		{
			Source = source;
			Target = target;
			Data = data;
		}
	}

	internal abstract class BidirectionalGraphDirectNodeAccess<TEdgeData>
	{
		public abstract IEnumerable<Edge<TEdgeData>> OutEdges(long vertex);
		public abstract IEnumerable<Edge<TEdgeData>> InEdges(long vertex);
		public abstract IEnumerable<long> GetNodes();
	}


	internal sealed class BidirectionalGraph<TEdgeData> : BidirectionalGraphDirectNodeAccess<TEdgeData>
	{
		private Dictionary<long, List<Edge<TEdgeData>>> _outEdges = new Dictionary<long, List<Edge<TEdgeData>>>();
		private Dictionary<long, List<Edge<TEdgeData>>> _inEdges = new Dictionary<long, List<Edge<TEdgeData>>>();
		private Dictionary<long, bool> _nodes = new Dictionary<long, bool>();

		public override IEnumerable<Edge<TEdgeData>> OutEdges(long vertex) => _outEdges[vertex];
		public override IEnumerable<Edge<TEdgeData>> InEdges(long vertex) => _inEdges[vertex];

		public override IEnumerable<long> GetNodes()
		{
			foreach (var node in _nodes)
			{
				yield return node.Key;
			}
		}

		public List<Edge<TEdgeData>> GetOrCreateOutEdges(long vertex)
		{
			_nodes[vertex] = true;
			if (_outEdges.ContainsKey(vertex))
			{
				return _outEdges[vertex];
			}
			var dictionary = new List<Edge<TEdgeData>>();
			_outEdges.Add(vertex, dictionary);
			return dictionary;
		}

		public List<Edge<TEdgeData>> GetOrCreateInEdges(long vertex)
		{
			_nodes[vertex]=true;
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
		private Func<long, bool> _ignoreNodeFunc;
		private Func<Edge<TEdgeData>, bool> _ignoreEdgeFunc;

		public override IEnumerable<long> GetNodes()
		{
			foreach (var node in _baseGraph.GetNodes())
			{
				if (!_ignoreNodeFunc(node))
					yield return node;
			}
		}

		public override IEnumerable<Edge<TEdgeData>> OutEdges(long vertex)
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

		private IEnumerable<Edge<TEdgeData>> FilteredOutEdges(long vertex)
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

		public override IEnumerable<Edge<TEdgeData>> InEdges(long vertex)
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

		private IEnumerable<Edge<TEdgeData>> FilteredInEdges(long vertex)
		{
			// only use inside InEdges(int vertex)
			foreach (var edge in _baseGraph.InEdges(vertex))
			{
				if (!_ignoreNodeFunc(edge.Source) && !_ignoreEdgeFunc(edge))
				{
					yield return edge;
				}
			}
		}

		public BidirectionalGraphSubViewDecorator(BidirectionalGraphDirectNodeAccess<TEdgeData> baseGraph, Func<long, bool> ignoreNodeFunc = null,
												  Func<Edge<TEdgeData>, bool> ignoreEdgeFunc = null)
		{
			_baseGraph = baseGraph;
			_ignoreNodeFunc = ignoreNodeFunc;
			_ignoreEdgeFunc = ignoreEdgeFunc;
		}
	}

	public static class BidirectionalGraphTAlgorithmExtensions
	{
		internal static Dictionary<long, bool> GetAncestors<TEdgeData>(this BidirectionalGraphDirectNodeAccess<TEdgeData> graph,
																	  Dictionary<long, bool> targetNodes)
		{
			// standard behavior: do not ignore node or edge
			// node in toNodes are their own ancestors, if they are not ignored by ignoreNodeFunc
			// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
			var nodesAdded = new Dictionary<long, bool>();
			var nodesToTraverse = new Stack<long>();
			foreach (var node in targetNodes)
			{
				nodesToTraverse.Push(node.Key);
			}

			while (nodesToTraverse.Count > 0)
			{
				var currentNode = nodesToTraverse.Pop();
				var alreadyDiscovered = nodesAdded.ContainsKey(currentNode);
				if (!alreadyDiscovered)
				{
					nodesAdded.Add(currentNode, true);
					foreach (var inEdge in graph.InEdges(currentNode))
					{
						nodesToTraverse.Push(inEdge.Source);
					}
				}
			}
			return nodesAdded;
		}

		internal static Dictionary<long, bool> GetAncestors<TEdgeData>(this BidirectionalGraphDirectNodeAccess<TEdgeData> graph,
																	  Dictionary<long, bool> targetNodes, Func<long, bool> ignoreNodeFunc,
																	  Func<Edge<TEdgeData>, bool> ignoreEdgeFunc = null)
		{
			// standard behavior: do not ignore node or edge
			// node in toNodes are their own ancestors, if they are not ignored by ignoreNodeFunc
			// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
			var ancestors = new Dictionary<long, bool>();
			var nodesToTraverse = new Stack<long>();
			foreach (var node in targetNodes)
			{
				nodesToTraverse.Push(node.Key);
			}

			while (nodesToTraverse.Count > 0)
			{
				var currentNode = nodesToTraverse.Pop();
				var isIgnored = (ignoreNodeFunc != null && ignoreNodeFunc(currentNode));
				var alreadyDiscovered = ancestors.ContainsKey(currentNode);
				if (!(isIgnored || alreadyDiscovered))
				{
					ancestors.Add(currentNode, true);
					foreach (var inEdge in graph.InEdges(currentNode))
					{
						if (ignoreEdgeFunc == null || !ignoreEdgeFunc(inEdge))
							nodesToTraverse.Push(inEdge.Source);
					}
				}
			}
			return ancestors;
		}
		

		internal static BidirectionalGraph<TEdgeData> CreateSubGraph<TEdgeData>(this BidirectionalGraphDirectNodeAccess<TEdgeData> graph,
																						 Dictionary<long, bool> nodesOfSubGraph)
		{
			var newGraph = new BidirectionalGraph<TEdgeData>();
			foreach (var node in nodesOfSubGraph)
			{
				var newInEdges = newGraph.GetOrCreateInEdges(node.Key);
				foreach (var inEdge in graph.InEdges(node.Key))
				{
					if (nodesOfSubGraph.ContainsKey(inEdge.Source))
					{
						newInEdges.Add(inEdge);
					}
				}
				var newOutEdges = newGraph.GetOrCreateOutEdges(node.Key);
				foreach (var outEdge in graph.OutEdges(node.Key))
				{
					if (nodesOfSubGraph.ContainsKey(outEdge.Target))
					{
						newOutEdges.Add(outEdge);
					}
				}
			}
			return newGraph;
		}

		/*
		TODO: Transform into a DeriveFlatGraph method
		public BidirectionalGraph EdgeWhenAnyDistributionContainsTransition(MarkovDecisionProcess markovChain)
		{
			//Assumption "every node is reachable" is fulfilled due to the construction
			var graph = new BidirectionalGraph();

			var enumerator = markovChain.GetEnumerator();
			while (enumerator.MoveNextState())
			{
				// select targets of first distribution as candidates
				enumerator.MoveNextDistribution();
				var foundSuccessors = new HashSet<int>();
				while (enumerator.MoveNextTransition())
				{
					if (enumerator.CurrentTransition.Value > 0.0)
						foundSuccessors.Add(enumerator.CurrentTransition.Column);
				}

				while (enumerator.MoveNextDistribution())
				{
					//find targets of this distribution and create the union. Some possibleSuccessors may be added
					var successorsOfTransition = new HashSet<int>();
					while (enumerator.MoveNextTransition())
					{
						if (enumerator.CurrentTransition.Value > 0.0)
							successorsOfTransition.Add(enumerator.CurrentTransition.Column);
					}
					foundSuccessors.UnionWith(successorsOfTransition);
				}

				// add all possibleSuccessors
				foreach (var successor in foundSuccessors)
				{
					graph.AddVerticesAndEdge(new Edge(enumerator.CurrentState, successor));
				}
			}
			return new UnderlyingDigraph(graph);
		}		
		*/
	}
}
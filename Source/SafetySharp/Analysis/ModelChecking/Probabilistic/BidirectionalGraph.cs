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

	internal sealed class BidirectionalGraph
	{
		public struct Edge
		{
			public int Source;
			public int Target;

			public Edge(int source,int target)
			{
				Source = source;
				Target = target;
			}
		}

		private Dictionary<int, List<Edge>> _outEdges = new Dictionary<int, List<Edge>>();
		private Dictionary<int, List<Edge>> _inEdges = new Dictionary<int, List<Edge>>();

		public IEnumerable<Edge> OutEdges(int vertex) => _outEdges[vertex];
		public IEnumerable<Edge> InEdges(int vertex) => _inEdges[vertex];

		public List<Edge> GetOrCreateOutEdges(int vertex)
		{
			if (_outEdges.ContainsKey(vertex))
			{
				return _outEdges[vertex];
			}
			var dictionary = new List<Edge>();
			_outEdges.Add(vertex, dictionary);
			return dictionary;
		}

		public List<Edge> GetOrCreateInEdges(int vertex)
		{
			if (_inEdges.ContainsKey(vertex))
			{
				return _inEdges[vertex];
			}
			var dictionary = new List<Edge>();
			_inEdges.Add(vertex, dictionary);
			return dictionary;
		}

		public void AddVerticesAndEdge(Edge edge)
		{
			GetOrCreateOutEdges(edge.Source).Add(edge);
			GetOrCreateInEdges(edge.Target).Add(edge);
		}
	}
}

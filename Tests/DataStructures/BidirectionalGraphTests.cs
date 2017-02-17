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

namespace Tests.DataStructures
{
	using ISSE.SafetyChecking.GenericDataStructures;
	using SafetySharp.Analysis;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class BidirectionalGraphTests
	{

		public TestTraceOutput Output { get; }

		private BidirectionalGraph _graph;


		public BidirectionalGraphTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		private void CreateExemplaryGraphChain1()
		{
			_graph = new BidirectionalGraph();
			_graph.AddVerticesAndEdge(new Edge(0, 0));
			_graph.AddVerticesAndEdge(new Edge(0, 1));
			_graph.AddVerticesAndEdge(new Edge(1, 1));
		}

		private void CreateExemplaryGraphChain2()
		{
			_graph = new BidirectionalGraph();
			_graph.AddVerticesAndEdge(new Edge(0, 1));
			_graph.AddVerticesAndEdge(new Edge(1, 1));
		}

		[Fact]
		public void CalculateAncestorsTest()
		{
			CreateExemplaryGraphChain1();
			
			Assert.Equal(1, _graph.InEdges(0).Count());
			Assert.Equal(2, _graph.InEdges(1).Count());
			Assert.Equal(2, _graph.OutEdges(0).Count());
			Assert.Equal(1, _graph.OutEdges(1).Count());
		}


		[Fact]
		public void CalculateAncestors2Test()
		{
			CreateExemplaryGraphChain2();

			Assert.Equal(0, _graph.InEdges(0).Count());
			Assert.Equal(2, _graph.InEdges(1).Count());
			Assert.Equal(1, _graph.OutEdges(0).Count());
			Assert.Equal(1, _graph.OutEdges(1).Count());
		}
	}
}

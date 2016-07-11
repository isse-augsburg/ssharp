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

namespace Tests.DataStructures
{
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class DoubleVectorTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		public DoubleVectorTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			var vec = new DoubleVector();
			vec[0] = 1.0;
			vec[7] = 2.0;
			vec[0].ShouldBe(1.0);
			vec[7].ShouldBe(2.0);
			vec.Count.ShouldBe(8);
			var sum = 0.0;
			for (int i = 0; i < vec.Count; i++)
			{
				sum += vec[i];
			}
			sum.ShouldBe(3.0);
			vec[1] = 3.0;
			vec[2] = 4.0;
			vec[4] = 5.0;
			vec[3] = 6.0;
			vec[6] = 7.0;
			vec[5] = 8.0;
			vec[0].ShouldBe(1.0);
			vec[1].ShouldBe(3.0);
			vec[2].ShouldBe(4.0);
			vec[3].ShouldBe(6.0);
			vec[4].ShouldBe(5.0);
			vec[5].ShouldBe(8.0);
			vec[6].ShouldBe(7.0);
			vec[7].ShouldBe(2.0);
			sum = 0.0;
			for (int i = 0; i < vec.Count; i++)
			{
				sum += vec[i];
			}
			sum.ShouldBe(36.0);
		}
	}
}

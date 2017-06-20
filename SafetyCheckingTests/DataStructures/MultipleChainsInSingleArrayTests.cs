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
	using ISSE.SafetyChecking.Utilities;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using Shouldly;

	public class MultipleChainsInSingleArrayTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		public MultipleChainsInSingleArrayTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void RemoveFirst()
		{
			var chain = new MultipleChainsInSingleArray<long>();

			var entry = chain.GetUnusedChainIndex();
			chain.StartChain(entry,0,1L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 0, 2L);
			var todeleteentry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(todeleteentry, 1, 101L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 102L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 103L);
			chain.RemoveChainElement(1, todeleteentry);

			var enumerator = chain.GetEnumerator(1);
			var count = 0L;
			while (enumerator.MoveNext())
			{
				count += enumerator.CurrentElement;
			}
			count.ShouldBe(102L+103L);
		}
		
		[Fact]
		public void RemoveMiddle()
		{
			var chain = new MultipleChainsInSingleArray<long>();

			var entry = chain.GetUnusedChainIndex();
			chain.StartChain(entry, 0, 1L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 0, 2L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 101L);
			var todeleteentry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(todeleteentry, 1, 102L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 103L);
			chain.RemoveChainElement(1, todeleteentry);

			var enumerator = chain.GetEnumerator(1);
			var count = 0L;
			while (enumerator.MoveNext())
			{
				count += enumerator.CurrentElement;
			}
			count.ShouldBe(101L + 103L);
		}

		[Fact]
		public void RemoveLast()
		{
			var chain = new MultipleChainsInSingleArray<long>();

			var entry = chain.GetUnusedChainIndex();
			chain.StartChain(entry, 0, 1L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 0, 2L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 101L);
			entry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(entry, 1, 102L);
			var todeleteentry = chain.GetUnusedChainIndex();
			chain.AppendChainElement(todeleteentry, 1, 103L);
			chain.RemoveChainElement(1, todeleteentry);

			var enumerator = chain.GetEnumerator(1);
			var count = 0L;
			while (enumerator.MoveNext())
			{
				count += enumerator.CurrentElement;
			}
			count.ShouldBe(101L + 102L);
		}
	}
}

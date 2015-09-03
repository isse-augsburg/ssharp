// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Utilities;
	using Xunit.Abstractions;

	internal abstract class LtsMinTestObject : TestObject
	{
		protected bool CheckInvariant(Model model, Func<bool> invariant)
		{
			var ltsMin = new SafetySharp.Analysis.LtsMin();
			ltsMin.OutputWritten += message => Output.Log("{0}", message);

			return ltsMin.CheckInvariant(model, invariant);
		}

		protected bool Check(Model model, Formula formula)
		{
			var ltsMin = new SafetySharp.Analysis.LtsMin();
			ltsMin.OutputWritten += message => Output.Log("{0}", message);

			return ltsMin.Check(model, formula);
		}
	}

	public partial class LtsMinTests : Tests
	{
		public LtsMinTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(Path.Combine(Path.GetDirectoryName(GetFileName()), directory));
		}
	}
}
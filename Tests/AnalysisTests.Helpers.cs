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
	using System.Linq;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Utilities;
	using Xunit.Abstractions;

	public abstract class AnalysisTestObject : TestObject
	{
		protected CounterExample CounterExample { get; set; }

		protected bool CheckInvariant(Formula invariant, params IComponent[] components)
		{
			var modelChecker = CreateModelChecker();
			CounterExample = modelChecker.CheckInvariant(new Model(components), invariant);
			return CounterExample == null;
		}

		protected bool Check(Formula formula, params IComponent[] components)
		{
			var modelChecker = CreateModelChecker();
			CounterExample = modelChecker.Check(new Model(components), formula);
			return CounterExample == null;
		}

		protected SafetyAnalysis.Result Dcca(Formula hazard, params IComponent[] components)
		{
			var modelChecker = CreateModelChecker();
			var analysis = new SafetyAnalysis(modelChecker, new Model(components));
			return analysis.ComputeMinimalCutSets(hazard);
		}

		private ModelChecker CreateModelChecker()
		{
			var modelChecker = (ModelChecker)Activator.CreateInstance(Arguments.Length == 0 ? typeof(LtsMin) : (Type)Arguments[0]);
			modelChecker.OutputWritten += message => Output.Log("{0}", message);

			var ssharpChecker = modelChecker as SSharpChecker;
			if (ssharpChecker != null)
				ssharpChecker.StateCapacity = 1 << 16;

			return modelChecker;
		}

		protected void ShouldContain(ISet<ISet<Fault>> sets, params Fault[] faults)
		{
			foreach (var set in sets)
			{
				var faultSet = new HashSet<Fault>(faults);

				if (set.IsSubsetOf(faultSet) && faultSet.IsSubsetOf(set))
					return;
			}

			throw new TestException("Fault set is not contained in set.");
		}
	}

	public partial class AnalysisTests : Tests
	{
		public AnalysisTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTestsLtsMinOnly(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			foreach (var testCase in EnumerateTestCases(GetAbsoluteTestsDirectory(directory)))
				yield return new object[] { typeof(SSharpChecker) }.Concat(testCase).ToArray();

			foreach (var testCase in EnumerateTestCases(GetAbsoluteTestsDirectory(directory)))
				yield return new object[] { typeof(LtsMin) }.Concat(testCase).ToArray();
		}
	}
}
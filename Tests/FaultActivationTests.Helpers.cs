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

namespace Tests
{
	using System.Collections.Generic;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.ModelChecking;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;
	using Utilities;
	using Xunit.Abstractions;

	public abstract class FaultActivationTestObject : TestObject
	{
		private AnalysisResult<SafetySharpRuntimeModel> _result;
		protected CounterExample<SafetySharpRuntimeModel> CounterExample => _result.CounterExample;
		protected int StateCount => _result.StateCount;
		protected long TransitionCount => _result.TransitionCount;
		protected long ComputedTransitionCount => _result.ComputedTransitionCount;

		protected void GenerateStateSpace(params IComponent[] components)
		{
			var modelCreator=SafetySharpRuntimeModel.CreateExecutedModelCreator(TestModel.InitializeModel(components), new ExecutableStateFormula(() => true));

			var configuration = AnalysisConfiguration.Default;
			configuration.StateCapacity = 10000;
			configuration.StackCapacity = 10000;
			configuration.CpuCount = 1;

			var checker = new InvariantChecker<SafetySharpRuntimeModel>(
				() => new ActivationMinimalExecutedModel<SafetySharpRuntimeModel>(modelCreator, 0, configuration.SuccessorCapacity),
				s => Output.Log("{0}", s),
				configuration,
				formulaIndex: 0);

			_result = checker.Check();
			CounterExample.ShouldBe(null);

			Output.Log($"States: {_result.StateCount}");
			Output.Log($"Actual Transitions: {_result.TransitionCount}");
			Output.Log($"Computed Transitions: {_result.ComputedTransitionCount}");
		}
	}

	public partial class FaultActivationTests : Tests
	{
		public FaultActivationTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}
}
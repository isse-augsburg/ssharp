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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using SafetySharp.Utilities;
	using Shouldly;
	using Utilities;
	using Xunit.Abstractions;

	public abstract class AnalysisTestObject : TestObject
	{
		protected CounterExample CounterExample { get; private set; }
		protected CounterExample[] CounterExamples { get; private set; }
		protected bool SuppressCounterExampleGeneration { get; set; }
		protected IFaultSetHeuristic[] Heuristics { get; set; }
		protected AnalysisResult Result { get; private set; }

		protected void SimulateCounterExample(CounterExample counterExample, Action<Simulator> action)
		{
			// Test directly
			action(new Simulator(counterExample));

			// Test persisted
			using (var file = new TemporaryFile(".ssharp"))
			{
				counterExample.Save(file.FilePath);
				action(new Simulator(CounterExample.Load(file.FilePath)));
			}
		}

		protected bool CheckInvariant(Formula invariant, params IComponent[] components)
		{
			var modelChecker = CreateModelChecker();

			if (Arguments.Length > 1 && (bool)Arguments[1])
			{
				var results = modelChecker.CheckInvariants(TestModel.InitializeModel(components), invariant);
				CounterExample = results[0].CounterExample;
				return results[0].FormulaHolds;
			}

			Result = modelChecker.CheckInvariant(TestModel.InitializeModel(components), invariant);
			CounterExample = Result.CounterExample;
			return Result.FormulaHolds;
		}

		protected bool[] CheckInvariants(IComponent component, params Formula[] invariants)
		{
			var modelChecker = CreateModelChecker();
			var results = (AnalysisResult[])modelChecker.CheckInvariants(TestModel.InitializeModel(component), invariants);
			CounterExamples = results.Select(result => result.CounterExample).ToArray();
			return results.Select(result => result.FormulaHolds).ToArray();
		}

		protected bool Check(Formula formula, params IComponent[] components)
		{
			var modelChecker = CreateModelChecker();
			var result = modelChecker.Check(TestModel.InitializeModel(components), formula);

			CounterExample = result.CounterExample;
			return result.FormulaHolds;
		}

		protected SafetyAnalysisResults DccaWithMaxCardinality(Formula hazard, int maxCardinality, params IComponent[] components)
		{
			return DccaWithMaxCardinality(TestModel.InitializeModel(components), hazard, maxCardinality);
		}

		protected SafetyAnalysisResults Dcca(Formula hazard, params IComponent[] components)
		{
			return DccaWithMaxCardinality(hazard, Int32.MaxValue, components);
		}

		protected OrderAnalysisResults AnalyzeOrder(Formula hazard, params IComponent[] components)
		{
			var configuration = AnalysisConfiguration.Default;
			configuration.StateCapacity = 1 << 10;
			configuration.TransitionCapacity = 1 << 12;
			configuration.GenerateCounterExample = !SuppressCounterExampleGeneration;
			configuration.ProgressReportsOnly = true;

			var analysis = new OrderAnalysis(DccaWithMaxCardinality(hazard, Int32.MaxValue, components), configuration);
			analysis.OutputWritten += message => Output.Log("{0}", message);

			var result = analysis.ComputeOrderRelationships();
			Output.Log("{0}", result);

			return result;
		}

		protected SafetyAnalysisResults DccaWithMaxCardinality(ModelBase model, Formula hazard, int maxCardinality)
		{
			var analysis = new SafetyAnalysis
			{
				Backend = (SafetyAnalysisBackend)Arguments[0],
				Configuration =
				{
					StateCapacity = 1 << 10,
					TransitionCapacity = 1 << 12,
					GenerateCounterExample = !SuppressCounterExampleGeneration
				}
			};
			analysis.OutputWritten += message => Output.Log("{0}", message);

			if (Heuristics != null)
				analysis.Heuristics.AddRange(Heuristics);

			var result = analysis.ComputeMinimalCriticalSets(model, hazard, maxCardinality);
			Output.Log("{0}", result);

			result.Model.ShouldBe(model);
			return result;
		}

		protected SafetyAnalysisResults Dcca(ModelBase model, Formula hazard)
		{
			return DccaWithMaxCardinality(model, hazard, Int32.MaxValue);
		}

		private dynamic CreateModelChecker()
		{
			dynamic modelChecker = Activator.CreateInstance((Type)Arguments[0]);
			modelChecker.OutputWritten += (Action<string>)(message => Output.Log("{0}", message));

			var ssharpChecker = modelChecker as SSharpChecker;
			if (ssharpChecker != null)
			{
				ssharpChecker.Configuration.StateCapacity = 1 << 14;
				ssharpChecker.Configuration.TransitionCapacity = 1 << 16;
				ssharpChecker.Configuration.GenerateCounterExample = !SuppressCounterExampleGeneration;
			}

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

		protected void ShouldContain(OrderRelationship[] relationships, Fault fault1, Fault fault2, OrderRelationshipKind kind)
		{
			foreach (var relationship in relationships)
			{
				if (kind == OrderRelationshipKind.Simultaneously)
				{
					if (relationship.Kind != OrderRelationshipKind.Simultaneously)
						continue;

					if (relationship.FirstFault == fault1 && relationship.SecondFault == fault2)
						return;

					if (relationship.FirstFault == fault2 && relationship.SecondFault == fault1)
						return;
				}

				if (relationship.FirstFault == fault1 && relationship.SecondFault == fault2 && relationship.Kind == kind)
					return;
			}

			throw new TestException("Relationship is not contained in set.");
		}
	}

	public partial class InvariantTests : Tests
	{
		public InvariantTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	public partial class StateConstraintTests : Tests
	{
		public StateConstraintTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	public partial class StateGraphInvariantTests : Tests
	{
		public StateGraphInvariantTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	public partial class LtsMinInvariantTests : Tests
	{
		public LtsMinInvariantTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	public partial class DccaTests : Tests
	{
		public DccaTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}

	public partial class LtlTests : Tests
	{
		public LtlTests(ITestOutputHelper output)
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
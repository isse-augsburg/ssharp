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

namespace Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.ExecutableModel;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Utilities;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit.Abstractions;
	using ISSE.SafetyChecking.FaultMinimalKripkeStructure;

	public abstract class AnalysisTestObject : TestObject
	{
		protected ExecutableCounterExample<SafetySharpRuntimeModel> CounterExample { get; private set; }
		protected ExecutableCounterExample<SafetySharpRuntimeModel>[] CounterExamples { get; private set; }
		protected bool SuppressCounterExampleGeneration { get; set; }
		protected IFaultSetHeuristic[] Heuristics { get; set; }
		protected InvariantAnalysisResult Result { get; private set; }

		protected void SimulateCounterExample(ExecutableCounterExample<SafetySharpRuntimeModel> counterExample, Action<SafetySharpSimulator> action)
		{
			// Test directly
			action(new SafetySharpSimulator(counterExample));

			// Test persisted
			using (var file = new TemporaryFile(".ssharp"))
			{
				counterExample.Save(file.FilePath);
				var counterExampleSerialization = new SafetySharpCounterExampleSerialization();
				action(new SafetySharpSimulator(counterExampleSerialization.Load(file.FilePath)));
			}
		}

		protected bool CheckInvariant(Formula invariant, params IComponent[] components)
		{
			var analysisTestsVariant = (AnalysisTestsVariant)Arguments[0];

			analysisTestsVariant.SetModelCheckerParameter(SuppressCounterExampleGeneration, Output.TextWriterAdapter());

			var modelCreator = SafetySharpRuntimeModel.CreateExecutedModelCreator(TestModel.InitializeModel(components), invariant);

			var useCheckInvariantsInsteadOfCheckInvariant = Arguments.Length > 1 && (bool)Arguments[1];


			if (useCheckInvariantsInsteadOfCheckInvariant)
			{
				var results = analysisTestsVariant.CheckInvariants(modelCreator, invariant);
				CounterExample = results[0].ExecutableCounterExample(modelCreator);
				return results[0].FormulaHolds;
			}
			
			Result = analysisTestsVariant.CheckInvariant(modelCreator, invariant);
			CounterExample = Result.ExecutableCounterExample(modelCreator);
			return Result.FormulaHolds;
		}

		protected bool[] CheckInvariants(IComponent component, params Formula[] invariants)
		{
			var analysisTestsVariant = (AnalysisTestsVariant)Arguments[0];

			var modelCreator = SafetySharpRuntimeModel.CreateExecutedModelCreator(TestModel.InitializeModel(component), invariants);

			analysisTestsVariant.SetModelCheckerParameter(SuppressCounterExampleGeneration, Output.TextWriterAdapter());
			
			var results = analysisTestsVariant.CheckInvariants(modelCreator, invariants);
			CounterExamples = results.Select(result => result.ExecutableCounterExample(modelCreator)).ToArray();
			return results.Select(result => result.FormulaHolds).ToArray();
		}

		protected bool Check(Formula formula, params IComponent[] components)
		{
			var analysisTestsVariant = (AnalysisTestsVariant)Arguments[0];

			var modelCreator = SafetySharpRuntimeModel.CreateExecutedModelCreator(TestModel.InitializeModel(components),formula);

			analysisTestsVariant.SetModelCheckerParameter(SuppressCounterExampleGeneration, Output.TextWriterAdapter());

			var result = analysisTestsVariant.Check(modelCreator,formula);

			CounterExample = result.ExecutableCounterExample(modelCreator);
			return result.FormulaHolds;
		}


		protected SafetyAnalysisResults<SafetySharpRuntimeModel> DccaWithMaxCardinality(Formula hazard, int maxCardinality, params IComponent[] components)
		{
			return DccaWithMaxCardinality(hazard, maxCardinality, ModelCapacityByMemorySize.Small, components);
		}

		protected SafetyAnalysisResults<SafetySharpRuntimeModel> DccaWithMaxCardinality(Formula hazard, int maxCardinality, ModelCapacity capacity, params IComponent[] components)
		{
			return DccaWithMaxCardinality(TestModel.InitializeModel(components), hazard, maxCardinality, capacity);
		}

		protected SafetyAnalysisResults<SafetySharpRuntimeModel> Dcca(Formula hazard, params IComponent[] components)
		{
			return DccaWithMaxCardinality(hazard, Int32.MaxValue, components);
		}

		protected OrderAnalysisResults<SafetySharpRuntimeModel> AnalyzeOrder(Formula hazard, params IComponent[] components)
		{
			var configuration = AnalysisConfiguration.Default;
			configuration.ModelCapacity=ModelCapacityByMemorySize.Tiny;
			configuration.GenerateCounterExample = !SuppressCounterExampleGeneration;
			configuration.ProgressReportsOnly = true;
			configuration.DefaultTraceOutput = Output.TextWriterAdapter();

			var analysis = new SafetySharpOrderAnalysis(DccaWithMaxCardinality(hazard, Int32.MaxValue, components), configuration);

			var result = analysis.ComputeOrderRelationships();
			Output.Log("{0}", result);

			return result;
		}

		protected SafetyAnalysisResults<SafetySharpRuntimeModel> DccaWithMaxCardinality(ModelBase model, Formula hazard, int maxCardinality, ModelCapacity capacity)
		{
			var analysis = new SafetySharpSafetyAnalysis
			{
				Backend = (SafetyAnalysisBackend)Arguments[0],
				Configuration =
				{
					ModelCapacity=capacity,
					GenerateCounterExample = !SuppressCounterExampleGeneration,
					DefaultTraceOutput = Output.TextWriterAdapter()
		}
			};

			if (Heuristics != null)
				analysis.Heuristics.AddRange(Heuristics);

			var result = analysis.ComputeMinimalCriticalSets(model, hazard, maxCardinality);
			Output.Log("{0}", result);
			
			return result;
		}

		protected SafetyAnalysisResults<SafetySharpRuntimeModel> Dcca(ModelBase model, Formula hazard)
		{
			return DccaWithMaxCardinality(model, hazard, Int32.MaxValue, ModelCapacityByMemorySize.Small);
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

		protected void ShouldContain(OrderRelationship<SafetySharpRuntimeModel>[] relationships, Fault fault1, Fault fault2, OrderRelationshipKind kind)
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
	
	public abstract class ProbabilisticAnalysisTestObject : TestObject
	{
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

	public partial class InvariantWithIndexTests : Tests
	{
		public InvariantWithIndexTests(ITestOutputHelper output)
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


	public partial class ProbabilisticTests : Tests
	{
		public ProbabilisticTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> AllProbabilisticModelCheckerTests(string directory)
		{
			foreach (var testCase in EnumerateTestCases(GetAbsoluteTestsDirectory(directory)))
				yield return testCase;
		}
	}
}
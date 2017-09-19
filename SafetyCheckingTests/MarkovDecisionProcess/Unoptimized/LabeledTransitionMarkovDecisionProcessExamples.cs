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

namespace Tests.MarkovDecisionProcess.Unoptimized.LtmdpExamples
{
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using JetBrains.Annotations;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public static class AllExamples
	{
		internal static LabeledTransitionMarkovDecisionProcessExample[] Examples = {new Example1()};
	}

	public abstract class LabeledTransitionMarkovDecisionProcessExample
	{
		internal LabeledTransitionMarkovDecisionProcess Ltmdp { get;  set; } //TODO: When C# supports it, make setter "internal and protected"

		public Dictionary<int, bool> NoState = new Dictionary<int, bool>() { };

		internal static AtomarPropositionFormula Label1Formula = new AtomarPropositionFormula("label1");
		internal static AtomarPropositionFormula Label2Formula = new AtomarPropositionFormula("label2");

		internal Formula ExampleFormula1;
		internal Formula ExampleFormula2;

		public int States;
		public int StateDistributions;
		public int InitialDistributions;

		public double MinimalProbabilityFinallyLabel1;
		public double MaximalProbabilityFinallyLabel1;
	}

	public class LabeledTransitionMarkovDecisionProcessToStringTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests()
		{
			foreach (var example in AllExamples.Examples)
			{
				yield return new object[] { example }; // only one parameter
			}
		}

		public LabeledTransitionMarkovDecisionProcessToStringTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}
		
		[Theory, MemberData(nameof(DiscoverTests))]
		public void ToGraphvizString(LabeledTransitionMarkovDecisionProcessExample example)
		{
			var textWriter = Output.TextWriterAdapter();
			example.Ltmdp.ExportToGv(textWriter);
			textWriter.WriteLine();
		}
	}


	public class Example1 : LabeledTransitionMarkovDecisionProcessExample
	{
		internal static LabeledTransitionMarkovDecisionProcess Create()
		{
			// Just a simple MDP with no nondeterministic choices
			//   ⟳0⟶1⟲

			var ltmdpTestBuilder = new LtmdpTestBuilder();
			
			ltmdpTestBuilder.Ltmdp.StateFormulaLabels = new [] { Label1Formula.Label, Label2Formula.Label };
			ltmdpTestBuilder.Ltmdp.StateRewardRetrieverLabels = new string[] { };

			ltmdpTestBuilder.Clear();
			ltmdpTestBuilder.CreateTransition(new []{false,true}, 0, 0);
			ltmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpTestBuilder.ProcessInitialTransitions();

			ltmdpTestBuilder.Clear();
			ltmdpTestBuilder.StepGraph.NonDeterministicSplit(0, 1, 2);
			ltmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 1.0);
			ltmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 1.0);
			ltmdpTestBuilder.CreateTransition(new[] { true, true }, 0, 1);
			ltmdpTestBuilder.CreateTransition(new[] { false, false }, 1, 2);
			ltmdpTestBuilder.ProcessStateTransitions(0);

			ltmdpTestBuilder.Clear();
			ltmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpTestBuilder.CreateTransition(new[] { true, false }, 1, 0);
			ltmdpTestBuilder.ProcessStateTransitions(1);

			return ltmdpTestBuilder.Ltmdp;
		}

		public Example1()
		{
			Ltmdp = Create();

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}

	}
}

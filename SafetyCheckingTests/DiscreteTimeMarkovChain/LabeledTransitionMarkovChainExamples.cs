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

namespace Tests.DiscreteTimeMarkovChain.LabeledTransitionMarkovChainExamples
{
	using System.Diagnostics;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Utilities;
	using JetBrains.Annotations;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public static class AllExamples
	{
		internal static LabeledTransitionMarkovChainExample[] Examples = { new Example4()};
	}


	public class LabeledTransitionMarkovChainToStringTests
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

		public LabeledTransitionMarkovChainToStringTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void ToGraphvizString(LabeledTransitionMarkovChainExample example)
		{
			var textWriter = Output.TextWriterAdapter();
			example.Ltmc.ExportToGv(textWriter);
			textWriter.WriteLine();
		}
	}
	

	public abstract class LabeledTransitionMarkovChainExample
	{
		internal LabeledTransitionMarkovChain Ltmc { get; set; } //TODO: When C# supports it, make setter "internal and protected"
	
		internal static AtomarPropositionFormula Label1Formula = new AtomarPropositionFormula("label1");
		internal static AtomarPropositionFormula Label2Formula = new AtomarPropositionFormula("label2");

		internal Formula ExampleFormula1;
		internal Formula ExampleFormula2;

		public Dictionary<int, bool> StatesSatisfyDirectlyLabel1Formula;
		public Dictionary<int, bool> StatesSatisfyDirectlyLabel2Formula;
		public Dictionary<int, bool> StatesSatisfyDirectlyExampleFormula1;
		public Dictionary<int, bool> StatesSatisfyDirectlyExampleFormula2;

		public Dictionary<int, bool> AncestorsOfStatesWithLabel1;
		public Dictionary<int, bool> AncestorsOfStatesWithLabel2;

		public double ProbabilityFinallyLabel1;
		public double ProbabilityFinally10Label1;
		public double ProbabilityLabel1UntilLabel2;

	}
	

	public class Example4 : LabeledTransitionMarkovChainExample
	{
		internal static LabeledTransitionMarkovChain Create()
		{
			// 0----> 
			//  --b-> 1⟲
			//           ----> 2⟲
			//           --c-> 2
			// 0--c->        2

			var ltmcTestBuilder = new LtmcTestBuilder();
			
			// add initial state
			ltmcTestBuilder.ClearTransitions();
			ltmcTestBuilder.CreateTransition(new []{ false,false}, 0, 1.0);
			ltmcTestBuilder.ProcessInitialTransitions();

			// add state 0
			ltmcTestBuilder.ClearTransitions();
			ltmcTestBuilder.CreateTransition(new[] { false, false }, 1, 0.6);
			ltmcTestBuilder.CreateTransition(new[] { true, false }, 1, 0.3);
			ltmcTestBuilder.CreateTransition(new[] { false, true},  2, 0.1);
			ltmcTestBuilder.ProcessStateTransitions(0);

			// add state 1
			ltmcTestBuilder.ClearTransitions();
			ltmcTestBuilder.CreateTransition(new[] { false, false }, 1, 0.9);
			ltmcTestBuilder.CreateTransition(new[] { false, false }, 2, 0.01);
			ltmcTestBuilder.CreateTransition(new[] { false, true }, 2, 0.09);
			ltmcTestBuilder.ProcessStateTransitions(1);

			// add state 2
			ltmcTestBuilder.ClearTransitions();
			ltmcTestBuilder.CreateTransition(new[] { false, false }, 2, 1.0);
			ltmcTestBuilder.ProcessStateTransitions(2);


			ltmcTestBuilder.Ltmc.StateFormulaLabels = new [] {"label1", "label2" };
			return ltmcTestBuilder.Ltmc;
		}

		public Example4()
		{
			Ltmc = Create();

			 ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			 ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			//StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 0, true }, { 2, true } };
			//StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 3, true } };
			//StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			//StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 2, true }, { 3, true } };
			//
			//AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 2, true } };
			//AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } , { 2, true } , { 3, true }, { 4, true } };
			//
			//ProbabilityFinallyLabel1 = 1.0;
			//ProbabilityFinally10Label1 = 1.0;
			//ProbabilityLabel1UntilLabel2 = 0.2;
		}
	}
}

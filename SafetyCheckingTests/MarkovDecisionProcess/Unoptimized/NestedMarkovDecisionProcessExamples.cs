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

namespace Tests.MarkovDecisionProcess.Unoptimized.NmdpExamples
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
		internal static NestedMarkovDecisionProcessExample[] Examples = {new Example1(), new Example2(), new Example3(), new Example4(), new Example5(), new Example6(), new Example7(), new Example8(), new Example9(), new Example10() };
	}

	public abstract class NestedMarkovDecisionProcessExample
	{
		internal NestedMarkovDecisionProcess Nmdp { get;  set; } //TODO: When C# supports it, make setter "internal and protected"

		public Dictionary<int, bool> NoState = new Dictionary<int, bool>() { };

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
		
		public Dictionary<int, bool> StatesProb0ALabel1;
		public Dictionary<int, bool> StatesProb1ELabel1;
		public Dictionary<int, bool> StatesProb0ELabel1;

		public int States;
		public int StateDistributions;
		public int InitialDistributions;

		public double MinimalProbabilityFinallyLabel1;
		public double MaximalProbabilityFinallyLabel1;

		protected static NestedMarkovDecisionProcess InitializeNmdp(int states)
		{
			var modelCapacity = new ModelCapacityByModelSize(states, states * 5);
			return new NestedMarkovDecisionProcess(modelCapacity);
		}
	}

	public class NestedMarkovDecisionProcessToStringTests
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

		public NestedMarkovDecisionProcessToStringTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}
		
		[Theory, MemberData(nameof(DiscoverTests))]
		public void ToGraphvizString(NestedMarkovDecisionProcessExample example)
		{
			var textWriter = Output.TextWriterAdapter();
			example.Nmdp.ExportToGv(textWriter);
			textWriter.WriteLine();
		}
	}


	public class Example1 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with no nondeterministic choices
			//   ⟳0⟶1⟲
			var nmdp = InitializeNmdp(2);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false}));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0,LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0+1,1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0+0, 1, 0.6);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0+1, 0, 0.4);
			return nmdp;
		}

		public Example1()
		{
			Nmdp = Create();

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() {  };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true } , { 1, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { };

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}

	}

	public class Example2 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just another simple MDP with no nondeterministic choices
			//   0⟶1⟲
			var nmdp = InitializeNmdp(2);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 1, 1.0);
			return nmdp;
		}

		public Example2()
		{
			Nmdp = Create();

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { };

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class Example3 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob1e
			//   0⟶0.5⟼1⟲    0.5⇢0
			//       0.5⟼3⟶4➞0.5⟲
			//             ↘2⟲
			var nmdp = InitializeNmdp(5);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 0.5);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 3, 0.5);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { true, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			var cidFirstInnerState3 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState3, LtmdpChoiceType.Nondeterministic, cidFirstInnerState3, cidFirstInnerState3 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState3 + 0, 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState3 + 1, 4, 1.0);

			nmdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			var cidRootState4 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(4, cidRootState4);
			var cidFirstInnerState4 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState4, LtmdpChoiceType.Probabilitstic, cidFirstInnerState4, cidFirstInnerState4 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState4 + 0, 0, 0.5);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState4 + 1, 4, 0.5);
			return nmdp;
		}

		public Example3()
		{
			Nmdp = Create();

			States = 5;
			StateDistributions = 6;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 2, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 2, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 2, true }, { 3, true }, { 4, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 1, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 2, true }, { 3, true } }; //Explanation: 1st iteration removes 1. 2nd 0 and 4
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 3, true }, { 4, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 0.5;
		}
	}

	public class Example4 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob1e
			//   0⇢3
			//    ↘0.5⟼1⟲    0.5⇢0
			//      0.5⟼3⟶4➞0.5⟲
			//            ↘2⟲
			var nmdp = InitializeNmdp(5);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			var cidSecondInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState0, LtmdpChoiceType.Probabilitstic, cidSecondInnerState0, cidSecondInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 0, 1, 0.5);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 1, 3, 0.5);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 3, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { true, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			var cidFirstInnerState3 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState3, LtmdpChoiceType.Nondeterministic, cidFirstInnerState3, cidFirstInnerState3 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState3 + 0, 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState3 + 1, 4, 1.0);

			nmdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			var cidRootState4 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(4, cidRootState4);
			var cidFirstInnerState4 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState4, LtmdpChoiceType.Probabilitstic, cidFirstInnerState4, cidFirstInnerState4 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState4 + 0, 0, 0.5);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState4 + 1, 4, 0.5);
			return nmdp;
		}

		public Example4()
		{
			Nmdp = Create();

			States = 5;
			StateDistributions = 7;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 2, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() {  };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 2, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 2, true }, { 3, true }, { 4, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 1, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 2, true }, { 3, true }, { 4, true } };  //Explanation: 1st iteration removes 1.
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 3, true }, { 4, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class Example5 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   0⟼1↘
			//    ↘2⟼3⟲
			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 2, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 3, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 3, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);
			return nmdp;
		}

		public Example5()
		{
			Nmdp = Create();

			States = 4;
			StateDistributions = 5;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 3, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() {  };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 3, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { };

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class Example6 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   4
			//   ⇅
			//   0⟼1↘
			//    ↘2⟼3⟲
			var nmdp = InitializeNmdp(5);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(3);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 2, 4, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 3, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 3, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			nmdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			var cidRootState4 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(4, cidRootState4);
			nmdp.AddContinuationGraphLeaf(cidRootState4, 4, 1.0);
			return nmdp;
		}

		public Example6()
		{
			Nmdp = Create();

			States = 4;
			StateDistributions = 7;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 3, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() {  };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 3, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true }, { 4, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true }, { 4, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 4, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class Example7 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   0⟼1↘
			//    ↘2⟶3⟲
			//       ⟶0.5⇢3
			//        ↘0.5⟶4⇢0

			var nmdp = InitializeNmdp(5);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 2, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 3, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			var cidFirstInnerState2 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState2, LtmdpChoiceType.Nondeterministic, cidFirstInnerState2, cidFirstInnerState2 + 1, 1.0);
			var cidSecondInnerState2 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState2 + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerState2, cidSecondInnerState2 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState2 + 0, 3, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState2 + 0, 3, 0.5);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState2 + 1, 4, 0.5);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			nmdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			var cidRootState4 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(4, cidRootState4);
			nmdp.AddContinuationGraphLeaf(cidRootState4, 0, 1.0);
			return nmdp;
		}

		public Example7()
		{
			Nmdp = Create();

			States = 5;
			StateDistributions = 7;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 3, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 3, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true }, { 4, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true }, { 4, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { };

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}


	public class Example8 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with simple nondeterministic choices
			//   ⟳0⟶1⟲
			var nmdp = InitializeNmdp(2);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 0, 1.0);
			return nmdp;
		}

		public Example8()
		{
			Nmdp = Create();

			States = 2;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true } , { 1, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class Example9 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// MDP of [Parker02, page 36]
			//   0
			//   ⇅
			//   1➞0.6⟼2⟲
			//      0.4⟼3⟲
			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 1, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			var cidFirstInnerState1 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState1, LtmdpChoiceType.Nondeterministic, cidFirstInnerState1, cidFirstInnerState1 + 1, 1.0);
			var cidSecondInnerState1 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState1 + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerState1, cidSecondInnerState1 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState1 + 0, 0, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState1 + 0, 2, 0.6);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState1 + 1, 3, 0.4);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { true, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, true }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);
			return nmdp;
		}

		public Example9()
		{
			Nmdp = Create();

			States = 4;
			StateDistributions = 5;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 2, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 3, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 2, true }, { 3, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 3, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 2, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 0.6;
		}
	}

	public class Example10 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// SimpleExample1a
			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(3);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0 + 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 1, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 2, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 2, 3, 0.4);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, true }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);
			return nmdp;
		}

		public Example10()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 4;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 3, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 3, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 3, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}
	public class ExampleNoChoices : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with no choices
			//   ⟳0
			var nmdp = InitializeNmdp(1);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);
			return nmdp;
		}

		public ExampleNoChoices()
		{
			Nmdp = Create();

			States = 1;
			StateDistributions = 1;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() {  };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }};
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true } };

			MinimalProbabilityFinallyLabel1 = 0.0;
			MaximalProbabilityFinallyLabel1 = 0.0;
		}
	}

	public class ExampleNoChoices2 : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with only deterministic choices
			//   0⟶1⟲
			var nmdp = InitializeNmdp(2);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 1, 1.0);
			return nmdp;
		}

		public ExampleNoChoices2()
		{
			Nmdp = Create();

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { };

			MinimalProbabilityFinallyLabel1 = 1.0;
			MaximalProbabilityFinallyLabel1 = 1.0;
		}
	}

	public class ExampleOneInitialProbabilisticSplit : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with one initial probabilistic choice to states [0,1,2]

			var nmdp = InitializeNmdp(3);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			var cidFirstInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(3);
			nmdp.AddContinuationGraphInnerNode(cidRootInitial, LtmdpChoiceType.Probabilitstic, cidFirstInnerStateInitial, cidFirstInnerStateInitial + 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 0, 0, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 1, 1, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 2, 2, 0.4);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);
			
			return nmdp;
		}

		public ExampleOneInitialProbabilisticSplit()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}
	

	public class ExampleTwoInitialProbabilisticSplits : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with two probabilistic splits to states [0,1,2]

			var nmdp = InitializeNmdp(3);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			var cidFirstInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootInitial, LtmdpChoiceType.Probabilitstic, cidFirstInnerStateInitial, cidFirstInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 0, 0, 0.3);
			var cidSecondInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerStateInitial + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerStateInitial, cidSecondInnerStateInitial + 1, 0.7);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 0, 1, 0.3 / 0.7);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 1, 2, 0.4 / 0.7);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			return nmdp;
		}

		public ExampleTwoInitialProbabilisticSplits()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}


	public class ExampleTwoInitialNondeterministicSplits : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with two probabilistic splits to states [0,1,2]

			var nmdp = InitializeNmdp(3);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			var cidFirstInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootInitial, LtmdpChoiceType.Nondeterministic, cidFirstInnerStateInitial, cidFirstInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 0, 0, 1.0);
			var cidSecondInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerStateInitial + 1, LtmdpChoiceType.Nondeterministic, cidSecondInnerStateInitial, cidSecondInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 1, 2, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			return nmdp;
		}

		public ExampleTwoInitialNondeterministicSplits()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleTwoInitialSplitsNondeterministicThenProbabilistic : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with two probabilistic splits to states [0,1,2]

			var nmdp = InitializeNmdp(3);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			var cidFirstInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootInitial, LtmdpChoiceType.Nondeterministic, cidFirstInnerStateInitial, cidFirstInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 0, 0, 1.0);
			var cidSecondInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerStateInitial + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerStateInitial, cidSecondInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 0, 1, 0.4);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 1, 2, 0.6);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			return nmdp;
		}

		public ExampleTwoInitialSplitsNondeterministicThenProbabilistic()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}


	public class ExampleTwoInitialSplitsProbabilisticThenNondeterministic : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with two probabilistic splits to states [0,1,2]

			var nmdp = InitializeNmdp(3);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);
			var cidFirstInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootInitial, LtmdpChoiceType.Probabilitstic, cidFirstInnerStateInitial, cidFirstInnerStateInitial + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerStateInitial + 0, 0, 0.4);
			var cidSecondInnerStateInitial = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerStateInitial + 1, LtmdpChoiceType.Nondeterministic, cidSecondInnerStateInitial, cidSecondInnerStateInitial + 1, 0.6);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerStateInitial + 1, 2, 1.0);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			nmdp.AddContinuationGraphLeaf(cidRootState0, 0, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			return nmdp;
		}

		public ExampleTwoInitialSplitsProbabilisticThenNondeterministic()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleOneStateProbabilisticSplit : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with one initial probabilistic choice to states [0,1,2]

			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(3);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0 + 2, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 0, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 1, 1, 0.3);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 2, 2, 0.4);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			return nmdp;
		}

		public ExampleOneStateProbabilisticSplit()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleTwoStateProbabilisticSplits : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 0, 0.3);
			var cidSecondInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState0 + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerState0, cidSecondInnerState0 + 1, 0.7);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 0, 1, 0.3 / 0.7);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 1, 2, 0.4 / 0.7);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			return nmdp;
		}

		public ExampleTwoStateProbabilisticSplits()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleTwoStateNondeterministicSplits : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with one initial probabilistic choice to states [0,1,2]

			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 0, 1.0);
			var cidSecondInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState0 + 1, LtmdpChoiceType.Nondeterministic, cidSecondInnerState0, cidSecondInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 1, 2, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			return nmdp;
		}

		public ExampleTwoStateNondeterministicSplits()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleTwoStateSplitsNondeterministicThenProbabilistic : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with one initial probabilistic choice to states [0,1,2]

			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Nondeterministic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 0, 1.0);
			var cidSecondInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState0 + 1, LtmdpChoiceType.Probabilitstic, cidSecondInnerState0, cidSecondInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 0, 1, 0.4);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 1, 2, 0.6);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			return nmdp;
		}

		public ExampleTwoStateSplitsNondeterministicThenProbabilistic()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}

	public class ExampleTwoStateSplitsProbabilisticThenNondeterministic : NestedMarkovDecisionProcessExample
	{
		internal static NestedMarkovDecisionProcess Create()
		{
			// Just a simple MDP with one initial probabilistic choice to states [0,1,2]

			var nmdp = InitializeNmdp(4);
			nmdp.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			nmdp.StateRewardRetrieverLabels = new string[] { };
			var cidRootInitial = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.AddContinuationGraphLeaf(cidRootInitial, 0, 1.0);
			nmdp.SetRootContinuationGraphLocationOfInitialState(cidRootInitial);

			nmdp.SetStateLabeling(0, new StateFormulaSet(new[] { true, false }));
			var cidRootState0 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(0, cidRootState0);
			var cidFirstInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidRootState0, LtmdpChoiceType.Probabilitstic, cidFirstInnerState0, cidFirstInnerState0 + 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidFirstInnerState0 + 0, 0, 0.4);
			var cidSecondInnerState0 = nmdp.GetPlaceForNewContinuationGraphElements(2);
			nmdp.AddContinuationGraphInnerNode(cidFirstInnerState0 + 1, LtmdpChoiceType.Nondeterministic, cidSecondInnerState0, cidSecondInnerState0 + 1, 0.6);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 0, 1, 1.0);
			nmdp.AddContinuationGraphLeaf(cidSecondInnerState0 + 1, 2, 1.0);

			nmdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			var cidRootState1 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(1, cidRootState1);
			nmdp.AddContinuationGraphLeaf(cidRootState1, 1, 1.0);

			nmdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			var cidRootState2 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(2, cidRootState2);
			nmdp.AddContinuationGraphLeaf(cidRootState2, 2, 1.0);

			nmdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			var cidRootState3 = nmdp.GetPlaceForNewContinuationGraphElements(1);
			nmdp.SetRootContinuationGraphLocationOfState(3, cidRootState3);
			nmdp.AddContinuationGraphLeaf(cidRootState3, 3, 1.0);

			return nmdp;
		}

		public ExampleTwoStateSplitsProbabilisticThenNondeterministic()
		{
			Nmdp = Create();

			States = 3;
			StateDistributions = 3;
			InitialDistributions = 1;

			ExampleFormula1 = Label1Formula;
			ExampleFormula2 = Label2Formula;

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = StatesSatisfyDirectlyLabel1Formula;
			StatesSatisfyDirectlyExampleFormula2 = StatesSatisfyDirectlyLabel2Formula;

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };

			StatesProb0ALabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };
			StatesProb1ELabel1 = new Dictionary<int, bool>() { };
			StatesProb0ELabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true }, { 2, true }, { 3, true } };

			MinimalProbabilityFinallyLabel1 = 0.4;
			MaximalProbabilityFinallyLabel1 = 0.4;
		}
	}
}

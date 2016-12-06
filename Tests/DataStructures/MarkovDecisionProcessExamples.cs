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

namespace Tests.DataStructures.MarkovDecisionProcessExamples
{
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using SafetySharp.Utilities.Graph;
	using SafetySharp.Analysis.Probabilistic.MdpBased.ExportToGv;
	using SafetySharp.Utilities;

	public static class AllExamples
	{
		internal static MarkovDecisionProcessExample[] Examples = {new Example1(), new Example2(), new Example3(), new Example4(), new Example5(), new Example6(), new Example7()};
	}

	public abstract class MarkovDecisionProcessExample
	{
		internal MarkovDecisionProcess Mdp { get;  set; } //TODO: When C# supports it, make setter "internal and protected"

		public Dictionary<int, bool> NoState = new Dictionary<int, bool>() { };

		internal ExecutableStateFormula StateFormula1;

		public Dictionary<int, bool> StatesSatisfyDirectlyCondition1;
		public Dictionary<int, bool> StatesProb1ACondition1;
		public Dictionary<int, bool> StatesProb1ECondition1;
		public Dictionary<int, bool> StatesProb0ECondition1;
		public Dictionary<int, bool> StatesAncestorsCondition1;

		public Dictionary<int, bool> StatesSatisfyDirectlyCondition2;
		public Dictionary<int, bool> StatesAncestorsCondition2;

		public int States;
		public int StateDistributions;
		public int InitialDistributions;

		[Fact]
		public string ToGraphvizString()
		{
			var sb = new StringBuilder();
			Mdp.ExportToGv(sb);
			sb.AppendLine();
			return sb.ToString();
		}
	}

	public class Example1 : MarkovDecisionProcessExample
	{
		private MarkovDecisionProcess Create()
		{
			// Just a simple MDP with no nondeterministic choices
			//   ⟳0⟶1⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();
			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false}));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 0.6);
			mdp.AddTransition(0, 0.4);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example1()
		{
			Mdp = Create();

			 StateFormula1 = new ExecutableStateFormula(Mdp.StateFormulaLabels);//new StateFormula(returnTrue, "label1")

			// Condition 1 is here label1
			StatesSatisfyDirectlyCondition1 = new Dictionary<int, bool>() { {1,true} };
			StatesProb1ACondition1 = new Dictionary<int, bool>() { {1,true} };
			StatesProb1ECondition1 = new Dictionary<int, bool>() { {1,true} };
			StatesProb0ECondition1 = new Dictionary<int, bool>() { { 1, true } };
			StatesAncestorsCondition1 = new Dictionary<int, bool>() { { 1, true } };

			StatesSatisfyDirectlyCondition1 = new Dictionary<int, bool>() { { 1, true } };
			StatesAncestorsCondition2 = new Dictionary<int, bool>() { { 1, true } };

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;
		}

	}

	public class Example2 : MarkovDecisionProcessExample
	{
		internal MarkovDecisionProcess Create()
		{
			// Just another simple MDP with no nondeterministic choices
			//   0⟶1⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();
			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example2()
		{
			Mdp = Create();

			States = 2;
			StateDistributions = 2;
			InitialDistributions = 1;
		}
	}

	public class Example3 : MarkovDecisionProcessExample
	{
		internal static MarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob1e
			//   0⟶0.5⟼1⟲    0.5⇢0
			//       0.5⟼3⟶4➞0.5⟲
			//             ↘2⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();

			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 0.5);
			mdp.AddTransition(3, 0.5);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(2, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(2);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(3);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(4, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(4);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(0, 0.5);
			mdp.AddTransition(4, 0.5);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example3()
		{
			Mdp = Create();

			States = 5;
			StateDistributions = 6;
			InitialDistributions = 1;
		}
	}

	public class Example4 : MarkovDecisionProcessExample
	{
		internal MarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob1e
			//   0⇢3
			//    ↘0.5⟼1⟲    0.5⇢0
			//      0.5⟼3⟶4➞0.5⟲
			//            ↘2⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();

			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 0.5);
			mdp.AddTransition(3, 0.5);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(2, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(2);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(3, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(3);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(4, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(4);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(0, 0.5);
			mdp.AddTransition(4, 0.5);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example4()
		{
			Mdp = Create();

			States = 5;
			StateDistributions = 7;
			InitialDistributions = 1;
		}
	}

	public class Example5 : MarkovDecisionProcessExample
	{
		internal static MarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   0⟼1↘
			//    ↘2⟼3⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();

			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(2);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(3);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example5()
		{
			Mdp = Create();

			States = 4;
			StateDistributions = 5;
			InitialDistributions = 1;
		}
	}

	public class Example6 : MarkovDecisionProcessExample
	{
		internal static MarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   4
			//   ⇅
			//   0⟼1↘
			//    ↘2⟼3⟲
			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();

			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(4, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(2);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(3);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(4);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(0, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example6()
		{
			Mdp = Create();

			States = 4;
			StateDistributions = 7;
			InitialDistributions = 1;
		}
	}

	public class Example7 : MarkovDecisionProcessExample
	{
		internal static MarkovDecisionProcess Create()
		{
			// A MDP which was designed to test prob0e
			//   0⟼1↘
			//    ↘2⟶3⟲
			//       ⟶0.5⇢3
			//        ↘0.5⟶4⇢0

			var mdp = new MarkovDecisionProcess(ModelDensity.Medium, ByteSize.MebiByte);
			mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			mdp.StateRewardRetrieverLabels = new string[] { };
			mdp.StartWithInitialDistributions();
			mdp.StartWithNewInitialDistribution();
			mdp.AddTransitionToInitialDistribution(0, 1.0);
			mdp.FinishInitialDistribution();
			mdp.FinishInitialDistributions();

			mdp.SetStateLabeling(0, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(0);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(1, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(2, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(1, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(1);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(2, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(2);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 0.5);
			mdp.AddTransition(4, 0.5);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(3, new StateFormulaSet(new[] { true, false }));
			mdp.StartWithNewDistributions(3);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(3, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();

			mdp.SetStateLabeling(4, new StateFormulaSet(new[] { false, false }));
			mdp.StartWithNewDistributions(4);
			mdp.StartWithNewDistribution();
			mdp.AddTransition(0, 1.0);
			mdp.FinishDistribution();
			mdp.FinishDistributions();
			return mdp;
		}

		public Example7()
		{
			Mdp = Create();

			States = 5;
			StateDistributions = 7;
			InitialDistributions = 1;
		}
	}
}

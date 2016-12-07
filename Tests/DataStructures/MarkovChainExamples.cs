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

namespace Tests.DataStructures.MarkovChainExamples
{
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using SafetySharp.Utilities.Graph;
	using SafetySharp.Analysis.Probabilistic.DtmcBased.ExportToGv;
	using SafetySharp.Utilities;

	public static class AllExamples
	{
		internal static MarkovChainExample[] Examples = { new Example1(), new Example2()};
	}

	public abstract class MarkovChainExample
	{
		internal DiscreteTimeMarkovChain MarkovChain { get; set; } //TODO: When C# supports it, make setter "internal and protected"

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

		[Fact]
		public string ToGraphvizString()
		{
			var sb = new StringBuilder();
			MarkovChain.ExportToGv(sb);
			sb.AppendLine();
			return sb.ToString();
		}
	}

	public class Example1 : MarkovChainExample
	{
		internal static DiscreteTimeMarkovChain Create()
		{
			// Just a simple DTMC
			//   ⟳0⟶1⟲
			var markovChain = new DiscreteTimeMarkovChain(ModelDensity.Medium,ByteSize.MebiByte);
			markovChain.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			markovChain.StateRewardRetrieverLabels = new string[] { };
			markovChain.StartWithInitialDistribution();
			markovChain.AddInitialTransition(0, 1.0);
			markovChain.FinishInitialDistribution();
			markovChain.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			markovChain.StartWithNewDistribution(1);
			markovChain.AddTransition(1, 1.0);
			markovChain.FinishDistribution();
			markovChain.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			markovChain.StartWithNewDistribution(0);
			markovChain.AddTransition(1, 0.6);
			markovChain.AddTransition(0, 0.4);
			markovChain.FinishDistribution();
			//markovChain.ProbabilityMatrix.OptimizeAndSeal();
			return markovChain;
		}

		public Example1()
		{
			MarkovChain = Create();

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);
			
			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() {  };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };
		}
	}

	public class Example2 : MarkovChainExample
	{
		internal static DiscreteTimeMarkovChain Create()
		{
			// Just a simple DTMC
			//   0⟶1⟲
			var markovChain = new DiscreteTimeMarkovChain(ModelDensity.Medium, ByteSize.MebiByte);
			markovChain.StateFormulaLabels = new string[] { Label1Formula.Label, Label2Formula.Label };
			markovChain.StateRewardRetrieverLabels = new string[] { };
			markovChain.StartWithInitialDistribution();
			markovChain.AddInitialTransition(0, 1.0);
			markovChain.FinishInitialDistribution();
			markovChain.SetStateLabeling(1, new StateFormulaSet(new[] { true, false }));
			markovChain.StartWithNewDistribution(1);
			markovChain.AddTransition(1, 1.0);
			markovChain.FinishDistribution();
			markovChain.SetStateLabeling(0, new StateFormulaSet(new[] { false, true }));
			markovChain.StartWithNewDistribution(0);
			markovChain.AddTransition(1, 1.0);
			markovChain.FinishDistribution();
			//markovChain.ProbabilityMatrix.OptimizeAndSeal();
			return markovChain;
		}

		public Example2()
		{
			MarkovChain = Create();

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			ExampleFormula1 = new BinaryFormula(Label1Formula, BinaryOperator.And, Label2Formula);
			ExampleFormula2 = new BinaryFormula(Label1Formula, BinaryOperator.Or, Label2Formula);

			StatesSatisfyDirectlyLabel1Formula = new Dictionary<int, bool>() { { 1, true } };
			StatesSatisfyDirectlyLabel2Formula = new Dictionary<int, bool>() { { 0, true } };
			StatesSatisfyDirectlyExampleFormula1 = new Dictionary<int, bool>() { };
			StatesSatisfyDirectlyExampleFormula2 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };

			AncestorsOfStatesWithLabel1 = new Dictionary<int, bool>() { { 0, true }, { 1, true } };
			AncestorsOfStatesWithLabel2 = new Dictionary<int, bool>() { { 0, true } };
		}
	}
}

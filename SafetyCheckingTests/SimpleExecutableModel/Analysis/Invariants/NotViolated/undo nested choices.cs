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

namespace Tests.SimpleExecutableModel.Analysis.Invariants.NotViolated
{
	using System;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Simulator;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	
	public class UndoNestedChoices : AnalysisTest
	{
		public UndoNestedChoices(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new Model();

			//F != 2 && F != 3 && (G == 0 || G == 7 || G == 8
			var formulaGIs078 =
				new BinaryFormula(new BinaryFormula(Model.GIs0, BinaryOperator.Or, Model.GIs7), BinaryOperator.Or, Model.GIs8);
			var formulaFNot2 = new UnaryFormula(Model.FIs2, UnaryOperator.Not);
			var formulaFNot3 = new UnaryFormula(Model.FIs3, UnaryOperator.Not);
			var formula =
				new BinaryFormula(new BinaryFormula(formulaFNot2, BinaryOperator.And, formulaFNot3), BinaryOperator.And, formulaGIs078);

			var checker = new SimpleQualitativeChecker
			{
				Configuration = AnalysisConfiguration.Default
			};
			checker.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			checker.OutputWritten += output => Output.Log(output);

			var result = checker.CheckInvariant(m, formula);
			result.FormulaHolds.ShouldBe(true);
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[] {0,0};

			public override void SetInitialState()
			{
				State = 0;
			}

			private int F
			{
				get { return LocalInts[0]; }
				set { LocalInts[0] = value; }
			}

			private int G
			{
				get { return LocalInts[1]; }
				set { LocalInts[1] = value; }
			}

			public override void Update()
			{
				G = Choice.Choose(3, 5);
				F = Choice.Choose(1, 2, 3);

				var index = Choice.Resolver.LastChoiceIndex;

				G = Choice.Choose(7, 8);

				Choice.Resolver.MakeChoiceAtIndexDeterministic(index);
			}

			public static Formula FIs2 = new SimpleLocalVarInRangeFormula(0,2);
			public static Formula FIs3 = new SimpleLocalVarInRangeFormula(0,3);
			public static Formula GIs0 = new SimpleLocalVarInRangeFormula(1,0);
			public static Formula GIs7 = new SimpleLocalVarInRangeFormula(1,7);
			public static Formula GIs8 = new SimpleLocalVarInRangeFormula(1,8);
		}
	}
}
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

namespace Tests.Analysis.Probabilistic
{
	using System;
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class MultipleInitialStates : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinally2;
			
			Formula stateIs2 = c.F == 2;
			var finally2 = new UnaryFormula(stateIs2, UnaryOperator.Finally);

			var markovChainGenerator = new SafetySharpDtmcFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.AddFormulaToCheck(finally2);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = (Type)Arguments[0];
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinally2 = modelChecker.CalculateProbability(finally2);
			}

			probabilityOfFinally2.Between(0.33, 0.34).ShouldBe(true);
		}

		private class C : Component
		{
			public int F;

			protected internal override void Initialize()
			{
				F = Choose(1, 2, 3);
			}
		}
	}
}
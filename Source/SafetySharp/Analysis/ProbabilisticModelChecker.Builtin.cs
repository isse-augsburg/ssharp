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

namespace SafetySharp.Analysis
{
	using System.Globalization;
	using System.IO;
	using System.Linq.Expressions;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	public class BuiltinProbabilisticModelChecker : ProbabilisticModelChecker
	{
		private List<int> InitialStates;

		public BuiltinProbabilisticModelChecker(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}
		

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();

			var reachStateFormula = formulaToCheck as ProbabilityToReachStateFormula;
			if (reachStateFormula==null)
				throw new NotImplementedException();
			
			var formulaEvaluator = MarkovChain.CreateFormulaEvaluator(reachStateFormula.Operand);


			throw new NotImplementedException();
		}

		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
		}
	}
}

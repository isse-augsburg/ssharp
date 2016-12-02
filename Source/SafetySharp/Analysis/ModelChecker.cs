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

namespace SafetySharp.Analysis
{
	using ModelChecking.Probabilistic;
	using Modeling;

	/// <summary>
	///   Provides convienent methods for model checking S# models.
	/// </summary>
	public static class ModelChecker
	{
		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />. The appropriate model
		///   checker is chosen automatically.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public static AnalysisResult Check(ModelBase model, Formula formula)
		{
			return new LtsMin().Check(model, formula);
		}

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />. The appropriate
		///   model checker is chosen automatically.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public static AnalysisResult CheckInvariant(ModelBase model, Formula invariant)
		{
			return new SSharpChecker().CheckInvariant(model, invariant);
		}

		/// <summary>
		///   Checks whether the <paramref name="invariants" /> hold in all states of the <paramref name="model" />. The appropriate
		///   model checker is chosen automatically.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariants">The invariants that should be checked.</param>
		public static AnalysisResult[] CheckInvariants(ModelBase model, params Formula[] invariants)
		{
			return new SSharpChecker().CheckInvariants(model, invariants);
		}

		/// <summary>
		///   Calculates the probability to reach a state whether <paramref name="stateFormula" /> holds.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="stateFormula">The state formula to be checked.</param>
		public static Probability CalculateProbabilityToReachState(ModelBase model, Formula stateFormula)
		{
			Probability probabilityToReachState;

			var probabilityToReachStateFormula = new CalculateProbabilityToReachStateFormula(stateFormula);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator(model);
			markovChainGenerator.AddFormulaToCheck(probabilityToReachStateFormula);
			var markovChain=markovChainGenerator.GenerateMarkovChain(probabilityToReachStateFormula);
			var modelChecker = new BuiltinDtmcModelChecker(markovChain);
			probabilityToReachState = modelChecker.CalculateProbability(stateFormula);
			return probabilityToReachState;
		}
	}
}
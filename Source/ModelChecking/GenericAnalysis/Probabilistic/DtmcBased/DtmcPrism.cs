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
	using FormulaVisitors;
	using Modeling;
	using Runtime.Serialization;
	using Utilities;

	public class Prism : ProbabilisticModelChecker
	{
		public Prism(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		

		/************************************************/
		/*         PRISM MODEL WRITER                   */
		/************************************************/

		// There are two simple ways to transform the state space into a pm file.
		// 1.) Add variables for each state label. Initial "active" labels
		//     depend on the initial states. Every time we enter a state, we
		//     set the state label, accordingly. Only reachable states are in the state space.
		//     State vector is quite big.
		// 2.) Add for every label a big formula which is defined as big OR above all
		//     states where the label is true. Formula might get big. Might be
		//     inefficient for Prism to evaluate the formula in each state
		// Currently, we use transformation 1 as it seems the better way. Don't really know which
		// transformation performs better. A third possibility is to use the computation
		// engines of Prism directly
		//  -> http://www.prismmodelchecker.org/manual/ConfiguringPRISM/ComputationEngines

		private TemporaryFile _filePrism;


		private void WriteCommandSourcePart(StreamWriter writer, int sourceState)
		{
			writer.Write("\t [] currentState=" + sourceState + " -> ");
		}

		private void WriteCommandTransitionToState(StreamWriter writer, int targetState, double probability, bool firstTransitionOfCommand)
		{
			if (!firstTransitionOfCommand)
				writer.Write(" + ");
			writer.Write(probability.ToString(CultureInfo.InvariantCulture));
			writer.Write(":(");
			writer.Write("currentState'=");
			writer.Write(targetState);
			writer.Write(")");

			var stateFormulaSet = MarkovChain.StateLabeling[targetState];
			var noStateFormulaLabels = MarkovChain.StateFormulaLabels.Length;
			for (var i = 0; i < noStateFormulaLabels; i++)
			{
				var label = MarkovChain.StateFormulaLabels[i];
				writer.Write(" & (" + label + "' = ");
				if (stateFormulaSet[i])
					writer.Write("true");
				else
					writer.Write("false");
				writer.Write(")");
			}
		}

		private void WriteCommandEnd(StreamWriter writer)
		{
			writer.Write(";");
			writer.WriteLine();
		}

		private void WriteMarkovChainToDisk()
		{
			_filePrism = new TemporaryFile("prism");

			var streamPrism = new StreamWriter(_filePrism.FilePath) { NewLine = "\n" };

			streamPrism.WriteLine("dtmc");
			streamPrism.WriteLine("");
			streamPrism.WriteLine("global currentState : [-1.." + MarkovChain.States + "] init -1;"); // -1 is artificial initial state.

			foreach (var label in MarkovChain.StateFormulaLabels)
				{
				streamPrism.WriteLine("global " + label + " : bool init false;");
			}
			streamPrism.WriteLine("");
			streamPrism.WriteLine("module systemModule");

			// From artificial initial state to real initial states
			var artificialSourceState = -1;
			WriteCommandSourcePart(streamPrism, artificialSourceState);
			var firstTransitionOfCommand = true;

			var initialStateProbabilities = MarkovChain.InitialStateProbabilities;
			for (var indexOfInitialState = 0; indexOfInitialState < initialStateProbabilities.Count; indexOfInitialState++)
			{
				var probability = initialStateProbabilities[indexOfInitialState];
				if (probability > 0.0)
				{
					WriteCommandTransitionToState(streamPrism, indexOfInitialState, probability, firstTransitionOfCommand);
					firstTransitionOfCommand = false;
				}
			}
			WriteCommandEnd(streamPrism);

			var enumerator = MarkovChain.ProbabilityMatrix.GetEnumerator();

			while (enumerator.MoveNextRow())
			{
				var sourceState = enumerator.CurrentRow;
				WriteCommandSourcePart(streamPrism, sourceState);
				firstTransitionOfCommand = true;
				while (enumerator.MoveNextColumn())
				{
					var currentColumnValue = enumerator.CurrentColumnValue.Value;
					WriteCommandTransitionToState(streamPrism, currentColumnValue.Column, currentColumnValue.Value, firstTransitionOfCommand);
					firstTransitionOfCommand = false;
				}
				WriteCommandEnd(streamPrism);
			}
			streamPrism.WriteLine("endmodule");

			streamPrism.Flush();
			streamPrism.Close();
		}

		/************************************************/
		/*         PRISM EXECUTION                      */
		/************************************************/


		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteMarkovChainToDisk();
			
			var isFormulaReturningProbabilityVisitor = new IsFormulaReturningProbabilityVisitor();
			isFormulaReturningProbabilityVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningProbabilityVisitor.IsReturningProbability)
			{
				throw new Exception("expected formula which returns a probability");
			}

			var transformationVisitor = new PrismTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckString = transformationVisitor.TransformedFormula;

			using (var fileProperties = new TemporaryFile("props"))
			{
				File.WriteAllText(fileProperties.FilePath, formulaToCheckString);

				var prismArguments = _filePrism.FilePath + " " + fileProperties.FilePath;

				var prism = ExecutePrism(prismArguments);
				_prismProcessOutput.Clear();
				prism.Run();

				var result = ParseOutput(_prismProcessOutput);
				var quantitativeResult = (PrismResultQuantitative)result.Result;
				
				return new Probability(quantitativeResult.Result);
			}
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
			_filePrism.SafeDispose();
		}
	}
}

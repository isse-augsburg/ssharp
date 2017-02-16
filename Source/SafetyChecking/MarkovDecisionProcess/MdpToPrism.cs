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


namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Globalization;
	using System.IO;

	public static class MdpToPrismExtension
	{
		internal static void ExportToPrism(this MarkovDecisionProcess mdp, TextWriter sb)
		{
			var mdpToPrism = new MdpToPrism(mdp);
			mdpToPrism.WriteMarkovDecisionProcessToStream(sb);
		}
	}

	public class MdpToPrism
	{

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

		private MarkovDecisionProcess _mdp;

		internal MdpToPrism(MarkovDecisionProcess mdp)
		{
			_mdp = mdp;
		}

		private void WriteCommandSourcePart(TextWriter writer, int sourceState)
		{
			writer.Write("\t [] currentState=" + sourceState + " -> ");
		}

		private void WriteCommandTransitionToState(TextWriter writer, int targetState, double probability, bool firstTransitionOfCommand)
		{
			if (!firstTransitionOfCommand)
				writer.Write(" + ");
			writer.Write(probability.ToString(CultureInfo.InvariantCulture));
			writer.Write(":(");
			writer.Write("currentState'=");
			writer.Write(targetState);
			writer.Write(")");

			var stateFormulaSet = _mdp.StateLabeling[targetState];
			var noStateFormulaLabels = _mdp.StateFormulaLabels.Length;
			for (var i = 0; i < noStateFormulaLabels; i++)
			{
				var label = _mdp.StateFormulaLabels[i];
				writer.Write(" & (" + label + "' = ");
				if (stateFormulaSet[i])
					writer.Write("true");
				else
					writer.Write("false");
				writer.Write(")");
			}
		}

		private void WriteCommandEnd(TextWriter writer)
		{
			writer.Write(";");
			writer.WriteLine();
		}

		internal void WriteMarkovDecisionProcessToStream(TextWriter streamPrism)
		{
			streamPrism.WriteLine("mdp");
			streamPrism.WriteLine("");
			streamPrism.WriteLine("global currentState : [-1.." + _mdp.States + "] init -1;"); // -1 is artificial initial state.

			foreach (var label in _mdp.StateFormulaLabels)
				{
				streamPrism.WriteLine("global " + label + " : bool init false;");
			}
			streamPrism.WriteLine("");
			streamPrism.WriteLine("module systemModule");

			var enumerator = _mdp.GetEnumerator();

			// From artificial initial state to real initial states
			var artificialSourceState = -1;
			
			enumerator.SelectInitialDistributions();
			
			while (enumerator.MoveNextDistribution())
			{
				WriteCommandSourcePart(streamPrism, artificialSourceState);
				var firstTransitionOfDistribution = true;
				while (enumerator.MoveNextTransition())
				{
					var probability = enumerator.CurrentTransition.Value;
					if (probability > 0.0)
					{
						WriteCommandTransitionToState(streamPrism, enumerator.CurrentTransition.Column, probability, firstTransitionOfDistribution);
						firstTransitionOfDistribution = false;
					}
				}
				WriteCommandEnd(streamPrism);
			}

			while (enumerator.MoveNextState())
			{
				var sourceState = enumerator.CurrentState;
				while (enumerator.MoveNextDistribution())
				{
					WriteCommandSourcePart(streamPrism, sourceState);
					var firstTransitionOfDistribution = true;
					while (enumerator.MoveNextTransition())
					{
						var probability = enumerator.CurrentTransition.Value;
						if (probability > 0.0)
						{
							WriteCommandTransitionToState(streamPrism, enumerator.CurrentTransition.Column, probability, firstTransitionOfDistribution);
							firstTransitionOfDistribution = false;
						}
					}
					WriteCommandEnd(streamPrism);
				}
			}

			streamPrism.WriteLine("endmodule");

			streamPrism.Flush();
			streamPrism.Close();
		}
	}
}

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
	using System.IO;
	using Modeling;
	using Utilities;

	public class Cadp : ProbabilisticModelChecker
	{
		// There are two simple ways to transform the state space into an aut file.
		// 1.) Add artificial entrance state. Move the state labeling to _incoming_ transitions.
		//     To revert the conversion: Iterate all transitions. Add to the target state the
		//     labeling of the transition. State might already have exactly this label.
		//     Remove the artificial entrance state.
		// 2.) Add artificial entrance state. Move the state labeling to _outgoing_ transitions.
		//     To revert the conversion: Iterate all transitions. Add to the source state the
		//     labeling of the transition. State might already have exactly this label.
		//     Remove the artificial entrance state.
		//     Example implementation: Converter from AUT to MRMC input of
		//     http://wwwhome.cs.utwente.nl/~timmer/scoop/casestudies.html
		// We use transformation 1, because no additional initial "next" is necessary for the transformation
		// of formulas.

		// For syntax refer to
		//   * http://cadp.inria.fr/man/aut.html
		//   * http://cadp.inria.fr/man/bcg_write.html
		//   * http://cadp.inria.fr/man/bcg_min.html#sect2

		private TemporaryFile _fileAut;

		public Cadp(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		private void WriteLabelOfState(StreamWriter writer, int state)
		{
			var firstElement = true;
			var stateFormulaSet = CompactProbabilityMatrix.StateLabeling[state];
			for (var i = 0; i < CompactProbabilityMatrix.NoOfStateFormulaLabels; i++)
			{
				if (stateFormulaSet[i])
				{
					if (firstElement == false)
						writer.Write(" !");
					var label = CompactProbabilityMatrix.StateFormulaLabels[i];
					writer.Write(label);
					firstElement = false;
				}
			}
			if (firstElement)
				writer.Write("i");
		}

		private void WriteProbabilityMatrixToDisk()
		{
			_fileAut = new TemporaryFile("aut");

			var streamAut = new StreamWriter(_fileAut.FilePath) { NewLine = "\n" };

			var stateCount = CompactProbabilityMatrix.States + 1; //we add one artifical initial state with the number 0. CompactProbabilityMatrix is 1-indexed.
			var transitionCount = CompactProbabilityMatrix.NumberOfTransitions;

			streamAut.WriteLine("des (0, " + transitionCount + ", " + stateCount + ")");
			foreach (var initialState in CompactProbabilityMatrix.InitialStates)
			{
				streamAut.Write("(");
				streamAut.Write("0"); // artificial initial state
				streamAut.Write(", ");
				streamAut.Write("\"");
				WriteLabelOfState(streamAut, initialState.State); // target state of initial state
				streamAut.Write("; prob "); // see http://cadp.inria.fr/man/bcg_min.html#sect2
				streamAut.Write(initialState.Probability);
				streamAut.Write("\"");
				streamAut.Write(", ");
				streamAut.Write(initialState.State); // target state of initial state
				streamAut.Write(")");
				streamAut.WriteLine();
			}
			foreach (var transitionList in CompactProbabilityMatrix.TransitionGroups)
			{

				var sourceState = transitionList.Key;
				foreach (var transition in transitionList.Value)
				{
					streamAut.Write("(");
					streamAut.Write(sourceState);
					streamAut.Write(", ");
					streamAut.Write("\"");
					WriteLabelOfState(streamAut, transition.State);
					streamAut.Write("; prob "); // see http://cadp.inria.fr/man/bcg_min.html#sect2
					streamAut.Write(transition.Probability);
					streamAut.Write("\"");
					streamAut.Write(", ");
					streamAut.Write(transition.State);
					streamAut.Write(")");
					streamAut.WriteLine();
				}
			}
			streamAut.Flush();
			streamAut.Close();
		}

		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();

			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			_fileAut.SafeDispose();
		}
	}
}

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
	using System;
	using Utilities;
	using System.IO;
	using System.Globalization;
	using System.Text;

	/// <summary>
	///   Represents a base class for external probabilistic model checker tools.
	/// </summary>
	public abstract class ProbabilisticModelChecker : IDisposable
	{
		public ProbabilityChecker ProbabilityChecker { get; }

		internal CompactProbabilityMatrix CompactProbabilityMatrix => ProbabilityChecker.CompactProbabilityMatrix;

		protected ProbabilisticModelChecker(ProbabilityChecker probabilityChecker)
		{
			ProbabilityChecker = probabilityChecker;
		}

		public abstract void Dispose();

		internal abstract Probability ExecuteCalculation(Formula formulaToCheck);
	}

	public class Mrmc : ProbabilisticModelChecker
	{

		private TemporaryFile _fileTransitions;
		private TemporaryFile _fileStateLabelings;

		public Mrmc(ProbabilityChecker probabilityChecker) : base (probabilityChecker)
		{
		}
		
		private void WriteProbabilityMatrixToDisk()
		{

			_fileTransitions = new TemporaryFile("tra");
			_fileStateLabelings = new TemporaryFile("lab");

			var streamTransitions = new StreamWriter(_fileTransitions.FilePath);
			streamTransitions.NewLine = "\n";
			var streamStateLabelings = new StreamWriter(_fileStateLabelings.FilePath);
			streamStateLabelings.NewLine = "\n";

			streamTransitions.WriteLine("STATES " + CompactProbabilityMatrix.States);
			streamTransitions.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.NumberOfTransitions);
			foreach (var transitionList in CompactProbabilityMatrix.TransitionGroups)
			{
				var sourceState = transitionList.Key;
				foreach (var transition in transitionList.Value)
				{
					streamTransitions.WriteLine(sourceState + " " + transition.State + " " + transition.Probability);
				}
			}
			streamTransitions.Flush();
			streamTransitions.Close();

			streamStateLabelings.WriteLine("#DECLARATION");
			//bool firstElement = true;
			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				if (i > 0)
				{
					streamStateLabelings.Write(" ");
				}
				streamStateLabelings.Write("formula" + i);
			}
			streamStateLabelings.WriteLine();
			streamStateLabelings.WriteLine("#END");
			foreach (var stateFormulaSet in CompactProbabilityMatrix.StateLabeling)
			{
				streamStateLabelings.Write(stateFormulaSet.Key);
				//stateFormulaSet.Value.
				for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
				{
					if (stateFormulaSet.Value[i])
						streamStateLabelings.Write(" formula" + i);
				}
				streamStateLabelings.WriteLine();
			}
			streamStateLabelings.Flush();
			streamStateLabelings.Close();
		}

		private System.Text.RegularExpressions.Regex MrmcResultParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<probability>[0-1]\\.?[0-9]+)$");


		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();


			using (var fileResults = new TemporaryFile("res"))
			using (var fileCommandScript = new TemporaryFile("cmd"))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi");
				script.AppendLine("P { > 0 } [ tt U formula0 ]");
				script.AppendLine("write_res_file 1");
				script.AppendLine("quit");

				File.WriteAllText(fileCommandScript.FilePath, script.ToString());

				var commandlinearguments = "dtmc " + _fileTransitions.FilePath + " " + _fileStateLabelings.FilePath + " " + fileCommandScript.FilePath + " " + fileResults.FilePath;

				var mrmc = new ExternalProcess("mrmc.exe", commandlinearguments);
				mrmc.Run();

				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();

				var index = 0;
				var probability = Probability.Zero;
				while (resultEnumerator.MoveNext())
				{
					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcResultParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = Probability.One;
							var probabilityInState = Double.Parse(parsed.Groups["probability"].Value, CultureInfo.InvariantCulture);
							probability += probabilityOfState * probabilityInState;
						}
						else
						{
							throw new Exception("Expected different output of MRMC");
						}
					}
				}
				return probability;
			}
		}
		
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			_fileTransitions.SafeDispose();
			_fileStateLabelings.SafeDispose();
		}
	}


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

		private void WriteLabelOfState(StreamWriter writer,int state)
		{
			var firstElement = true;
			var stateFormulaSet = CompactProbabilityMatrix.StateLabeling[state];
			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				if (stateFormulaSet[i])
				{
					if (firstElement==false)
						writer.Write(" !");
					writer.Write("formula" + i);
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



	public class Prism : ProbabilisticModelChecker
	{
		// There are two simple ways to transform the state space into a pm file.
		// 1.) Add variables for each state label. Initial "active" labels
		//     depend on the initial states. Every time we enter a state, we
		//     set the state label, accordingly. Only reachable states are in the state space.
		// 2.) Add for every label a big formula which is defined as big OR above all
		//     states where the label is true. Formula might get big. Might be
		//     inefficient for Prism to evaluate the formula in each state
		// Currently, we use transformation 1 as it seems the better way. Don't really know which
		// transformation performs better. A third possibility is to use the computation
		// engines of Prism directly
		//  -> http://www.prismmodelchecker.org/manual/ConfiguringPRISM/ComputationEngines

		private TemporaryFile _filePrism ;

		public Prism(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		private void WriteCommandSourcePart(StreamWriter writer,int sourceState)
		{
			writer.Write("\t [] currentState="+sourceState+" -> ");
		}

		private void WriteCommandTransitionToState(StreamWriter writer, TupleStateProbability transition, bool firstTransitionOfCommand)
		{
			if (!firstTransitionOfCommand)
				writer.Write(" + ");
			writer.Write(transition.Probability);
			writer.Write(":(");
			writer.Write("currentState'=");
			writer.Write(transition.State);
			writer.Write(")");

			var stateFormulaSet = CompactProbabilityMatrix.StateLabeling[transition.State];
			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				writer.Write(" & (formula" + i + "' = ");
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

		private void WriteProbabilityMatrixToDisk()
		{
			_filePrism = new TemporaryFile("prism");

			var streamPrism = new StreamWriter(_filePrism.FilePath) { NewLine = "\n" };

			streamPrism.WriteLine("dtmc");
			streamPrism.WriteLine("");
			streamPrism.WriteLine("global currentState : [0.."+ CompactProbabilityMatrix.States + "] init 0;"); // 0 is artificial initial state.

			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				streamPrism.WriteLine("global formula" + i + " : bool init false;");
			}
			streamPrism.WriteLine("");
			streamPrism.WriteLine("module systemModule");

			// From artificial initial state to real initial states
			var artificialSourceState = 0;
			WriteCommandSourcePart(streamPrism, artificialSourceState);
			var firstTransitionOfCommand = true;
			foreach (var tupleStateProbability in CompactProbabilityMatrix.InitialStates)
			{
				WriteCommandTransitionToState(streamPrism, tupleStateProbability, firstTransitionOfCommand);
				firstTransitionOfCommand = false;
			}
			WriteCommandEnd(streamPrism);

			foreach (var transitionList in CompactProbabilityMatrix.TransitionGroups)
			{
				var sourceState = transitionList.Key;
				WriteCommandSourcePart(streamPrism, sourceState);
				firstTransitionOfCommand = true;
				foreach (var transition in transitionList.Value)
				{
					WriteCommandTransitionToState(streamPrism, transition, firstTransitionOfCommand);
					firstTransitionOfCommand = false;
				}
				WriteCommandEnd(streamPrism);
			}
			streamPrism.WriteLine("endmodule");

			streamPrism.Flush();
			streamPrism.Close();
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
			//_filePrism.SafeDispose();
		}
	}
}
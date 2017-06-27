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



namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Globalization;
	using System.IO;
	using Modeling;

	internal static class MdpToGv
	{
		private static void ExportDistributionsOfEnumerator(MarkovDecisionProcess.MarkovDecisionProcessEnumerator enumerator, string stateName, TextWriter sb)
		{
			var distributionCounter = 0;
			while (enumerator.MoveNextDistribution())
			{
				var distributionNode = $"n{stateName}_{distributionCounter}";
				sb.WriteLine($"{distributionNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");
				sb.WriteLine($"{stateName} -> {distributionNode};");
				while (enumerator.MoveNextTransition())
				{
					sb.WriteLine($"{distributionNode} -> {enumerator.CurrentTransition.Column} [label=\"{Probability.PrettyPrint(enumerator.CurrentTransition.Value)}\"];");
				}
				distributionCounter++;
			}
		}

		public static void ExportToGv(this MarkovDecisionProcess mdp,TextWriter sb)
		{
			sb.WriteLine("digraph S {");
			//sb.WriteLine("size = \"8,5\"");
			sb.WriteLine("node [shape=box];");
			var enumerator = mdp.GetEnumerator();

			enumerator.SelectInitialDistributions();
			var initialStateName = "initialState";
			sb.WriteLine($" {initialStateName} [shape=point,width=0.0,height=0.0,label=\"\"];");
			ExportDistributionsOfEnumerator(enumerator, initialStateName, sb);


			enumerator = mdp.GetEnumerator();
			while (enumerator.MoveNextState())
			{
				var state = enumerator.CurrentState;
				sb.Write($" {state} [label=\"{state}\\n(");
				for (int i = 0; i < mdp.StateFormulaLabels.Length; i++)
				{
					if (i>0)
						sb.Write(",");
					sb.Write(mdp.StateLabeling[state][i]);
				}
				sb.WriteLine(")\"];");
				ExportDistributionsOfEnumerator(enumerator, state.ToString(), sb);
			}
			sb.WriteLine("}");
		}
	}
}

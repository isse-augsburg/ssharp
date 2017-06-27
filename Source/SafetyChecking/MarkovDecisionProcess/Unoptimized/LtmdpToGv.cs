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



namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System.Globalization;
	using System.IO;

	internal static class LtmdpToGv
	{
		private static void ExportCid(LabeledTransitionMarkovDecisionProcess ltmdp, TextWriter sb, string fromNode, bool fromProbabilistic, long currentCid)
		{
			LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement choice = ltmdp.GetContinuationGraphElement(currentCid);
			if (choice.IsChoiceTypeUnsplitOrFinal)
			{
				var thisNode = $"cid{currentCid}";
				sb.WriteLine($" {thisNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");

				if (fromProbabilistic)
					sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"onormal\", label=\"{choice.Probability.ToString(CultureInfo.InvariantCulture)}\"];");
				else
					sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"normal\"];");

				var transitionTarget = ltmdp.GetTransitionTarget((int)choice.To);
				sb.Write($" {thisNode} -> {transitionTarget.TargetState} [ arrowhead =\"normal\",");
				sb.Write("label=\"");
				for (int i = 0; i < ltmdp.StateFormulaLabels.Length; i++)
				{
					if (i > 0)
						sb.Write(",");
					if (transitionTarget.Formulas[i])
						sb.Write("t");
					else
						sb.Write("f");
				}
				sb.WriteLine("\"];");
			}
			else if (choice.IsChoiceTypeForward)
			{
				// only forward node (no recursion)
				// do not print thisNode
				var toNode = $"cid{choice.To}";
				sb.WriteLine($" {fromNode}->{toNode} [ style =\"dashed\", label=\"{choice.Probability.ToString(CultureInfo.InvariantCulture)}\"];");
			}
			else
			{
				// we print how we came to this node
				var thisNode = $"cid{currentCid}";
				sb.WriteLine($" {thisNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");

				if (fromProbabilistic)
					sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"onormal\", label=\"{choice.Probability.ToString(CultureInfo.InvariantCulture)}\"];");
				else
					sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"normal\"];");



				for (var i = choice.From; i <= choice.To; i++)
				{
					ExportCid(ltmdp,sb, thisNode, choice.IsChoiceTypeProbabilitstic, i);
				}
			}
		}

		public static void ExportToGv(this LabeledTransitionMarkovDecisionProcess ltmdp,TextWriter sb)
		{
			sb.WriteLine("digraph S {");
			//sb.WriteLine("size = \"8,5\"");
			sb.WriteLine("node [shape=box];");

			var initialStateName = "initialState";
			sb.WriteLine($" {initialStateName} [shape=point,width=0.0,height=0.0,label=\"\"];");
			var initialCid = ltmdp.GetRootContinuationGraphLocationOfInitialState();
			ExportCid(ltmdp, sb, initialStateName, false, initialCid);

			foreach (var state in ltmdp.SourceStates)
			{
				sb.Write($" {state} [label=\"{state}");
				sb.WriteLine("\"];");

				var cid = ltmdp.GetRootContinuationGraphLocationOfState(state);
				var fromNode = state.ToString();
				ExportCid(ltmdp, sb, fromNode, false, cid);
			}
			sb.WriteLine("}");
		}
	}
}

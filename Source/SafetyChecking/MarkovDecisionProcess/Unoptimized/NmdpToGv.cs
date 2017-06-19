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

	internal static class NmdpToGv
	{
		private static void ExportCid(NestedMarkovDecisionProcess nmdp, TextWriter sb, string fromNode, string fromArrowHead, long currentCid)
		{
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = nmdp.GetContinuationGraphLeaf(currentCid);

				var thisNode = $"cid{currentCid}";
				sb.WriteLine($" {thisNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");
				sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"{fromArrowHead}\", label=\"{cgl.Probability.ToString(CultureInfo.InvariantCulture)}\"];");

				sb.WriteLine($" {thisNode} -> {cgl.ToState} [ arrowhead =\"normal\"];");
			}
			else if (cge.IsChoiceTypeForward)
			{
				// only forward node (no recursion)
				// do not print thisNode
				var cgi = nmdp.GetContinuationGraphInnerNode(currentCid);
				var toNode = $"cid{cgi.ToCid}";
				sb.WriteLine($" {fromNode}->{toNode} [ style =\"dashed\", label=\"{cgi.Probability.ToString(CultureInfo.InvariantCulture)}\"];");
			}
			else
			{
				// we print how we came to this node
				var cgi = nmdp.GetContinuationGraphInnerNode(currentCid);

				var thisNode = $"cid{currentCid}";
				sb.WriteLine($" {thisNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");
				sb.WriteLine($" {fromNode}->{thisNode} [ arrowhead =\"{fromArrowHead}\", label=\"{cgi.Probability.ToString(CultureInfo.InvariantCulture)}\"];");

				var thisArrowhead = "normal";
				if (cge.IsChoiceTypeProbabilitstic)
					thisArrowhead = "onormal";

				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					ExportCid(nmdp,sb, thisNode, thisArrowhead, i);
				}
			}
		}

		public static void ExportToGv(this NestedMarkovDecisionProcess nmdp,TextWriter sb)
		{
			sb.WriteLine("digraph S {");
			sb.WriteLine("size = \"8,5\"");
			sb.WriteLine("node [shape=box];");

			var initialStateName = "initialState";
			sb.WriteLine($" {initialStateName} [shape=point,width=0.0,height=0.0,label=\"\"];");
			var initialCid = nmdp.GetRootContinuationGraphLocationOfInitialState();
			ExportCid(nmdp, sb, initialStateName, "normal", initialCid);

			for (var state=0; state < nmdp.States; state++)
			{
				sb.Write($" {state} [label=\"{state}\\n(");
				for (int i = 0; i < nmdp.StateFormulaLabels.Length; i++)
				{
					if (i>0)
						sb.Write(",");
					sb.Write(nmdp.StateLabeling[state][i]);
				}
				sb.WriteLine(")\"];");

				var cid = nmdp.GetRootContinuationGraphLocationOfState(state);
				var fromNode = state.ToString();
				ExportCid(nmdp, sb, fromNode, "normal", cid);
			}
			sb.WriteLine("}");
		}
	}
}

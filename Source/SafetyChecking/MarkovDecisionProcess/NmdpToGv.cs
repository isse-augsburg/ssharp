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

	internal static class NmdpToGv
	{
		private static void ExportCid(NestedMarkovDecisionProcess nmdp, TextWriter sb, long currentCid)
		{
			var fromNode = $"cid{currentCid}";
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = nmdp.GetContinuationGraphLeaf(currentCid);
				sb.WriteLine($" {fromNode} -> {cgl.ToState} [label=\"{cgl.Probability.ToString(CultureInfo.InvariantCulture)}\"];");
			}
			else
			{
				var cgi = nmdp.GetContinuationGraphInnerNode(currentCid);
				var arrowhead = "normal";
				if (cge.IsChoiceTypeProbabilitstic)
					arrowhead = "onormal";

				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					var toNode = $"cid{i}";
					sb.WriteLine($" {toNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");
					sb.WriteLine($" {fromNode}->{toNode} [ arrowhead =\"{arrowhead}\"];");
					ExportCid(nmdp,sb, i);
				}
			}

		}

		public static void ExportToGv(this NestedMarkovDecisionProcess nmdp,TextWriter sb)
		{
			sb.WriteLine("digraph S {");
			sb.WriteLine("size = \"8,5\"");
			sb.WriteLine("node [shape=box];");

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
				var rootNode = $"cid{cid}";
				sb.WriteLine($" {rootNode} [ shape=point,width=0.1,height=0.1,label=\"\" ];");
				sb.WriteLine($" {state}->{rootNode};");
				ExportCid(nmdp, sb, cid);
			}
			sb.WriteLine("}");
		}
	}
}

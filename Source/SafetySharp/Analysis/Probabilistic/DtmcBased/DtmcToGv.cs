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
using SafetySharp.Runtime;

namespace SafetySharp.Analysis.Probabilistic.DtmcBased.ExportToGv
{
	using System.Globalization;

	internal static class DtmcToGv
	{
		public static void ExportToGv(this DiscreteTimeMarkovChain markovChain,StringBuilder sb)
		{
			sb.AppendLine("digraph S {");
			sb.AppendLine("size = \"8,5\"");
			sb.AppendLine("node [shape=box];");
			var enumerator = markovChain.GetEnumerator();
			while (enumerator.MoveNextState())
			{
				var state = enumerator.CurrentState;
				sb.Append($" {state} [label=\"{state}\\n(");
				for (int i = 0; i < markovChain.StateFormulaLabels.Length; i++)
				{
					if (i>0)
						sb.Append(",");
					sb.Append(markovChain.StateLabeling[state][i]);
				}
				sb.AppendLine(")\"];");
				while (enumerator.MoveNextTransition())
				{
					sb.AppendLine($"{enumerator.CurrentState} -> {enumerator.CurrentTransition.Column} [label=\"{enumerator.CurrentTransition.Value.ToString(CultureInfo.InvariantCulture)}\"];");
				}
			}
			sb.AppendLine("}");
		}
	}
}

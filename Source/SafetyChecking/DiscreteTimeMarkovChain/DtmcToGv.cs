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


namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Globalization;
	using System.IO;

	internal static class DtmcToGv
	{
		public static void ExportToGv(this DiscreteTimeMarkovChain markovChain, TextWriter sb)
		{
			sb.WriteLine("digraph S {");
			sb.WriteLine("size = \"8,5\"");
			sb.WriteLine("node [shape=box];");
			var enumerator = markovChain.GetEnumerator();
			while (enumerator.MoveNextState())
			{
				var state = enumerator.CurrentState;
				sb.Write($" {state} [label=\"{state}\\n(");
				for (int i = 0; i < markovChain.StateFormulaLabels.Length; i++)
				{
					if (i>0)
						sb.Write(",");
					sb.Write(markovChain.StateLabeling[state][i]);
				}
				sb.WriteLine(")\"];");
				while (enumerator.MoveNextTransition())
				{
					sb.WriteLine($"{enumerator.CurrentState} -> {enumerator.CurrentTransition.Column} [label=\"{enumerator.CurrentTransition.Value.ToString(CultureInfo.InvariantCulture)}\"];");
				}
			}
			sb.WriteLine("}");
		}
	}
}

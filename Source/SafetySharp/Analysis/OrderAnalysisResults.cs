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
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Modeling;

	/// <summary>
	///   Represents the result of an <see cref="OrderAnalysis" />.
	/// </summary>
	public sealed class OrderAnalysisResults
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		internal OrderAnalysisResults(SafetyAnalysisResults safetyAnalysisResults, TimeSpan time,
									  IDictionary<ISet<Fault>, IEnumerable<OrderRelationship>> orderRelationships)
		{
			SafetyAnalysisResults = safetyAnalysisResults;
			Time = time;
			OrderRelationships = orderRelationships;
		}

		/// <summary>
		///   Gets the time it took to complete the analysis.
		/// </summary>
		public TimeSpan Time { get; internal set; }

		/// <summary>
		///   Gets the results of the <see cref="SafetyAnalysis" /> conducted to find the minimal critical fault sets.
		/// </summary>
		public SafetyAnalysisResults SafetyAnalysisResults { get; }

		/// <summary>
		///   Gets the order relationships that were found for the minimal critical fault sets.
		/// </summary>
		public IDictionary<ISet<Fault>, IEnumerable<OrderRelationship>> OrderRelationships { get; }

		/// <summary>
		///   Returns a string representation of the minimal critical fault sets.
		/// </summary>
		public override string ToString()
		{
			var builder = new StringBuilder();
			builder.AppendLine(SafetyAnalysisResults.ToString());

			builder.AppendLine();
			builder.AppendLine("=======================================================================");
			builder.AppendLine("=======        Fault Activation Order Analysis: Results         =======");
			builder.AppendLine("=======================================================================");
			builder.AppendLine();

			builder.AppendLine($"Elapsed Time: {Time}");
			builder.AppendLine($"Order Relationship Count: {OrderRelationships.Values.SelectMany(r => r).Count()}");
			builder.AppendLine();

			foreach (var pair in OrderRelationships)
			{
				var relationships = pair.Value.ToArray();

				builder.AppendLine($"{{ {String.Join(", ", pair.Key.Select(f => f.Name))} }}");

				if (relationships.Length == 0)
					builder.AppendLine("    no order relationships exist");
				else
				{
					var i = 1;
					foreach (var relationship in pair.Value)
					{
						builder.AppendLine($"    ({i}) {relationship}");
						++i;
					}
				}
			}

			return builder.ToString();
		}
	}
}
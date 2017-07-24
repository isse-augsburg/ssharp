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
	using System;
	using System.IO;
	using Formula;
	using Utilities;

	public class QuantitativeParametricAnalysisParameter
	{
		/// The state formula which _must_ finally be true.
		public Formula StateFormula;

		/// The maximal number of steps. If stateFormula is satisfied the first time any step later than bound, this probability does not count into the end result.
		public int? Bound;

		public double From;

		public double To;

		public int Steps;

		public Action<double> UpdateParameterInModel;
	}
	
	public class QuantitativeParametricAnalysisResults
	{
		public double From;

		public double To;

		public double Steps;

		public double[] SourceValues;

		public double[] ResultValues;

		/// <param name="outputWriter">The TextWriter to write the output csv-file to.</param>
		public void ToCsv(TextWriter outputWriter)
		{
			var csvWriter = new CsvWriter(outputWriter);
			csvWriter.AddEntry("Step");
			for (var i = 0; i < Steps; i++)
			{
				csvWriter.AddEntry(i);
			}
			csvWriter.NewLine();

			csvWriter.AddEntry("CurrentValue");
			for (var i = 0; i < Steps; i++)
			{
				csvWriter.AddEntry(SourceValues[i]);
			}
			csvWriter.NewLine();

			csvWriter.AddEntry("Pr");
			for (var i = 0; i < Steps; i++)
			{
				csvWriter.AddEntry(ResultValues[i]);
			}
			csvWriter.NewLine();

		}
	}
}

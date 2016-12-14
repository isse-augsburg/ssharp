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
using SafetySharp.Modeling;

namespace SafetySharp.Analysis.ModelChecking.Probabilistic
{
	using FormulaVisitors;
	using Runtime;
	using System.IO;
	using Analysis.Probabilistic;
	using Utilities;

	//Not very mature! Use only as oracle for tests of the builtin model checker!

	class ExternalMdpModelCheckerPrism : MdpModelChecker, IDisposable
	{
		// Note: Should be used with using(var modelchecker = new ...), otherwise the disposed method may be
		// executed by the .net framework directly after using _filePrism.FilePath the last time and the
		// file deleted before it could be used by the prism process
		public ExternalMdpModelCheckerPrism(MarkovDecisionProcess mdp, TextWriter output=null) : base(mdp, output)
		{
			WriteMarkovChainToDisk();
		}

		private void WriteMarkovChainToDisk()
		{
			_filePrism = new TemporaryFile("prism");

			var streamPrism = new StreamWriter(_filePrism.FilePath) { NewLine = "\n" };

			MarkovDecisionProcess.ExportToPrism(streamPrism);
		}

		private TemporaryFile _filePrism;
		
		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override Probability CalculateMaximalProbability(Formula formulaToCheck)
		{
			var isFormulaReturningBoolVisitor = new IsFormulaReturningBoolValueVisitor();

			isFormulaReturningBoolVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningBoolVisitor.IsFormulaReturningBoolValue)
			{
				throw new Exception("expected formula which returns a bool");
			}

			var transformationVisitor = new PrismTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckInnerString = transformationVisitor.TransformedFormula;
			var formulaToCheckString = "Pmax=? [ " + formulaToCheckInnerString + "]";

			using (var fileProperties = new TemporaryFile("props"))
			{
				File.WriteAllText(fileProperties.FilePath, formulaToCheckString);

				var prismArguments = _filePrism.FilePath + " " + fileProperties.FilePath;

				var prismProcess = new PrismProcess(_output);
				var quantitativeResult = prismProcess.ExecutePrismAndParseResult(prismArguments);

				return new Probability(quantitativeResult.ResultingProbability);
			}
		}

		internal override Probability CalculateMinimalProbability(Formula formulaToCheck)
		{
			var isFormulaReturningBoolVisitor = new IsFormulaReturningBoolValueVisitor();

			isFormulaReturningBoolVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningBoolVisitor.IsFormulaReturningBoolValue)
			{
				throw new Exception("expected formula which returns a bool");
			}

			var transformationVisitor = new PrismTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckInnerString = transformationVisitor.TransformedFormula;
			var formulaToCheckString = "Pmin=? [ " + formulaToCheckInnerString + "]";

			using (var fileProperties = new TemporaryFile("props"))
			{
				File.WriteAllText(fileProperties.FilePath, formulaToCheckString);

				var prismArguments = _filePrism.FilePath + " " + fileProperties.FilePath;

				var prismProcess = new PrismProcess(_output);
				var quantitativeResult = prismProcess.ExecutePrismAndParseResult(prismArguments);

				return new Probability(quantitativeResult.ResultingProbability);
			}
		}

		internal override Probability CalculateProbabilityRange(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}
		
		public override void Dispose()
		{
			_filePrism.SafeDispose();
		}
	}
}

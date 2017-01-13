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

namespace SafetySharp.Analysis.FormulaVisitors
{
	using Runtime;
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;

	/// <summary>
	///   Transfers a <see cref="Formula" /> which was created for one instance of an executable model to another
	/// </summary>
	internal class TransferFormulaToNewExecutedModelInstanceVisitor : FormulaVisitor
	{
		public Dictionary<string,AtomarPropositionFormula> LabelToFormula { get; }

		public Formula CurrentFormula { get; private set; }

		private TransferFormulaToNewExecutedModelInstanceVisitor(AtomarPropositionFormula[] atomarPropositionFormulasInNewExecutedModel)
		{
			LabelToFormula=new Dictionary<string, AtomarPropositionFormula>();
			foreach (var atomarPropositionFormula in atomarPropositionFormulasInNewExecutedModel)
			{
				LabelToFormula.Add(atomarPropositionFormula.Label,atomarPropositionFormula);
			}
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			Visit(formula.Operand);
			CurrentFormula = new UnaryFormula(CurrentFormula, formula.Operator);
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			Visit(formula.LeftOperand);
			var left = CurrentFormula;
			Visit(formula.RightOperand);
			var right = CurrentFormula;
			CurrentFormula = new BinaryFormula(left,formula.Operator, right);
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			// If this fails, new model is incomparable with old one, because "formula" is not
			// contained in exectutableModel.AtomarPropositionFormulas.
			// To solve this issue, perhaps the createModel method must be created in a way
			// that it is aware of formula.
			// (example in S#: When createModel is created by Serializer(ModelBase,formulas), formula is
			// in formulas (directly or as leaf in one element)
			CurrentFormula = LabelToFormula[formula.Label];
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitRewardFormula(RewardFormula formula)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitProbabilisticFormula(ProbabilitisticFormula formula)
		{
			Visit(formula.Operand);
			CurrentFormula=new ProbabilitisticFormula(CurrentFormula, formula.Comparator, formula.CompareToValue);
		}

		public static Formula Transfer<TExecutableModel>(TExecutableModel exectutableModel, Formula formula) where TExecutableModel : ExecutableModel<TExecutableModel>
		{
			var visitor = new TransferFormulaToNewExecutedModelInstanceVisitor(exectutableModel.AtomarPropositionFormulas);
			visitor.Visit(formula);

			return visitor.CurrentFormula;
		}
	}
}
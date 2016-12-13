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
	using FormulaVisitors;
	using Utilities;

	public enum ProbabilisticComparator
	{
		LowerThan,
		LowerEqual,
		BiggerThan,
		BiggerEqual
	}

	public class ProbabilitisticFormula : Formula
	{
		public Formula Operand { get; }

		/// <summary>
		///   Gets the operator of the unary formula.
		/// </summary>
		public ProbabilisticComparator Comparator { get; }

		public double CompareToValue { get; }

		public ProbabilitisticFormula(Formula operand, ProbabilisticComparator comparator, double compareToValue)
		{
			//P_{comparator value}(operand)
			Operand = operand;
			Comparator = comparator;
			CompareToValue = compareToValue;
		}

		/// <summary>
		///   Executes the <paramref name="visitor" /> for this formula.
		/// </summary>
		/// <param name="visitor">The visitor that should be executed.</param>
		internal override void Visit(FormulaVisitor visitor)
		{
			visitor.VisitProbabilisticFormula(this);
		}
	}
}
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System;
	using System.Diagnostics;
	using System.Text;
	using Utilities;

	/// <summary>
	///   Transforms the formula to a LtsMin μ-calculus formula.
	/// </summary>
	/// <remarks>
	///   LtsMin μ-calculus syntax: http://fmt.cs.utwente.nl/tools/ltsmin/doc/ltsmin-mucalc.html
	///   Transformation from CTL to mu calculus from: E. Allen Emerson, Model Checking and the Mu-calculus
	///   which is also a good introduction to the mu calculus and Model Checking itself.
	/// </remarks>
	internal class LtsMinMuCalculusTransformer : FormulaVisitor
	{
		// We support only CTL for now.

		/// <summary>
		///   The string builder that is used to construct the transformed formula.
		/// </summary>
		private readonly StringBuilder _builder = new StringBuilder();

		private int _sigmaVariableCounter = 0;

		private string CreateFreshSigmaVariableName()
		{
			// This function should return a fresh variable name, which does not occur in any StateFormula.
			_sigmaVariableCounter += 1;
			return $"sigma{_sigmaVariableCounter}";
		}
		
		/// <summary>
		///   Gets the transformed μ-calculus formula.
		/// </summary>
		public string TransformedFormula => _builder.ToString();

		/// <summary>
		///   Visits the <paramref name="formula" />.
		/// </summary>
		public override void VisitUnaryFormula(UnaryFormula formula)
		{
			switch (formula.Operator)
			{
				case UnaryOperator.Next:
				case UnaryOperator.Finally:
				case UnaryOperator.Globally:
					Assert.NotReached($"Path Operator only allowed in Path Formulas.");
					break;
				case UnaryOperator.Not:
					_builder.Append("(");
					_builder.Append(" ! ");
					Visit(formula.Operand);
					_builder.Append(")");
					break;
				case UnaryOperator.All:
					VisitForAllPathFormula(formula.Operand);
					break;
				case UnaryOperator.Exists:
					VisitExistsPathFormula(formula.Operand);
					break;
				default:
					Assert.NotReached($"Unknown or unsupported unary operator '{formula.Operator}'.");
					break;
			}
		}

		
		public void VisitForAllPathFormula(Formula formula)
		{
			var freshSigmaVariable = "";
			if (formula is BinaryFormula)
			{
				var binaryFormula = (BinaryFormula)formula;
				switch (binaryFormula.Operator)
				{
					case BinaryOperator.And:
					case BinaryOperator.Or:
					case BinaryOperator.Implication:
					case BinaryOperator.Equivalence:
						Assert.NotReached($"Assumed to get a Path Operator.");
						break;
					case BinaryOperator.Until:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( mu {freshSigmaVariable}. (");
						Visit(binaryFormula.RightOperand);
						_builder.Append($" || (");
						Visit(binaryFormula.LeftOperand);
						_builder.Append($" && [] {freshSigmaVariable} )))");
						break;
					default:
						Assert.NotReached($"Unknown or unsupported binary operator '{binaryFormula.Operator}'.");
						break;
				}
			}
			else if (formula is UnaryFormula)
			{
				var unaryFormula = (UnaryFormula)formula;
				switch (unaryFormula.Operator)
				{
					case UnaryOperator.Next:
						_builder.Append($" [] (");
						Visit(unaryFormula.Operand);
						_builder.Append($" )");
						break;
					case UnaryOperator.Finally:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( mu {freshSigmaVariable}. (");
						Visit(unaryFormula.Operand);
						_builder.Append($" || [] {freshSigmaVariable} ))");
						break;
					case UnaryOperator.Globally:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( nu {freshSigmaVariable}. (");
						Visit(unaryFormula.Operand);
						_builder.Append($" && [] {freshSigmaVariable} ))");
						Visit(unaryFormula.Operand);
						break;
					case UnaryOperator.Not:
						Assert.NotReached($"Assumed to get a Path Operator.");
						break;
					default:
						Assert.NotReached($"Unknown or unsupported unary operator '{unaryFormula.Operator}'.");
						break;
				}
			}
			else
			{
				Assert.NotReached("Formula has to be either a BinaryFormula or an UnaryFormula");
			}
		}

		public void VisitExistsPathFormula(Formula formula)
		{
			var freshSigmaVariable = "";
			if (formula is BinaryFormula)
			{
				var binaryFormula = (BinaryFormula)formula;
				switch (binaryFormula.Operator)
				{
					case BinaryOperator.And:
					case BinaryOperator.Or:
					case BinaryOperator.Implication:
					case BinaryOperator.Equivalence:
						Assert.NotReached($"Assumed to get a Path Operator.");
						break;
					case BinaryOperator.Until:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( mu {freshSigmaVariable}. (");
						Visit(binaryFormula.RightOperand);
						_builder.Append($" || (");
						Visit(binaryFormula.LeftOperand);
						_builder.Append($" && <> {freshSigmaVariable} )))");
						break;
					default:
						Assert.NotReached($"Unknown or unsupported binary operator '{binaryFormula.Operator}'.");
						break;
				}
			}
			else if (formula is UnaryFormula)
			{
				var unaryFormula = (UnaryFormula)formula;
				switch (unaryFormula.Operator)
				{
					case UnaryOperator.Next:
						_builder.Append($" <> (");
						Visit(unaryFormula.Operand);
						_builder.Append($" )");
						break;
					case UnaryOperator.Finally:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( mu {freshSigmaVariable}. (");
						Visit(unaryFormula.Operand);
						_builder.Append($" || <> {freshSigmaVariable} ))");
						break;
					case UnaryOperator.Globally:
						freshSigmaVariable = CreateFreshSigmaVariableName();
						_builder.Append($"( nu {freshSigmaVariable}. (");
						Visit(unaryFormula.Operand);
						_builder.Append($" && <> {freshSigmaVariable} ))");
						Visit(unaryFormula.Operand);
						break;
					case UnaryOperator.Not:
						Assert.NotReached($"Assumed to get a Path Operator.");
						break;
					default:
						Assert.NotReached($"Unknown or unsupported unary operator '{unaryFormula.Operator}'.");
						break;
				}
			}
			else
			{
				Assert.NotReached("Formula has to be either a BinaryFormula or an UnaryFormula");
			}
		}


		/// <summary>
		///   Visits the <paramref name="formula"/>. This expects a CTL State Formula.
		/// </summary>
		public override void VisitBinaryFormula(BinaryFormula formula)
		{
			// We assume that the topMost operator is either A or G
			_builder.Append("(");
			Visit(formula.LeftOperand);
			
			switch (formula.Operator)
			{
				case BinaryOperator.And:
					_builder.Append(" && ");
					break;
				case BinaryOperator.Or:
					_builder.Append(" || ");
					break;
				case BinaryOperator.Implication:
					_builder.Append(" -> ");
					break;
				case BinaryOperator.Equivalence:
					_builder.Append(" <-> ");
					break;
				case BinaryOperator.Until:
					Assert.NotReached($"Until only allowed in Path Formulas.");
					break;
				default:
					Assert.NotReached($"Unknown or unsupported binary operator '{formula.Operator}'.");
					break;
			}

			Visit(formula.RightOperand);
			_builder.Append(")");
		}

		/// <summary>
		///   Visits the <paramref name="formula." />
		/// </summary>
		public override void VisitStateFormula(StateFormula formula)
		{
			_builder.Append(formula.Label);
		}
	}
}
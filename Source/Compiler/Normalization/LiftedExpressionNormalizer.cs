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

namespace SafetySharp.Compiler.Normalization
{
	using System.Linq;
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Roslyn.Syntax;

	/// <summary>
	///   Replaces the parameters of method invocations or object creations with a lifted lambda if the corresponding
	///   method argument has the <see cref="LiftExpressionAttribute" /> applied.
	///   For instance, with method M declared as <c>void M([LiftExpression] int a, int b)</c>:
	///   <code>
	///  		M(1 + 2, 4); 
	///  		// becomes:
	///  		M(() => 1 + 2, 4);
	/// 	</code>
	/// </summary>
	public sealed class LiftedExpressionNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   Ensures that we don't try to lift the arguments of an array access; we can't get a symbol for
		///   array's [] operator.
		/// </summary>
		public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax elementAccess)
		{
			if (elementAccess.Expression.GetExpressionType(SemanticModel).TypeKind == TypeKind.Array)
				return elementAccess;

			return base.VisitElementAccessExpression(elementAccess);
		}

		/// <summary>
		///   Lifts the expression represented by <paramref name="argument" />, if necessary.
		/// </summary>
		public override SyntaxNode VisitArgument(ArgumentSyntax argument)
		{
			if (argument.Parent.Parent is ElementAccessExpressionSyntax)
				return base.VisitArgument(argument);

			var requiresLifting = argument.HasAttribute<LiftExpressionAttribute>(SemanticModel);
			argument = (ArgumentSyntax)base.VisitArgument(argument);

			if (!requiresLifting)
				return argument;

			var expression = SyntaxBuilder.Lambda(Enumerable.Empty<ParameterSyntax>(), argument.Expression).WithTrivia(argument);
			return argument.WithExpression(expression);
		}
	}
}
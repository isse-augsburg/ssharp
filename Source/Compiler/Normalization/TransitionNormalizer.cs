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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Runtime.Reflection;
	using Utilities;

	/// <summary>
	///   Normalizes transition chains <c>stateMachine.Transition(...).Transition(...)...</c>.
	/// </summary>
	public sealed class TransitionNormalizer : Normalizer
	{
		/// <summary>
		///   The global name of the <see cref="StateMachineExtensions" /> type.
		/// </summary>
		private readonly string _extensionType = typeof(StateMachineExtensions).GetGlobalName();

		/// <summary>
		///   The stack of variable prefixes to uniquely name local variables of nested transitions.
		/// </summary>
		private readonly Stack<string> _variablePrefixes = new Stack<string>();

		/// <summary>
		///   The writer that is used to generate the code.
		/// </summary>
		private readonly CodeWriter _writer = new CodeWriter();

		/// <summary>
		///   The name of the generated choice variable.
		/// </summary>
		private string ChoiceVariable => $"choice{_variablePrefixes.Peek()}".ToSynthesized();

		/// <summary>
		///   The name of the generated transitions count variable.
		/// </summary>
		private string CountVariable => $"transitionsCount{_variablePrefixes.Peek()}".ToSynthesized();

		/// <summary>
		///   The name of the generated state machine variable.
		/// </summary>
		private string StateMachineVariable => $"stateMachine{_variablePrefixes.Peek()}".ToSynthesized();

		/// <summary>
		///   The name of the generated transitions array variable.
		/// </summary>
		private string TransitionsVariable => $"transitions{_variablePrefixes.Peek()}".ToSynthesized();

		/// <summary>
		///   Normalizes the <paramref name="statement" />.
		/// </summary>
		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax statement)
		{
			var transformed = TransformExpression(statement.Expression);
			if (transformed == null)
				return statement;

			return transformed.EnsureLineCount(statement);
		}

		/// <summary>
		///   Normalizes the <paramref name="expression" />.
		/// </summary>
		private StatementSyntax TransformExpression(ExpressionSyntax expression)
		{
			// If the expression statement is a sequence of 
			// invocation expressions of StateMachine.Transition() ->
			// member access expressions ->
			// invocation expressions StateMachine.Transition() -> 
			// member access expressions -> 
			// ... -> 
			// some other expression of type StateMachine
			// we have to replace all of that by the generated transition code

			if (expression.Kind() != SyntaxKind.InvocationExpression)
				return null;

			var methodSymbol = expression.GetReferencedSymbol<IMethodSymbol>(SemanticModel);
			if (!methodSymbol.IsTransitionMethod(SemanticModel))
				return null;

			try
			{
				_variablePrefixes.Push(Guid.NewGuid().ToString().Replace("-", "_"));

				ExpressionSyntax stateMachine;
				var transitions = DecomposeTransitionChain((InvocationExpressionSyntax)expression, out stateMachine);

				_writer.Clear();
				_writer.AppendLine("#line hidden");
				_writer.AppendLine("unsafe");
				_writer.AppendBlockStatement(() =>
				{
					_writer.AppendLine($"#line {stateMachine.GetLineNumber()}");
					_writer.AppendLine($"var {StateMachineVariable} = {stateMachine.RemoveTrivia().ToFullString()};");
					_writer.AppendLine("#line hidden");
					_writer.AppendLine($"var {ChoiceVariable} = {_extensionType}.GetChoice({StateMachineVariable});");
					_writer.NewLine();

					_writer.AppendLine($"var {TransitionsVariable} = stackalloc int[{transitions.Count}];");
					_writer.AppendLine($"var {CountVariable} = 0;");
					_writer.NewLine();

					GenerateTransitionSelection(transitions);

					_writer.AppendLine($"if ({CountVariable} != 0)");
					_writer.AppendBlockStatement(() =>
					{
						_writer.AppendLine($"switch ({TransitionsVariable}[{ChoiceVariable}.ChooseIndex({CountVariable})])");
						_writer.AppendBlockStatement(() => GenerateTransitionSections(transitions));
					});
				});

				return SyntaxFactory.ParseStatement(_writer.ToString()).WithLeadingNewLines(1);
			}
			finally
			{
				_variablePrefixes.Pop();
			}
		}

		/// <summary>
		///   Generates the code that selects the transitions.
		/// </summary>
		private void GenerateTransitionSelection(List<Transition> transitions)
		{
			for (var i = 0; i < transitions.Count; ++i)
			{
				var transition = transitions[i];

				WriteLineNumber(transition.SourceLineNumber);
				_writer.AppendLine($"if ({_extensionType}.IsInState({StateMachineVariable}, {transition.SourceStates.ToFullString()}))");
				_writer.AppendLine("#line hidden");
				_writer.AppendBlockStatement(() =>
				{
					WriteLineNumber(transition.GuardLineNumber);
					_writer.AppendLine($"if ({transition.Guard.ToFullString()})");
					_writer.AppendLine("#line hidden");
					_writer.AppendBlockStatement(() =>
					{
						_writer.AppendLine("#line hidden");
						_writer.AppendLine($"{TransitionsVariable}[{CountVariable}++] = {i};");
					});
				});
				_writer.NewLine();
			}
		}

		/// <summary>
		///   Generates the transition sections.
		/// </summary>
		private void GenerateTransitionSections(List<Transition> transitions)
		{
			for (var i = 0; i < transitions.Count; ++i)
			{
				var transition = transitions[i];

				_writer.AppendLine($"case {i}:");
				_writer.AppendBlockStatement(() =>
				{
					GenerateTransitionEffect(transition);

					_writer.AppendLine("#line hidden");
					_writer.AppendLine("break;");
				});
			}
		}

		/// <summary>
		///   Generates the code for the effect of the <paramref name="transition" />.
		/// </summary>
		private void GenerateTransitionEffect(Transition transition)
		{
			WriteLineNumber(transition.TargetLineNumber);
			_writer.AppendLine(
				$"{_extensionType}.ChangeState({StateMachineVariable}, {ChoiceVariable}.Choose({transition.TargetStates.ToFullString()}));");

			WriteLineNumber(transition.ActionLineNumber);

			// We have to be careful when writing out the action: If it contains any return statements,
			// we might prematurely exit the containing method. Therefore, if there is any return statement,
			// declare a lambda and call it immediately; this is inefficient, but this is a rare situation anyway.
			if (transition.Action.Descendants<ReturnStatementSyntax>().Any())
			{
				var lambda = "lambda".ToSynthesized();
				_writer.AppendLine($"{typeof(Action).GetGlobalName()} {lambda} = () => {transition.Action.ToFullString()};");
				_writer.AppendLine("#line hidden");
				_writer.AppendLine($"{lambda}();");
			}
			else
				_writer.AppendLine($"{transition.Action.ToFullString()}");
		}

		/// <summary>
		///   Writes the line number information.
		/// </summary>
		private void WriteLineNumber(int lineNumber)
		{
			if (lineNumber != 0)
				_writer.AppendLine($"#line {lineNumber}");
			else
				_writer.AppendLine("#line hidden");
		}

		/// <summary>
		///   Collects all calls to <see cref="StateMachine{TState}" /> <c>Transition</c> methods within
		///   <paramref name="expression" />.
		/// </summary>
		private List<Transition> DecomposeTransitionChain(InvocationExpressionSyntax expression, out ExpressionSyntax stateMachine)
		{
			var transitions = new List<Transition>();

			while (true)
			{
				AddTransitions(transitions, expression.ArgumentList);

				var memberAccess = (MemberAccessExpressionSyntax)expression.Expression;
				if (memberAccess.Expression.Kind() != SyntaxKind.InvocationExpression)
				{
					stateMachine = memberAccess.Expression;
					break;
				}

				expression = (InvocationExpressionSyntax)memberAccess.Expression;
				var methodSymbol = expression.GetReferencedSymbol<IMethodSymbol>(SemanticModel);
				if (!methodSymbol.IsTransitionMethod(SemanticModel))
				{
					stateMachine = expression;
					break;
				}
			}

			transitions.Reverse();
			return transitions;
		}

		/// <summary>
		///   Decomposes the source and target states within the <paramref name="arguments" /> and adds all resulting transition to the
		///   list of <paramref name="transitions" />.
		/// </summary>
		private void AddTransitions(List<Transition> transitions, ArgumentListSyntax arguments)
		{
			var transition = new Transition();

			foreach (var argument in arguments.Arguments)
			{
				var parameter = argument.GetParameterSymbol(SemanticModel);
				switch (parameter.Name)
				{
					case "from":
						transition.SourceStates = RemoveArrayCreation(argument.Expression);
						transition.SourceLineNumber = argument.GetLineNumber();
						break;
					case "to":
						transition.TargetStates = RemoveArrayCreation(argument.Expression);
						transition.TargetLineNumber = argument.GetLineNumber();
						break;
					case "guard":
						transition.Guard = argument.Expression;
						transition.GuardLineNumber = argument.Expression.GetLineNumber();
						break;
					case "action":
						var lambda = argument.Expression as ParenthesizedLambdaExpressionSyntax;
						if (lambda == null)
						{
							transition.Action = (StatementSyntax)Syntax.ExpressionStatement(Syntax.InvocationExpression(argument.Expression));
							transition.ActionLineNumber = argument.Expression.GetLineNumber();
						}
						else
						{
							transition.ActionLineNumber = lambda.Body.GetLineNumber();
							var body = lambda.Body as StatementSyntax;
							if (body != null)
								transition.Action = (StatementSyntax)Visit(body);
							else
							{
								var transformedBody = TransformExpression((ExpressionSyntax)lambda.Body);
								if (transformedBody == null)
									transition.Action = (StatementSyntax)Syntax.ExpressionStatement(lambda.Body);
								else
									transition.Action = SyntaxFactory.Block(transformedBody);
							}
						}
						break;
					default:
						Assert.NotReached($"Unknown transition method parameter '{parameter}'.");
						break;
				}
			}

			if (transition.Guard == null)
				transition.Guard = (ExpressionSyntax)Syntax.TrueLiteralExpression();

			if (transition.Action == null)
				transition.Action = SyntaxFactory.Block();

			transitions.Add(transition);
		}

		/// <summary>
		///   If the expression is an array creation expression, makes the array creation implicit to optimize the code.
		/// </summary>
		private static SeparatedSyntaxList<ExpressionSyntax> RemoveArrayCreation(ExpressionSyntax expression)
		{
			var explicitCreation = expression as ArrayCreationExpressionSyntax;
			if (explicitCreation != null)
				return explicitCreation.Initializer.Expressions;

			var implicitCreation = expression as ImplicitArrayCreationExpressionSyntax;
			if (implicitCreation != null)
				return implicitCreation.Initializer.Expressions;

			return SyntaxFactory.SingletonSeparatedList(expression);
		}

		private struct Transition
		{
			public SeparatedSyntaxList<ExpressionSyntax> SourceStates;
			public SeparatedSyntaxList<ExpressionSyntax> TargetStates;
			public ExpressionSyntax Guard;
			public StatementSyntax Action;
			public int SourceLineNumber;
			public int TargetLineNumber;
			public int GuardLineNumber;
			public int ActionLineNumber;
		}
	}
}
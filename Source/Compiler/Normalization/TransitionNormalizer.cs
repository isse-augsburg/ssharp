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
	public sealed class TransitionNormalizer : SyntaxNormalizer
	{
		/// <summary>
		///   The name of the generated choice array variable.
		/// </summary>
		private readonly string _choiceArrayVariable = "choices".ToSynthesized();

		/// <summary>
		///   The name of the generated choice count variable.
		/// </summary>
		private readonly string _choiceCountVariable = "choiceCount".ToSynthesized();

		/// <summary>
		///   The global name of the <see cref="StateMachineExtensions" /> type.
		/// </summary>
		private readonly string _stateMachineExtensionsType = typeof(StateMachineExtensions).GetGlobalName();

		/// <summary>
		///   The name of the generated state machine variable.
		/// </summary>
		private readonly string _stateMachineVariable = "stateMachine".ToSynthesized();

		/// <summary>
		///   The writer that is used to generate the code.
		/// </summary>
		private readonly CodeWriter _writer = new CodeWriter();

		/// <summary>
		///   Represents the <see cref="StateMachine" /> type.
		/// </summary>
		private INamedTypeSymbol _stateMachineType;

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			_stateMachineType = Compilation.GetTypeSymbol(typeof(StateMachine));
			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="statement" />.
		/// </summary>
		public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax statement)
		{
			// If the expression statement is a sequence of 
			// invocation expressions of StateMachine.Transition() ->
			// member access expressions ->
			// invocation expressions StateMachine.Transition() -> 
			// member access expressions -> 
			// ... -> 
			// some other expression of type StateMachine
			// we have to replace all of that by the generated transition code

			if (statement.Expression.Kind() != SyntaxKind.InvocationExpression)
				return statement; // no nested rewritings

			var methodSymbol = statement.Expression.GetReferencedSymbol<IMethodSymbol>(SemanticModel);
			if (methodSymbol.Name != nameof(StateMachine.Transition) || !methodSymbol.ContainingType.Equals(_stateMachineType))
				return statement;

			ExpressionSyntax stateMachine;
			var transitions = DecomposeTransitionChain((InvocationExpressionSyntax)statement.Expression, out stateMachine);

			_writer.Clear();
			_writer.AppendLine("unsafe");
			_writer.AppendBlockStatement(() =>
			{
				_writer.AppendLine($"#line {stateMachine.GetLineNumber()}");
				_writer.AppendLine($"var {_stateMachineVariable} = {stateMachine.RemoveTrivia().ToFullString()};");

				_writer.AppendLine("#line hidden");
				_writer.AppendLine($"switch ({_stateMachineExtensionsType}.GetState({_stateMachineVariable}))");
				_writer.AppendBlockStatement(() => GenerateTransitionSections(transitions));
			});

			return SyntaxFactory.ParseStatement(_writer.ToString()).EnsureLineCount(statement);
		}

		/// <summary>
		///   Generates the sections of the main transition table switch statement.
		/// </summary>
		private void GenerateTransitionSections(List<Transition> transitions)
		{
			foreach (var transitionGroup in transitions.GroupBy(t => t.SourceState))
			{
				var transitionsInGroup = transitionGroup.ToArray();
				_writer.AppendLine($"case (int){transitionsInGroup[0].SourceStateExpression}:");
				_writer.AppendBlockStatement(() =>
				{
					if (transitionsInGroup.Length == 1)
						GenerateSingleTransition(transitionsInGroup[0]);
					else
						GenerateMultipleTransitions(transitionsInGroup);
				});
			}
		}

		/// <summary>
		///   Generates the code for multiple transitions from a source state.
		/// </summary>
		private void GenerateMultipleTransitions(Transition[] transitions)
		{
			_writer.AppendLine("#line hidden");
			_writer.AppendLine($"var {_choiceArrayVariable} = stackalloc int[{transitions.Length}];");
			_writer.AppendLine($"var {_choiceCountVariable} = 0;");
			_writer.NewLine();

			for (var i = 0; i < transitions.Length; ++i)
			{
				WriteLineNumber(transitions[i].GuardLineNumber);
				_writer.AppendLine($"if ({transitions[i].Guard.ToFullString()})");
				_writer.AppendLine("#line hidden");
				_writer.AppendLine($"{_choiceArrayVariable}[{_choiceCountVariable}++] = {i};");
			}

			_writer.NewLine();
			_writer.AppendLine(
				$"switch ({_choiceArrayVariable}[{_stateMachineExtensionsType}.GetChoice({_stateMachineVariable}).ChooseIndex({_choiceCountVariable})])");
			_writer.AppendBlockStatement(() =>
			{
				for (var i = 0; i < transitions.Length; ++i)
				{
					_writer.AppendLine($"case {i}:");
					_writer.AppendBlockStatement(() =>
					{
						GenerateTransitionEffect(transitions[i]);
						_writer.AppendLine("break;");
					});
				}
			});

			_writer.AppendLine("break;");
		}

		/// <summary>
		///   Generates the code for a single transition from a source state.
		/// </summary>
		private void GenerateSingleTransition(Transition transition)
		{
			WriteLineNumber(transition.GuardLineNumber);
			_writer.AppendLine($"if ({transition.Guard.ToFullString()})");
			_writer.AppendBlockStatement(() => GenerateTransitionEffect(transition));
			_writer.AppendLine("break;");
		}

		/// <summary>
		///   Generates the code for the effect of the <paramref name="transition" />.
		/// </summary>
		private void GenerateTransitionEffect(Transition transition)
		{
			_writer.AppendLine("#line hidden");
			_writer.AppendLine(
				$"{_stateMachineExtensionsType}.ChangeState({_stateMachineVariable}, (int){transition.TargetStateExpression.ToFullString()});");
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

			_writer.AppendLine("#line hidden");
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
		///   Collects all calls to <see cref="StateMachine.Transition{TSourceState,TTargetState}" /> within
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
				if (methodSymbol.Name != nameof(StateMachine.Transition) || !methodSymbol.ContainingType.Equals(_stateMachineType))
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

			ArgumentSyntax sources = null;
			ArgumentSyntax targets = null;

			foreach (var argument in arguments.Arguments)
			{
				var parameter = argument.GetParameterSymbol(SemanticModel);
				switch (parameter.Name)
				{
					case "from":
						sources = argument;
						break;
					case "to":
						targets = argument;
						break;
					case "guard":
						transition.Guard = argument.Expression;
						transition.GuardLineNumber = argument.Expression.GetLineNumber();
						break;
					case "action":
						transition.ActionLineNumber = argument.Expression.GetLineNumber();
						var lambda = argument.Expression as ParenthesizedLambdaExpressionSyntax;
						if (lambda == null)
							transition.Action = (StatementSyntax)Syntax.ExpressionStatement(Syntax.InvocationExpression(argument.Expression));
						else
						{
							var body = lambda.Body as StatementSyntax;
							if (body != null)
								transition.Action = body;
							else
								transition.Action = (StatementSyntax)Syntax.ExpressionStatement(lambda.Body);
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

			var sourceStates = sources.Descendants<MemberAccessExpressionSyntax>();
			var targetStates = targets.Descendants<MemberAccessExpressionSyntax>();

			foreach (var sourceState in sourceStates)
			{
				foreach (var targetState in targetStates)
				{
					transition.SourceStateExpression = sourceState;
					transition.TargetStateExpression = targetState;

					transition.SourceState = (int)SemanticModel.GetConstantValue(sourceState).Value;
					transition.TargetState = (int)SemanticModel.GetConstantValue(targetState).Value;

					transitions.Add(transition);
				}
			}
		}

		/// <summary>
		///   Represents a transition.
		/// </summary>
		private struct Transition
		{
			public int SourceState;
			public int TargetState;
			public ExpressionSyntax SourceStateExpression;
			public ExpressionSyntax TargetStateExpression;
			public ExpressionSyntax Guard;
			public StatementSyntax Action;
			public int GuardLineNumber;
			public int ActionLineNumber;
		}
	}
}
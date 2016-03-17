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

namespace SafetySharp.Compiler.Normalization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using CompilerServices;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using Microsoft.CodeAnalysis.Editing;
	using Modeling;
	using Roslyn.Symbols;
	using Roslyn.Syntax;
	using Utilities;

	/// <summary>
	///   Normalizes classes marked with <see cref="FaultEffectAttribute" />.
	/// </summary>
	public sealed class FaultEffectNormalizer : Normalizer
	{
		private readonly Dictionary<Tuple<IMethodSymbol, int>, string> _faultChoiceFields = new Dictionary<Tuple<IMethodSymbol, int>, string>();
		private readonly Dictionary<INamedTypeSymbol, INamedTypeSymbol[]> _faults = new Dictionary<INamedTypeSymbol, INamedTypeSymbol[]>();
		private readonly Dictionary<string, IMethodSymbol> _methodLookup = new Dictionary<string, IMethodSymbol>();
		private readonly string _tryActivate = $"global::{typeof(FaultHelper).FullName}.{nameof(FaultHelper.Activate)}";
		private readonly Dictionary<string, INamedTypeSymbol> _typeLookup = new Dictionary<string, INamedTypeSymbol>();
		private readonly string _undoActivation = $"global::{typeof(FaultHelper).FullName}.{nameof(FaultHelper.UndoActivation)}";

		/// <summary>
		///   Normalizes the syntax trees of the <see cref="Compilation" />.
		/// </summary>
		protected override Compilation Normalize()
		{
			var types = Compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>().ToArray();
			var components = types.Where(type => type.IsComponent(Compilation)).ToArray();
			var faultEffects = types.Where(type => type.IsFaultEffect(Compilation)).ToArray();

			foreach (var type in components.Concat(faultEffects))
			{
				var t = type;
				while (t != null && !_typeLookup.ContainsKey(t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
				{
					_typeLookup.Add(t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), t);
					t = t.BaseType;
				}
			}

			foreach (var component in components)
			{
				var faults = faultEffects.Where(faultEffect => faultEffect.BaseType.Equals(component)).ToArray();
				var faultTypes = faults.OrderBy(type => type.GetPriority(Compilation)).ThenBy(type => type.Name).ToArray();
				var nondeterministicFaults = faults.GroupBy(fault => fault.GetPriority(Compilation)).Where(group => group.Count() > 1).ToArray();

				_faults.Add(component, faultTypes);
				foreach (var method in component.GetFaultAffectableMethods(Compilation))
				{
					var methodKey = method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
					if (!_methodLookup.ContainsKey(methodKey))
						_methodLookup.Add(methodKey, method);

					foreach (var group in nondeterministicFaults)
					{
						var isNondeterministic = group.Count(f => f.GetMembers().OfType<IMethodSymbol>().Any(m => m.Overrides(method))) > 1;
						if (!isNondeterministic)
							continue;

						var key = Tuple.Create(method, group.Key);
						var fieldName = Guid.NewGuid().ToString().Replace("-", "_").ToSynthesized();

						_faultChoiceFields.Add(key, fieldName);
						AddFaultChoiceField(component, fieldName);
					}
				}

				AddRuntimeTypeField(component);
			}

			return base.Normalize();
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax declaration)
		{
			var classSymbol = declaration.GetTypeSymbol(SemanticModel);
			if (!classSymbol.IsFaultEffect(SemanticModel))
			{
				declaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(declaration);

				if (classSymbol.IsComponent(SemanticModel))
					declaration = ChangeComponentBaseType(classSymbol, declaration);

				return declaration;
			}

			AddFaultField(classSymbol);

			declaration = (ClassDeclarationSyntax)base.VisitClassDeclaration(declaration);
			return ChangeFaultEffectBaseType(classSymbol, declaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="declaration" />.
		/// </summary>
		public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax declaration)
		{
			var originalDeclaration = declaration;
			var methodSymbol = declaration.GetMethodSymbol(SemanticModel);

			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel) || !methodSymbol.IsOverride)
				return declaration;

			var memberAccess = Syntax.MemberAccessExpression(Syntax.BaseExpression(), methodSymbol.Name);
			var invocation = Syntax.InvocationExpression(memberAccess, CreateInvocationArguments(methodSymbol.Parameters));

			declaration = declaration.WithBody(CreateBody(methodSymbol, declaration.Body, invocation));
			return declaration.EnsureLineCount(originalDeclaration);
		}

		/// <summary>
		///   Normalizes the <paramref name="accessor" />.
		/// </summary>
		public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax accessor)
		{
			var originalAccessor = accessor;
			var methodSymbol = accessor.GetMethodSymbol(SemanticModel);

			if (!methodSymbol.ContainingType.IsFaultEffect(SemanticModel) || !methodSymbol.IsOverride)
				return accessor;

			SyntaxNode baseExpression;
			if (((IPropertySymbol)methodSymbol.AssociatedSymbol).IsIndexer)
			{
				var parameterCount = methodSymbol.Parameters.Length - (accessor.Kind() != SyntaxKind.GetAccessorDeclaration ? 1 : 0);
				var parameters = methodSymbol.Parameters.Take(parameterCount);
				baseExpression = Syntax.ElementAccessExpression(Syntax.BaseExpression(), CreateInvocationArguments(parameters));
			}
			else
				baseExpression = Syntax.MemberAccessExpression(Syntax.BaseExpression(), methodSymbol.GetPropertySymbol().Name);

			if (accessor.Kind() != SyntaxKind.GetAccessorDeclaration)
				baseExpression = Syntax.AssignmentStatement(baseExpression, Syntax.IdentifierName("value"));

			accessor = accessor.WithBody(CreateBody(methodSymbol, accessor.Body, baseExpression));
			return accessor.EnsureLineCount(originalAccessor);
		}

		/// <summary>
		///   Creates the arguments for a delegate invocation.
		/// </summary>
		private static IEnumerable<ArgumentSyntax> CreateInvocationArguments(IEnumerable<IParameterSymbol> parameters)
		{
			return parameters.Select(parameter =>
			{
				var argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Name));

				switch (parameter.RefKind)
				{
					case RefKind.Ref:
						return argument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword));
					case RefKind.Out:
						return argument.WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));
					case RefKind.None:
						return argument;
					default:
						Assert.NotReached("Unsupported ref kind.");
						return null;
				}
			});
		}

		/// <summary>
		///   Creates a deterministic or nondeterministic fault effect body.
		/// </summary>
		private BlockSyntax CreateBody(IMethodSymbol method, BlockSyntax originalBody, SyntaxNode baseEffect)
		{
			var lineAdjustedOriginalBody = originalBody.AppendLineDirective(-1).PrependLineDirective(originalBody.GetLineNumber());
			var componentType = _typeLookup[method.ContainingType.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)];
			var faultEffectType = _typeLookup[method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)];
			var faults = _faults[componentType];
			var baseStatements = !method.ReturnsVoid
				? new[] { Syntax.ReturnStatement(baseEffect) }
				: new[] { Syntax.ExpressionStatement(baseEffect), Syntax.ReturnStatement() };

			IMethodSymbol methodSymbol;
			BlockSyntax body = null;

			if (_methodLookup.TryGetValue(method.OverriddenMethod.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), out methodSymbol))
			{
				var priorityFaults = faults.Where(fault => fault.GetPriority(Compilation) == method.ContainingType.GetPriority(Compilation)).ToArray();
				var overridingEffects = priorityFaults.Where(f => f.GetMembers().OfType<IMethodSymbol>().Any(m => m.Overrides(methodSymbol))).ToArray();
				var overrideCount = overridingEffects.Length;

				if (overrideCount > 1)
				{
					var fieldName = _faultChoiceFields[Tuple.Create(methodSymbol, priorityFaults[0].GetPriority(Compilation))];
					var effectIndex = Array.IndexOf(priorityFaults, faultEffectType);
					var choiceField = Syntax.MemberAccessExpression(Syntax.ThisExpression(), fieldName);

					var levelCondition = Syntax.ValueNotEqualsExpression(choiceField, Syntax.LiteralExpression(effectIndex));
					var ifStatement = Syntax.IfStatement(levelCondition, baseStatements).NormalizeWhitespace().WithTrailingNewLines(1);

					if (overridingEffects.Last().Equals(faultEffectType))
					{
						var levelChoiceVariable = "levelChoice".ToSynthesized();
						var levelCountVariable = "levelCount".ToSynthesized();

						var writer = new CodeWriter();
						writer.AppendLine("unsafe");
						writer.AppendBlockStatement(() =>
						{
							writer.AppendLine($"var {levelChoiceVariable} = stackalloc int[{overrideCount}];");
							writer.AppendLine($"var {levelCountVariable} = 0;");

							for (var i = 0; i < overrideCount; ++i)
							{
								var effectType = overridingEffects[i].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
								var index = Array.IndexOf(priorityFaults, overridingEffects[i]);

								writer.AppendLine($"if ({_tryActivate}((({effectType})this).{"fault".ToSynthesized()}))");
								writer.IncreaseIndent();
								writer.AppendLine($"{levelChoiceVariable}[{levelCountVariable}++] = {index};");
								writer.DecreaseIndent();
								writer.NewLine();
							}

							writer.AppendLine($"{fieldName} = {levelCountVariable} == 0 ? - 1 : {levelChoiceVariable}[ChooseIndex({levelCountVariable})];");
						});

						var selectionStatement = SyntaxFactory.ParseStatement(writer.ToString());
						body = SyntaxFactory.Block(selectionStatement, (StatementSyntax)ifStatement, lineAdjustedOriginalBody);
					}
					else
						body = SyntaxFactory.Block((StatementSyntax)ifStatement, lineAdjustedOriginalBody);
				}
			}

			if (body == null)
			{
				var writer = new CodeWriter();
				writer.AppendLine($"if (!{_tryActivate}(this.{"fault".ToSynthesized()}))");
				writer.AppendBlockStatement(() =>
				{
					// Optimization: If we're normalizing a non-void returning method without ref/out parameters and
					// the fault effect simply returns a constant value of primitive type, we generate code to check whether the non-fault
					// value for the case that the fault is not activated (which is always the first case) actually differs 
					// from the constant value returned by the fault effect when the fault is activated. If both values are
					// the same, the activation of the fault will have no effect, so we can undo it, reducing the number
					// of transitions that have to be checked
					var signatureAllowsOptimization =
						!method.ReturnsVoid && CanBeCompared(method.ReturnType) && method.Parameters.All(parameter => parameter.RefKind == RefKind.None);
					var faultEffectReturn = originalBody.Statements.Count == 1 ? originalBody.Statements[0] as ReturnStatementSyntax : null;
					var isConstantValue = faultEffectReturn != null && SemanticModel.GetConstantValue(faultEffectReturn.Expression).HasValue;

					if (signatureAllowsOptimization && isConstantValue)
					{
						writer.AppendLine($"var {"tmp".ToSynthesized()} = {baseEffect.ToFullString()};");
						writer.AppendLine($"if ({"tmp".ToSynthesized()} == {faultEffectReturn.Expression.ToFullString()})");
						writer.AppendBlockStatement(() => { writer.AppendLine($"{_undoActivation}(this.{"fault".ToSynthesized()});"); });
						writer.AppendLine($"return {"tmp".ToSynthesized()};");
					}
					else
					{
						foreach (var statement in baseStatements)
							writer.AppendLine(statement.NormalizeWhitespace().ToFullString());
					}
				});

				writer.NewLine();
				body = SyntaxFactory.Block(SyntaxFactory.ParseStatement(writer.ToString()), lineAdjustedOriginalBody);
			}

			return body.PrependLineDirective(-1);
		}

		/// <summary>
		///   Gets the value indicating whether <paramref name="typeSymbol" /> can be compared using the <c>==</c> operator.
		/// </summary>
		public static bool CanBeCompared(ITypeSymbol typeSymbol)
		{
			if (typeSymbol.TypeKind == TypeKind.Enum)
				return true;

			switch (typeSymbol.SpecialType)
			{
				case SpecialType.System_Enum:
				case SpecialType.System_Void:
				case SpecialType.System_Boolean:
				case SpecialType.System_Char:
				case SpecialType.System_SByte:
				case SpecialType.System_Byte:
				case SpecialType.System_Int16:
				case SpecialType.System_UInt16:
				case SpecialType.System_Int32:
				case SpecialType.System_UInt32:
				case SpecialType.System_Int64:
				case SpecialType.System_UInt64:
				case SpecialType.System_Decimal:
				case SpecialType.System_Single:
				case SpecialType.System_Double:
				case SpecialType.System_String:
				case SpecialType.System_IntPtr:
				case SpecialType.System_UIntPtr:
				case SpecialType.System_Nullable_T:
				case SpecialType.System_DateTime:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		///   Changes the base type of the fault effect declaration based on its location in the fault effect list.
		/// </summary>
		private ClassDeclarationSyntax ChangeFaultEffectBaseType(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
		{
			var baseTypeName = classSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var faultEffectSymbol = _typeLookup[classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)];
			var faultIndex = Array.IndexOf(_faults[_typeLookup[baseTypeName]], faultEffectSymbol);
			if (faultIndex == 0)
				return classDeclaration;

			var baseType = Syntax.TypeExpression(_faults[_typeLookup[baseTypeName]][faultIndex - 1]).WithTrivia(classDeclaration.BaseList.Types[0]);
			var baseTypes = SyntaxFactory.SingletonSeparatedList((BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)baseType));
			var baseList = SyntaxFactory.BaseList(classDeclaration.BaseList.ColonToken, baseTypes).WithTrivia(classDeclaration.BaseList);
			return classDeclaration.WithBaseList(baseList);
		}

		/// <summary>
		///   Changes the base type of the derived component declaration.
		/// </summary>
		private ClassDeclarationSyntax ChangeComponentBaseType(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
		{
			if (classSymbol.BaseType.Equals(SemanticModel.GetTypeSymbol<Component>()))
				return classDeclaration;

			var baseTypeName = classSymbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			INamedTypeSymbol[] faults;

			if (!_faults.TryGetValue(_typeLookup[baseTypeName], out faults) || faults.Length == 0)
				return classDeclaration;

			var baseType = Syntax.TypeExpression(faults[faults.Length - 1]).WithTrivia(classDeclaration.BaseList.Types[0]);
			var baseTypes = SyntaxFactory.SingletonSeparatedList((BaseTypeSyntax)SyntaxFactory.SimpleBaseType((TypeSyntax)baseType));
			var baseList = SyntaxFactory.BaseList(classDeclaration.BaseList.ColonToken, baseTypes).WithTrivia(classDeclaration.BaseList);
			return classDeclaration.WithBaseList(baseList);
		}

		/// <summary>
		///   Adds the runtime type field to the component symbol.
		/// </summary>
		private void AddRuntimeTypeField(INamedTypeSymbol classSymbol)
		{
			var className = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var faults = _faults[_typeLookup[className]];
			var runtimeType = faults.Length > 0 ? faults[faults.Length - 1] : classSymbol;
			var typeofExpression = SyntaxFactory.TypeOfExpression((TypeSyntax)Syntax.TypeExpression((runtimeType)));
			var field = Syntax.FieldDeclaration(
				name: "runtimeType".ToSynthesized(),
				type: Syntax.TypeExpression<Type>(Compilation),
				accessibility: Accessibility.Private,
				modifiers: DeclarationModifiers.Static | DeclarationModifiers.ReadOnly,
				initializer: typeofExpression);

			field = Syntax.MarkAsNonDebuggerBrowsable(field);
			field = field.NormalizeWhitespace();

			AddMembers(classSymbol, (MemberDeclarationSyntax)field);
		}

		/// <summary>
		///   Adds the fault choice field to the component symbol.
		/// </summary>
		private void AddFaultChoiceField(INamedTypeSymbol classSymbol, string fieldName)
		{
			var field = Syntax.FieldDeclaration(
				name: fieldName,
				type: Syntax.TypeExpression(SpecialType.System_Int32),
				accessibility: Accessibility.Internal);

			field = Syntax.MarkAsNonDebuggerBrowsable(field);
			field = Syntax.AddAttribute<NonSerializableAttribute>(field);
			field = Syntax.AddAttribute<CompilerGeneratedAttribute>(field);
			field = field.NormalizeWhitespace();

			AddMembers(classSymbol, (MemberDeclarationSyntax)field);
		}

		/// <summary>
		///   Adds the fault field to the fault effect symbol.
		/// </summary>
		private void AddFaultField(INamedTypeSymbol classSymbol)
		{
			var faultField = Syntax.FieldDeclaration(
				name: "fault".ToSynthesized(),
				type: Syntax.TypeExpression<Fault>(SemanticModel),
				accessibility: Accessibility.Internal);

			faultField = Syntax.MarkAsNonDebuggerBrowsable(faultField);
			faultField = Syntax.AddAttribute<HiddenAttribute>(faultField);
			faultField = Syntax.AddAttribute<NonDiscoverableAttribute>(faultField);
			faultField = Syntax.AddAttribute<CompilerGeneratedAttribute>(faultField);
			faultField = faultField.NormalizeWhitespace();

			AddMembers(classSymbol, (MemberDeclarationSyntax)faultField);
		}
	}
}
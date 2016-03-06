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

namespace SafetySharp.Compiler.Roslyn.Symbols
{
	using System.Linq;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Provides extension methods for working with <see cref="IMethodSymbol" /> instances.
	/// </summary>
	public static class MethodSymbolExtensions
	{
		/// <summary>
		///   Gets a value indicating whether the <paramref name="methodSymbol" /> can be affected by fault effects.
		/// </summary>
		/// <param name="methodSymbol">The symbol of the method that should be checked.</param>
		/// <param name="semanticModel">The semantic model that is used to resolve type information.</param>
		[Pure]
		public static bool CanBeAffectedByFaults([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));

			// Faults can only affect non-abstract methods that can be overwritten
			if (methodSymbol.IsAbstract || methodSymbol.IsSealed || (!methodSymbol.IsVirtual && !methodSymbol.IsOverride))
				return false;

			// Faults affect only component methods
			return methodSymbol.IsProvidedPort(semanticModel) ||
				   methodSymbol.IsRequiredPort(semanticModel) ||
				   methodSymbol.IsUpdateMethod(semanticModel);
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="methodSymbol" /> can be affected by fault effects.
		/// </summary>
		/// <param name="methodSymbol">The symbol of the method that should be checked.</param>
		/// <param name="compilation">The compilation the method belongs to.</param>
		[Pure]
		public static bool CanBeAffectedByFaults([NotNull] this IMethodSymbol methodSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));

			// Faults can only affect non-abstract methods that can be overwritten
			if (methodSymbol.IsAbstract || methodSymbol.IsSealed || (!methodSymbol.IsVirtual && !methodSymbol.IsOverride))
				return false;

			// Faults affect only component methods
			return methodSymbol.IsProvidedPort(compilation) ||
				   methodSymbol.IsRequiredPort(compilation) ||
				   methodSymbol.IsUpdateMethod(compilation);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> overrides <paramref name="overriddenMethod" />.
		/// </summary>
		/// <param name="methodSymbol">The symbol of the methodSymbol that should be checked.</param>
		/// <param name="overriddenMethod">The symbol of the methodSymbol that should be overridden.</param>
		[Pure]
		public static bool Overrides([NotNull] this IMethodSymbol methodSymbol, [NotNull] IMethodSymbol overriddenMethod)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(overriddenMethod, nameof(overriddenMethod));

			if (methodSymbol.Equals(overriddenMethod))
				return true;

			if (!methodSymbol.IsOverride || methodSymbol.OverriddenMethod == null)
				return false;

			if (methodSymbol.OverriddenMethod.OriginalDefinition.Equals(overriddenMethod))
				return true;

			return methodSymbol.OverriddenMethod.Overrides(overriddenMethod);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> overrides <see cref="Component.Update()" />
		///   within the context of the <paramref name="compilation" />.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsUpdateMethod([NotNull] this IMethodSymbol methodSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(compilation, nameof(compilation));

			var updateMethod = compilation
				.GetTypeSymbol<Component>()
				.GetMembers("Update")
				.OfType<IMethodSymbol>()
				.Single(method => method.Parameters.Length == 0 && method.ReturnsVoid);

			return methodSymbol.Overrides(updateMethod);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> overrides <see cref="Component.Update()" /> within the
		///   context of the <paramref name="semanticModel" />.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsUpdateMethod([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return methodSymbol.IsUpdateMethod(semanticModel.Compilation);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a required port of a S# component or interface.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsRequiredPort([NotNull] this IMethodSymbol methodSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(compilation, nameof(compilation));

			if (methodSymbol.IsStatic)
				return false;

			var validKind = methodSymbol.MethodKind == MethodKind.Ordinary ||
							methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
							methodSymbol.MethodKind == MethodKind.PropertyGet ||
							methodSymbol.MethodKind == MethodKind.PropertySet;

			if (!validKind)
				return false;

			if (!methodSymbol.ContainingType.IsDerivedFrom(compilation.GetTypeSymbol<IComponent>()))
				return false;

			switch (methodSymbol.ContainingType.TypeKind)
			{
				case TypeKind.Class:
					if (methodSymbol.ContainingType.HasAttribute<FaultEffectAttribute>(compilation))
						return false;

					if (methodSymbol.IsPropertyAccessor())
					{
						var propertySymbol = methodSymbol.GetPropertySymbol();
						return propertySymbol.IsExtern || propertySymbol.HasAttribute<RequiredAttribute>(compilation);
					}

					return methodSymbol.IsExtern || methodSymbol.HasAttribute<RequiredAttribute>(compilation);
				case TypeKind.Interface:
					if (methodSymbol.IsPropertyAccessor())
						return methodSymbol.GetPropertySymbol().HasAttribute<RequiredAttribute>(compilation);

					return methodSymbol.HasAttribute<RequiredAttribute>(compilation);
				default:
					return false;
			}
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a required port of a S# component or interface.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsRequiredPort([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return methodSymbol.IsRequiredPort(semanticModel.Compilation);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a provided port of a S# component or interface.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsProvidedPort([NotNull] this IMethodSymbol methodSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(compilation, nameof(compilation));

			if (methodSymbol.IsStatic)
				return false;

			var validKind = methodSymbol.MethodKind == MethodKind.Ordinary ||
							methodSymbol.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
							methodSymbol.MethodKind == MethodKind.PropertyGet ||
							methodSymbol.MethodKind == MethodKind.PropertySet;

			if (!validKind)
				return false;

			if (!methodSymbol.ContainingType.IsDerivedFrom(compilation.GetTypeSymbol<IComponent>()))
				return false;

			switch (methodSymbol.ContainingType.TypeKind)
			{
				case TypeKind.Class:
					if (methodSymbol.ContainingType.HasAttribute<FaultEffectAttribute>(compilation))
						return false;

					if (methodSymbol.IsPropertyAccessor())
					{
						var propertySymbol = methodSymbol.GetPropertySymbol();
						return !propertySymbol.IsExtern && !propertySymbol.HasAttribute<RequiredAttribute>(compilation);
					}

					return !methodSymbol.IsExtern &&
						   !methodSymbol.HasAttribute<RequiredAttribute>(compilation) &&
						   !methodSymbol.IsUpdateMethod(compilation);
				case TypeKind.Interface:
					if (methodSymbol.IsPropertyAccessor())
						return methodSymbol.GetPropertySymbol().HasAttribute<ProvidedAttribute>(compilation);

					return methodSymbol.HasAttribute<ProvidedAttribute>(compilation);
				default:
					return false;
			}
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a provided port of a S# component or interface.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsProvidedPort([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return methodSymbol.IsProvidedPort(semanticModel.Compilation);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a <c>StateMachine{TState}.Transition</c> method.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsTransitionMethod([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return methodSymbol.Name == nameof(StateMachine<int>.Transition) &&
				   methodSymbol.ContainingType.OriginalDefinition.Equals(semanticModel.GetTypeSymbol(typeof(StateMachine<>)));
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a fault effect of a S# fault.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="compilation">The compilation that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsFaultEffect([NotNull] this IMethodSymbol methodSymbol, [NotNull] Compilation compilation)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(compilation, nameof(compilation));

			if (methodSymbol.IsStatic)
				return false;

			if (methodSymbol.MethodKind != MethodKind.Ordinary && methodSymbol.MethodKind != MethodKind.ExplicitInterfaceImplementation)
				return false;

			if (!methodSymbol.ContainingType.IsDerivedFrom(compilation.GetTypeSymbol<IComponent>()))
				return false;

			if (methodSymbol.IsUpdateMethod(compilation))
				return false;

			return true;
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a fault effect of a S# fault.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsFaultEffect([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			return methodSymbol.IsFaultEffect(semanticModel.Compilation);
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents the <see cref="Component.Bind(string,string)" /> or
		///   <see cref="Component.Bind{T}(string,string)" /> method.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		/// <param name="semanticModel">The semantic model that should be used to resolve symbol information.</param>
		[Pure]
		public static bool IsBindMethod([NotNull] this IMethodSymbol methodSymbol, [NotNull] SemanticModel semanticModel)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.NotNull(semanticModel, nameof(semanticModel));

			if (methodSymbol.Name != "Bind")
				return false;

			return methodSymbol.ContainingType.Equals(semanticModel.GetTypeSymbol<Component>());
		}

		/// <summary>
		///   Checks whether <paramref name="methodSymbol" /> represents a built-in operator.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		[Pure]
		public static bool IsBuiltInOperator([NotNull] this IMethodSymbol methodSymbol)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));

			return methodSymbol.ContainingType.SpecialType == SpecialType.System_Byte ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Int16 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Int32 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Int64 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_SByte ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_UInt16 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_UInt32 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_UInt64 ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Decimal ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Single ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Double ||
				   methodSymbol.ContainingType.SpecialType == SpecialType.System_Boolean ||
				   methodSymbol.ContainingType.TypeKind == TypeKind.Enum;
		}

		/// <summary>
		///   Gets a value indicating whether <paramref name="methodSymbol" /> represents a getter or setter accessor of an
		///   <see cref="IPropertySymbol" />.
		/// </summary>
		/// <param name="methodSymbol">The method symbol that should be checked.</param>
		public static bool IsPropertyAccessor([NotNull] this IMethodSymbol methodSymbol)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			return methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet;
		}

		/// <summary>
		///   Gets the <see cref="IPropertySymbol" /> of which <paramref name="methodSymbol" /> represents the getter or setter
		///   accessor.
		/// </summary>
		/// <param name="methodSymbol">The method symbol the property symbol should be returned for.</param>
		public static IPropertySymbol GetPropertySymbol([NotNull] this IMethodSymbol methodSymbol)
		{
			Requires.NotNull(methodSymbol, nameof(methodSymbol));
			Requires.That(methodSymbol.IsPropertyAccessor(), nameof(methodSymbol), "Method must be a property accessor.");

			return (IPropertySymbol)methodSymbol.AssociatedSymbol;
		}

		/// <summary>
		///   Checks whether the two <see cref="IMethodSymbol" />s are signature-compatible.
		/// </summary>
		/// <param name="methodSymbol1">The first method symbol that should be checked.</param>
		/// <param name="methodSymbol2">The second method symbol that should be checked.</param>
		public static bool IsSignatureCompatibleTo([NotNull] this IMethodSymbol methodSymbol1, [NotNull] IMethodSymbol methodSymbol2)
		{
			Requires.NotNull(methodSymbol1, nameof(methodSymbol1));
			Requires.NotNull(methodSymbol2, nameof(methodSymbol2));

			if (methodSymbol1.TypeParameters.Length != 0 || methodSymbol2.TypeParameters.Length != 0)
				return false;

			return methodSymbol1.ReturnType.Equals(methodSymbol2.ReturnType)
				   && methodSymbol1.Parameters.Length == methodSymbol2.Parameters.Length
				   && methodSymbol1.Parameters
								   .Zip(methodSymbol2.Parameters, (p1, p2) => p1.Type.Equals(p2.Type) && p1.RefKind == p2.RefKind)
								   .All(b => b);
		}
	}
}
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
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   Filters <see cref="IMethodSymbol" /> instances.
	/// </summary>
	public static class MethodSymbolFilter
	{
		/// <summary>
		///   Removes all methods from the two sets that don't have a signature-compatible equivalent in the other set.
		/// </summary>
		/// <param name="methodSet1">The first set of methods that should be filtered.</param>
		/// <param name="methodSet2">The second set of methods that should be filtered.</param>
		public static void Filter(HashSet<IMethodSymbol> methodSet1, HashSet<IMethodSymbol> methodSet2)
		{
			Requires.NotNull(methodSet1, nameof(methodSet1));
			Requires.NotNull(methodSet2, nameof(methodSet2));

			var tmp1 = new HashSet<IMethodSymbol>(methodSet1);
			var tmp2 = new HashSet<IMethodSymbol>(methodSet2);

			foreach (var port in tmp1)
			{
				if (!tmp2.Any(otherPort => otherPort.IsSignatureCompatibleTo(port)))
					methodSet1.Remove(port);
			}

			foreach (var port in tmp2)
			{
				if (!methodSet1.Any(otherPort => otherPort.IsSignatureCompatibleTo(port)))
					methodSet2.Remove(port);
			}
		}

		/// <summary>
		///   Removes all methods from the set that are not compatible to the signature of the <pramref name="signature" />.
		/// </summary>
		/// <param name="methods">The set of methods that should be filtered.</param>
		/// <param name="delegateType">The signature the methods must be compatible to.</param>
		public static void Filter(HashSet<IMethodSymbol> methods, INamedTypeSymbol delegateType)
		{
			Requires.NotNull(methods, nameof(methods));
			Requires.NotNull(delegateType, nameof(delegateType));
			Requires.That(delegateType.TypeKind == TypeKind.Delegate, "Expected a delegate type.");

			methods.RemoveWhere(port => !port.IsSignatureCompatibleTo(delegateType.DelegateInvokeMethod));
		}
	}
}
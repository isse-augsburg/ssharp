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

namespace SafetySharp.Compiler.Roslyn
{
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.CodeAnalysis;
	using Symbols;
	using Utilities;

	/// <summary>
	///   Matches the signatures of ports.
	/// </summary>
	public static class PortSignatureMatcher
	{
		/// <summary>
		///   Removes all ports from the two sets that don't have a signature-compatible equivalent in the other set.
		/// </summary>
		/// <param name="ports1">The first set of ports that should be filtered.</param>
		/// <param name="ports2">The second set of ports that should be filtered.</param>
		public static void Filter(ref HashSet<IMethodSymbol> ports1, ref HashSet<IMethodSymbol> ports2)
		{
			Requires.NotNull(ports1, nameof(ports1));
			Requires.NotNull(ports2, nameof(ports2));

			var result1 = new HashSet<IMethodSymbol>();
			var result2 = new HashSet<IMethodSymbol>();

			foreach (var port in ports1)
			{
				if (ports2.Any(otherPort => otherPort.IsSignatureCompatibleTo(port)))
					result1.Add(port);
			}

			foreach (var port in ports2)
			{
				if (result1.Any(otherPort => otherPort.IsSignatureCompatibleTo(port)))
					result2.Add(port);
			}

			ports1 = result1;
			ports2 = result2;
		}


		/// <summary>
		///   Removes all ports from the set that are not compatible to the <paramref name="signature"/>.
		/// </summary>
		/// <param name="ports">The set of ports that should be filtered.</param>
		/// <param name="signature">The signature the ports must be compatible to.</param>
		public static void Filter(HashSet<IMethodSymbol> ports, INamedTypeSymbol signature)
		{
			Requires.NotNull(ports, nameof(ports));
			Requires.NotNull(signature, nameof(signature));
			Requires.That(signature.DelegateInvokeMethod != null, nameof(signature), "Expected a delegate type.");

			ports.RemoveWhere(port => !port.IsSignatureCompatibleTo(signature.DelegateInvokeMethod));
		}
	}
}
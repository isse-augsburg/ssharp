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

namespace Tests.Utilities
{
	using System;
	using System.Text;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;

	/// <summary>
	///   Raised when invalid C# code is detected or compilation of a dynamic C# project failed.
	/// </summary>
	public class CSharpException : TestException
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="message">The format message of the exception.</param>
		/// <param name="args">The format arguments.</param>
		[StringFormatMethod("message")]
		public CSharpException(string message, params object[] args)
			: base(message, args)
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="diagnostics">The diagnostics that should be appended to the exception message.</param>
		/// <param name="message">The format message of the exception.</param>
		/// <param name="args">The format arguments.</param>
		[StringFormatMethod("message")]
		public CSharpException(Diagnostic[] diagnostics, string message, params object[] args)
			: base("{0}\n\n{1}", String.Format(message, args), ToString(diagnostics))
		{
		}

		/// <summary>
		///   Gets a string representation for the <paramref name="diagnostics" />.
		/// </summary>
		/// <param name="diagnostics">The diagnostics that should be returned as a string.</param>
		private static string ToString(Diagnostic[] diagnostics)
		{
			var builder = new StringBuilder();

			foreach (var diagnostic in diagnostics)
				Tests.Write(builder, diagnostic);

			return builder.ToString();
		}
	}
}
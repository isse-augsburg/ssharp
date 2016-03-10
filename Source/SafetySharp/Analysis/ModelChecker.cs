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

	/// <summary>
	///   Represents a base class for external model checker tools.
	/// </summary>
	public abstract class ModelChecker
	{
		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten;

		/// <summary>
		///   Forwards the output <paramref name="message" />.
		/// </summary>
		/// <param name="message">The message that should be output.</param>
		/// <param name="color">
		///   The color of the output that should be written. <c>null</c> indicates that the default color should be used.
		/// </param>
		protected internal void Output(string message, ConsoleColor? color = null)
		{
			if (color != null)
				Console.ForegroundColor = color.Value;

			Console.WriteLine(message);
			Console.ResetColor();

			OutputWritten?.Invoke(message);
		}

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public abstract AnalysisResult Check(Model model, Formula formula);

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public abstract AnalysisResult CheckInvariant(Model model, Formula invariant);
	}
}
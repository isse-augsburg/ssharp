// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.AnalysisModelTraverser
{
	using System;
	using AnalysisModel;
	using ExecutableModel;
	using Utilities;

	/// <summary>
	///   Provides details about an unhandled exception that was thrown during model checking.
	/// </summary>
	public class AnalysisException : Exception
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="exception">The unhandled exception that was thrown during model checkinig.</param>
		/// <param name="counterExample">The path through the model that leads to the <paramref name="exception" /> being thrown.</param>
		public AnalysisException(Exception exception, CounterExample counterExample)
			: base($"Error: An unhandled exception of type '{exception.GetType().FullName}' was " +
				   $"thrown during model checking: {exception.Message}", exception)
		{
			Requires.NotNull(exception, nameof(exception));
			CounterExample = counterExample;
		}

		/// <summary>
		///   Gets the path through the model that leads to the <see cref="Exception.InnerException" /> being thrown, if any.
		/// </summary>
		public CounterExample CounterExample { get; }
	}
}
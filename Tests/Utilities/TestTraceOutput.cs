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

namespace Tests.Utilities
{
	using JetBrains.Annotations;
	using SafetySharp.Utilities;
	using Xunit.Abstractions;

	/// <summary>
	///   Represents test tracing output for debugging purposes.
	/// </summary>
	public class TestTraceOutput
	{
		/// <summary>
		///   Writes to the test output stream.
		/// </summary>
		private readonly ITestOutputHelper _output;

		/// <summary>
		///   Indicates whether test output tracing should be enabled.
		/// </summary>
		private readonly bool EnableTracing = false;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="output">The stream that should be used to write the test output.</param>
		public TestTraceOutput(ITestOutputHelper output = null)
		{
			_output = output;
		}

		/// <summary>
		///   Writes the formatted <paramref name="message" /> to the test output stream.
		/// </summary>
		/// <param name="message">The formatted message that should be written.</param>
		/// <param name="args">The format arguments of the message.</param>
		[StringFormatMethod("message")]
		public void Log(string message, params object[] args)
		{
			Requires.NotNull(_output, "A test output helper must be provided via the constructor.");
			_output.WriteLine(message, args);
		}

		/// <summary>
		///   Writes the formatted <paramref name="message" /> to the test output stream if tracing is enabled.
		/// </summary>
		/// <param name="message">The formatted message that should be written.</param>
		/// <param name="args">The format arguments of the message.</param>
		[StringFormatMethod("message")]
		public void Trace(string message, params object[] args)
		{
			Requires.NotNull(_output, "A test output helper must be provided via the constructor.");

			if (EnableTracing)
				_output.WriteLine(message, args);
		}
	}
}
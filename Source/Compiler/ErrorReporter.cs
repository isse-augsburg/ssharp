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

namespace SafetySharp.Compiler
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using JetBrains.Annotations;
	using Microsoft.CodeAnalysis;
	using Utilities;

	/// <summary>
	///   A base class for reporting compilation errors; by default, errors are reported to the standard console.
	/// </summary>
	public class ErrorReporter
	{
		/// <summary>
		///   Gets or sets a value indicating whether all informational compiler output should be suppressed.
		/// </summary>
		public bool Silent { get; set; }

		/// <summary>
		///   Logs an application error.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		[StringFormatMethod("message")]
		public virtual void Error([NotNull] string message, params object[] arguments)
		{
			Requires.NotNull(message, nameof(message));
			Log(DiagnosticSeverity.Error, message, arguments);
		}

		/// <summary>
		///   Logs an application warning.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		[StringFormatMethod("message")]
		public virtual void Warn([NotNull] string message, params object[] arguments)
		{
			Requires.NotNull(message, nameof(message));
			Log(DiagnosticSeverity.Warning, message, arguments);
		}

		/// <summary>
		///   Logs an informational message.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		[StringFormatMethod("message")]
		public virtual void Info([NotNull] string message, params object[] arguments)
		{
			Requires.NotNull(message, nameof(message));
			Log(DiagnosticSeverity.Info, message, arguments);
		}

		/// <summary>
		///   Reports <paramref name="diagnostic" /> depending on its severity. If <paramref name="errorsOnly" /> is <c>true</c>, only
		///   error diagnostics are reported.
		/// </summary>
		/// <param name="diagnostic">The diagnostic that should be reported.</param>
		/// <param name="errorsOnly">Indicates whether error diagnostics should be reported exclusively.</param>
		internal virtual void Report(Diagnostic diagnostic, bool errorsOnly)
		{
			switch (diagnostic.Severity)
			{
				case DiagnosticSeverity.Error:
					Error("{0}", diagnostic);
					break;
				case DiagnosticSeverity.Warning:
					if (!errorsOnly)
						Warn("{0}", diagnostic);
					break;
				case DiagnosticSeverity.Info:
				case DiagnosticSeverity.Hidden:
					if (!errorsOnly)
						Info("{0}", diagnostic);
					break;
				default:
					Assert.NotReached("Unknown diagnostic severity.");
					break;
			}
		}

		/// <summary>
		///   Reports all <paramref name="diagnostics" /> depending on their severities. If <paramref name="errorsOnly" /> is
		///   <c>true</c>, only error diagnostics are reported. The function returns <c>false</c> when at least one error diagnostic
		///   has been reported.
		/// </summary>
		/// <param name="diagnostics">The diagnostics that should be reported.</param>
		/// <param name="errorsOnly">Indicates whether error diagnostics should be reported exclusively.</param>
		internal bool Report([NotNull] IEnumerable<Diagnostic> diagnostics, bool errorsOnly)
		{
			var containsError = false;
			foreach (var diagnostic in diagnostics)
			{
				Report(diagnostic, errorsOnly);
				containsError |= diagnostic.Severity == DiagnosticSeverity.Error;
			}

			return !containsError;
		}

		/// <summary>
		///   Writes the formatted <paramref name="message" />.
		/// </summary>
		/// <param name="severity">The type of the message.</param>
		/// <param name="message">The format message.</param>
		/// <param name="arguments">The format arguments for the message.</param>
		private void Log(DiagnosticSeverity severity, string message, params object[] arguments)
		{
			message = String.Format(message, arguments);

			PrintToDebugOutput(severity, message);
			PrintToConsole(severity, message);
		}

		/// <summary>
		///   Prints the message to the debug output.
		/// </summary>
		/// <param name="severity">The severity of the message.</param>
		/// <param name="message">The message that should be printed.</param>
		private static void PrintToDebugOutput(DiagnosticSeverity severity, string message)
		{
			var type = "";
			switch (severity)
			{
				case DiagnosticSeverity.Info:
					type = "Info ";
					break;
				case DiagnosticSeverity.Warning:
					type = "Warning";
					break;
				case DiagnosticSeverity.Error:
					type = "Error ";
					break;
			}

			Debug.WriteLine("[{0}] {1}", type, message);
		}

		/// <summary>
		///   Prints the message to the console.
		/// </summary>
		/// <param name="severity">The severity of the message.</param>
		/// <param name="message">The message that should be printed.</param>
		private void PrintToConsole(DiagnosticSeverity severity, string message)
		{
			switch (severity)
			{
				case DiagnosticSeverity.Info:
					if (!Silent)
						ConsoleHelpers.WriteLine(message, ConsoleColor.White);
					break;
				case DiagnosticSeverity.Warning:
					ConsoleHelpers.WriteLine(message, ConsoleColor.Yellow);
					break;
				case DiagnosticSeverity.Error:
					ConsoleHelpers.WriteLine(message, ConsoleColor.Red);
					break;
				default:
					Assert.NotReached();
					break;
			}
		}
	}
}
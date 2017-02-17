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

namespace SafetySharp.Compiler
{
	using System;
	using Microsoft.Build.Framework;
	using Microsoft.Build.Utilities;
	using Microsoft.CodeAnalysis;
	using ISSE.SafetyChecking.Utilities;

	/// <summary>
	///   Reports errors to MSBuild.
	/// </summary>
	internal class MSBuildReporter : ErrorReporter
	{
		private readonly TaskLoggingHelper _logger;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="logger">The logger that should be used to report messages to MSBuild.</param>
		public MSBuildReporter(TaskLoggingHelper logger)
		{
			Requires.NotNull(logger, nameof(logger));
			_logger = logger;
		}

		/// <summary>
		///   Logs an application error.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		public override void Error(string message, params object[] arguments)
		{
			_logger.LogError(String.Format(message, arguments));
		}

		/// <summary>
		///   Logs an application warning.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		public override void Warn(string message, params object[] arguments)
		{
			_logger.LogWarning(String.Format(message, arguments));
		}

		/// <summary>
		///   Logs an informational message.
		/// </summary>
		/// <param name="message">The non-empty message that should be logged.</param>
		/// <param name="arguments">The arguments that should be used to format <paramref name="message" />.</param>
		public override void Info(string message, params object[] arguments)
		{
			_logger.LogMessage(String.Format(message, arguments));
		}

		/// <summary>
		///   Reports <paramref name="diagnostic" /> depending on its severity. If <paramref name="errorsOnly" /> is <c>true</c>, only
		///   error diagnostics are reported.
		/// </summary>
		/// <param name="diagnostic">The diagnostic that should be reported.</param>
		/// <param name="errorsOnly">Indicates whether error diagnostics should be reported exclusively.</param>
		internal override void Report(Diagnostic diagnostic, bool errorsOnly)
		{
			var location = diagnostic.Location.GetMappedLineSpan();
			var message = diagnostic.GetMessage();

			if (message == null)
				return;

			switch (diagnostic.Severity)
			{
				case DiagnosticSeverity.Error:
					_logger.LogError(
						null,
						diagnostic.Id,
						diagnostic.Descriptor.HelpLinkUri,
						location.Path,
						location.StartLinePosition.Line + 1,
						location.StartLinePosition.Character + 1,
						location.EndLinePosition.Line + 1,
						location.EndLinePosition.Character + 1,
						"{0}",
						message);
					break;
				case DiagnosticSeverity.Warning:
					if (errorsOnly)
						break;

					_logger.LogWarning(
						null,
						diagnostic.Id,
						diagnostic.Descriptor.HelpLinkUri,
						location.Path,
						location.StartLinePosition.Line + 1,
						location.StartLinePosition.Character + 1,
						location.EndLinePosition.Line + 1,
						location.EndLinePosition.Character + 1,
						"{0}",
						message);
					break;
				case DiagnosticSeverity.Info:
				case DiagnosticSeverity.Hidden:
					if (errorsOnly)
						break;

					_logger.LogMessage(
						null,
						diagnostic.Id,
						diagnostic.Descriptor.HelpLinkUri,
						location.Path,
						location.StartLinePosition.Line + 1,
						location.StartLinePosition.Character + 1,
						location.EndLinePosition.Line + 1,
						location.EndLinePosition.Character + 1,
						MessageImportance.Low,
						"{0}",
						message);
					break;
				default:
					Assert.NotReached("Unknown diagnostic severity.");
					break;
			}
		}
	}
}
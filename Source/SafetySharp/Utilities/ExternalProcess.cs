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

namespace SafetySharp.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	/// <summary>
	///   Represents an external process.
	/// </summary>
	internal class ExternalProcess : IDisposable
	{
		/// <summary>
		///   The callback that is invoked when an output is generated.
		/// </summary>
		private readonly Action<Output> _outputCallback;

		/// <summary>
		///   The external process.
		/// </summary>
		private readonly Process _process;

		/// <summary>
		///   The outputs generated during the execution of the process.
		/// </summary>
		private List<Output> _outputs;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="fileName">The file name of the external executable.</param>
		/// <param name="commandLineArguments">The command line arguments that should be passed to the executable.</param>
		/// <param name="outputCallback">The callback that is invoked when an output is generated.</param>
		public ExternalProcess(string fileName, string commandLineArguments, Action<Output> outputCallback = null)
		{
			Requires.NotNullOrWhitespace(fileName, nameof(fileName));
			Requires.NotNull(commandLineArguments, nameof(commandLineArguments));

			_process = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = new ProcessStartInfo(fileName, commandLineArguments)
				{
					UseShellExecute = false,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};

			_process.OutputDataReceived += (o, e) => LogMessage(e.Data, isError: false);
			_process.ErrorDataReceived += (o, e) => LogMessage(e.Data, isError: true);

			_outputCallback = outputCallback;
		}

		/// <summary>
		///   Gets the outputs generated during the last execution of the process.
		/// </summary>
		public IEnumerable<Output> Outputs => _outputs ?? Enumerable.Empty<Output>();

		/// <summary>
		///   Gets a value indicating whether the process has exited.
		/// </summary>
		public bool Running { get; private set; }

		/// <summary>
		///   Gets the exit code of the last execution of the process.
		/// </summary>
		public int ExitCode => _process?.ExitCode ?? 0;

		/// <summary>
		///   Gets or sets the process' working directory.
		/// </summary>
		public string WorkingDirectory
		{
			get { return _process.StartInfo.WorkingDirectory; }
			set { _process.StartInfo.WorkingDirectory = value; }
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			_process.Dispose();
		}

		/// <summary>
		///   Adds the <paramref name="message" /> to the output queue.
		/// </summary>
		/// <param name="message">The message that should be added.</param>
		/// <param name="isError">Indicates whether <paramref name="message" /> describes an error.</param>
		private void LogMessage(string message, bool isError)
		{
			if (String.IsNullOrWhiteSpace(message))
				return;

			var output = new Output { Message = message, IsError = isError };
			_outputs.Add(output);

			_outputCallback?.Invoke(output);
		}

		/// <summary>
		///   Runs the process.
		/// </summary>
		public void Run()
		{
			Requires.That(!Running, "The process is already running.");

			Running = true;
			try
			{
				_outputs = new List<Output>();
				_process.Start();

				_process.BeginErrorReadLine();
				_process.BeginOutputReadLine();

				_process.WaitForExit();
			}
			finally
			{
				Running = false;
			}
		}

		/// <summary>
		///   Represents an output of the process.
		/// </summary>
		public struct Output
		{
			/// <summary>
			///   Indicates whether the message describes an error.
			/// </summary>
			public bool IsError;

			/// <summary>
			///   The message that has been written.
			/// </summary>
			public string Message;
		}
	}
}
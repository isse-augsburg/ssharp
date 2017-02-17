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

namespace ISSE.SafetyChecking.Utilities
{
	using System;
	using System.CodeDom;
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;

	/// <summary>
	///   Represents an external process.
	/// </summary>
	internal class ExternalProcess : IDisposable
	{
		/// <summary>
		///   The callback that is invoked when an output is generated.
		/// </summary>
		private readonly Action<string> _outputCallback;

		/// <summary>
		///   The external process.
		/// </summary>
		private readonly Process _process;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="fileName">The file name of the external executable.</param>
		/// <param name="commandLineArguments">The command line arguments that should be passed to the executable.</param>
		/// <param name="outputCallback">The callback that is invoked when an output is generated.</param>
		public ExternalProcess(string fileName, string commandLineArguments, Action<string> outputCallback = null)
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

			_outputCallback = outputCallback;
		}

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
		///   Runs the process.
		/// </summary>
		public void Run()
		{
			Requires.That(!Running, "The process is already running.");

			Running = true;
			try
			{
				_process.Start();

				using (var processWaiter = Task.Factory.StartNew(() => _process.WaitForExit()))
				using (var outputReader = Task.Factory.StartNew(() => HandleOutput(_process.StandardOutput)))
				using (var errorReader = Task.Factory.StartNew(() => HandleOutput(_process.StandardError)))
					Task.WaitAll(processWaiter, outputReader, errorReader);
			}
			finally
			{
				Running = false;
			}
		}

		/// <summary>
		///   Handles process output.
		/// </summary>
		private async Task HandleOutput(TextReader reader)
		{
			string text;
			while ((text = await reader.ReadLineAsync()) != null)
			{
				if (!String.IsNullOrWhiteSpace(text))
					_outputCallback?.Invoke(text);
			}
		}

		public static MachineType GetDllMachineType(string dllPath)
		{
			// Idea: http://stackoverflow.com/questions/1001404/check-if-unmanaged-dll-is-32-bit-or-64-bit/1002672#1002672
			// See also http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
			var fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
			var br = new BinaryReader(fs);

			fs.Seek(0x3c, SeekOrigin.Begin);
			var peOffset = br.ReadInt32();
			fs.Seek(peOffset, SeekOrigin.Begin);
			var peHead = br.ReadUInt32();
			if (peHead != (0x00004550))
				// "PE\0\0", little-endian
				throw new Exception("Can't find PE header");
			var machineTypeAsUint16 = br.ReadUInt16();
			br.Close();
			fs.Close();

			switch (machineTypeAsUint16)
			{
				case 0x8664:
					return MachineType.MachineTypeAmd64;
				case 0x14c:
					return MachineType.MachineTypeAmd64;
				default:
					return MachineType.MachineTypeOther;
			}
		}
	}

	public enum MachineType
	{
		MachineTypeAmd64,
		MachineTypeI386,
		MachineTypeOther
	}
}
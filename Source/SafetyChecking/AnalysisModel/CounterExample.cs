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

namespace ISSE.SafetyChecking.AnalysisModel
{
	using System;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	public sealed class CounterExample
	{
		/// <summary>
		///   The file extension used by counter example files.
		/// </summary>
		public const string FileExtension = ".ssharp";

		/// <summary>
		///   The first few bytes that indicate that a file is a valid S# counter example file.
		/// </summary>
		public const int FileHeader = 0x3FE0DD04;

		public int[][] ReplayInfo { get; }
		public byte[][] States { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="states">The serialized counter example.</param>
		/// <param name="replayInfo">The replay information of the counter example.</param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public CounterExample(byte[][] states, int[][] replayInfo, bool endsWithException)
		{
			Requires.NotNull(states, nameof(states));
			Requires.NotNull(replayInfo, nameof(replayInfo));
			Requires.That(replayInfo.Length == states.Length - 1, "Invalid replay info.");
			
			EndsWithException = endsWithException;

			States = states;
			ReplayInfo = replayInfo;
		}

		/// <summary>
		///   Indicates whether the counter example ends with an exception.
		/// </summary>
		public bool EndsWithException { get; }

		/// <summary>
		///   Gets the number of steps the counter example consists of.
		/// </summary>
		public int StepCount
		{
			get
			{
				Requires.That(States != null, "No counter example has been loaded.");
				return States.Length - 1;
			}
		}

		/// <summary>
		///   Gets the serialized states of the counter example.
		/// </summary>
		internal byte[] GetState(int position) => States[position + 1];
	}
}
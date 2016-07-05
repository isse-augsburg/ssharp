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
	using System.Collections.Generic;
	using Utilities;

	/// <summary>
	///   Configures S#'s model checker, determining the amount of CPU cores and memory to use.
	/// </summary>
	public struct AnalysisConfiguration
	{
		private const int DefaultStateCapacity = 1 << 24;
		private const int DefaultStackCapacity = 1 << 16;
		private const int DefaultSuccessorStateCapacity = 1 << 14;
		private const int MinCapacity = 1024;

		private int _cpuCount;
		private int _stackCapacity;
		private int _stateCapacity;
		private int _successorStateCapacity;

		/// <summary>
		///   Gets or sets a value indicating whether a counter example should be generated when a formula violation is detected or an
		///   unhandled exception occurred during model checking.
		/// </summary>
		public bool GenerateCounterExample { get; set; }

		/// <summary>
		///   Gets or sets a value indicating whether only progress reports should be output.
		/// </summary>
		public bool ProgressReportsOnly { get; set; }

		/// <summary>
		///   The default configuration.
		/// </summary>
		internal static readonly AnalysisConfiguration Default = new AnalysisConfiguration
		{
			CpuCount = Int32.MaxValue,
			ProgressReportsOnly = false,
			StackCapacity = DefaultStackCapacity,
			StateCapacity = DefaultStateCapacity,
			SuccessorCapacity = DefaultSuccessorStateCapacity,
			GenerateCounterExample = true
		};

		/// <summary>
		///   Gets or sets the number of states that can be stored during model checking.
		/// </summary>
		public int StateCapacity
		{
			get { return Math.Max(_stateCapacity, MinCapacity); }
			set
			{
				Requires.That(value >= MinCapacity, $"{nameof(StateCapacity)} must be at least {MinCapacity}.");
				_stateCapacity = value;
			}
		}

		/// <summary>
		///   Gets or sets the number of states that can be stored on the stack during model checking.
		/// </summary>
		public int StackCapacity
		{
			get { return Math.Max(_stackCapacity, MinCapacity); }
			set
			{
				Requires.That(value >= MinCapacity, $"{nameof(StackCapacity)} must be at least {MinCapacity}.");
				_stackCapacity = value;
			}
		}

		/// <summary>
		///   Gets or sets the number of successor states that can be computed for each state.
		/// </summary>
		public int SuccessorCapacity
		{
			get { return Math.Max(_successorStateCapacity, MinCapacity); }
			set
			{
				Requires.That(value >= MinCapacity, $"{nameof(SuccessorCapacity)} must be at least {MinCapacity}.");
				_successorStateCapacity = value;
			}
		}

		/// <summary>
		///   Gets or sets the number of CPUs that are used for model checking. The value is automatically clamped
		///   to the interval of [1, #CPUs].
		/// </summary>
		public int CpuCount
		{
			get { return _cpuCount; }
			set { _cpuCount = Math.Min(Environment.ProcessorCount, Math.Max(1, value)); }
		}
	}
}
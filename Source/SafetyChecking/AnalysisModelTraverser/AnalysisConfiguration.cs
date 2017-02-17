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
	using Utilities;

	/// <summary>
	///   Configures S#'s model checker, determining the amount of CPU cores and memory to use.
	/// </summary>
	public struct AnalysisConfiguration
	{
		private const long DefaultStateCapacity = 1 << 24;
		private const long DefaultTransitionCapacity = 1 << 28;
		private const long DefaultStackCapacity = 1 << 20;
		private const long DefaultSuccessorStateCapacity = 1 << 14;
		private const long MinCapacity = 1024;

		private int _cpuCount;
		private long _stackCapacity;
		private long _stateCapacity;
		private long _transitionCapacity;
		private long _successorStateCapacity;

		/// <summary>
		///   Gets or sets a value indicating whether a counter example should be generated when a formula violation is detected or an
		///   unhandled exception occurred during model checking.
		/// </summary>
		public bool GenerateCounterExample { get; set; }

		/// <summary>
		///   Gets or sets a value indicating whether only progress reports should be output.
		/// </summary>
		internal bool ProgressReportsOnly { get; set; }

		/// <summary>
		///   The default configuration.
		/// </summary>
		public static readonly AnalysisConfiguration Default = new AnalysisConfiguration
		{
			CpuCount = Int32.MaxValue,
			ProgressReportsOnly = false,
			StackCapacity = DefaultStackCapacity,
			StateCapacity = DefaultStateCapacity,
			SuccessorCapacity = DefaultSuccessorStateCapacity,
			TransitionCapacity = DefaultTransitionCapacity,
			GenerateCounterExample = true
		};

		/// <summary>
		///   Gets or sets the number of transitions that can be stored during model checking.
		/// </summary>
		public long TransitionCapacity
		{
			get { return Math.Max(_transitionCapacity, MinCapacity); }
			set
			{
				Requires.That(value >= MinCapacity, $"{nameof(TransitionCapacity)} must be at least {MinCapacity}.");
				_transitionCapacity = value;
			}
		}

		/// <summary>
		///   Gets or sets the number of states that can be stored during model checking.
		/// </summary>
		public long StateCapacity
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
		public long StackCapacity
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
		public long SuccessorCapacity
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
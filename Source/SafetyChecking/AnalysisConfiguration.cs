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

namespace ISSE.SafetyChecking
{
	using System;
	using ExecutedModel;
	using Utilities;

	public enum MomentOfIndependentFaultActivation
	{
		AtStepBeginning,
		OnFirstMethodWithoutUndo,
		OnFirstMethodWithUndo
	}

	// TODO: obsolete
	public enum RetraversalNormalizations
	{
		None,
		EmbedObserversIntoModel
	}

	public enum LtmcModelChecker
	{
		BuiltInLtmc,
		BuiltInDtmc,
		ExternalMrmc
	}

	public enum LtmdpModelChecker
	{
		BuiltInLtmdp,
		BuiltInNmdp,
		BuildInMdp
	}

	/// <summary>
	///   Configures S#'s model checker, determining the amount of CPU cores and memory to use.
	/// </summary>
	public struct AnalysisConfiguration
	{
		private const long DefaultStackCapacity = 1 << 20;
		private const long DefaultSuccessorStateCapacity = 1 << 14;
		private const long MinCapacity = 1024;
		private static readonly ModelCapacity _defaultModelCapacity = ModelCapacityByModelDensity.Normal;

		private int _cpuCount;
		private long _stackCapacity;
		private long _successorStateCapacity;

		/// <summary>
		/// </summary>
		public bool AllowFaultsOnInitialTransitions { get; set; }

		/// <summary>
		///   Simulation only: Use the probabilities of the options when selecting the result of a probabilistic choice.
		///   Thus, more probable options get selected more probable. Otherwise, each option has the same chance to get selected.
		/// </summary>
		public bool UseOptionProbabilitiesInSimulation { get; set; }

		/// <summary>
		/// </summary>
		public bool EnableEarlyTermination { get; set; }

		/// <summary>
		///   If set to true, the model checker uses a compact state storage, in which the found states are indexed by a continuous variable.
		///   If set to false, the traditional sparse state storage is used, which requires a little less memory and is unnoticeable faster.
		/// </summary>
		public bool UseCompactStateStorage { get; set; }

		/// <summary>
		///   Gets or sets a value indicating whether a counter example should be generated when a formula violation is detected or an
		///   unhandled exception occurred during model checking.
		/// </summary>
		public bool GenerateCounterExample { get; set; }

		/// <summary>
		///   Collect fault sets when conducting a MinimalCriticalSetAnalysis.
		/// </summary>
		public bool CollectFaultSets { get; set; }

		/// <summary>
		///   Gets or sets a value indicating whether only progress reports should be output.
		/// </summary>
		internal bool ProgressReportsOnly { get; set; }

		/// <summary>
		///   The TextWriter used to log the process.
		/// </summary>
		public System.IO.TextWriter DefaultTraceOutput { get; set; }

		/// <summary>
		///   Write GraphViz models of the state space to the DefaultTraceOutput when creating the models.
		/// </summary>
		public bool WriteGraphvizModels { get; set; }

		/// <summary>
		///   The default configuration.
		/// </summary>
		public static readonly AnalysisConfiguration Default = new AnalysisConfiguration
		{
			AllowFaultsOnInitialTransitions = false,
			UseOptionProbabilitiesInSimulation = false,
			EnableEarlyTermination = false,
			CpuCount = Int32.MaxValue,
			ProgressReportsOnly = false,
			DefaultTraceOutput = Console.Out,
			WriteGraphvizModels = false,
			ModelCapacity = _defaultModelCapacity,
			StackCapacity = DefaultStackCapacity,
			SuccessorCapacity = DefaultSuccessorStateCapacity,
			UseCompactStateStorage = false,
			GenerateCounterExample = true,
			CollectFaultSets = true,
			StateDetected = null,
			UseAtomarPropositionsAsStateLabels = true,
			MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.OnFirstMethodWithUndo,
			LimitOfActiveFaults = null,
			RetraversalNormalizations = RetraversalNormalizations.None,
			LtmcModelChecker = LtmcModelChecker.BuiltInDtmc,
			LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp
		};

		/// <summary>
		///   Gets or sets the number of states and transitions that can be stored during model checking.
		/// </summary>
		public ModelCapacity ModelCapacity { get; set; }
		
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

		/// <summary>
		///   Useful for debugging purposes. When a state is detected the first time then call the associated action.
		///   The delegate is only called in debug mode.
		///   A great usage example is to set "StateDetected = (state) => { if (state==190292) Debugger.Break();};"
		///   It makes sense to set CpuCount to 1 to omit races and ensure that a state always gets the same state
		///   number.
		/// </summary>
		public Action<int> StateDetected { get; set; }

		/// <summary>
		///   Determine if a formula like "Model.X==1 || Model.Y==true" should be split into the smaller parts
		///   "Model.X==1" and "Model.Y==true". If set to false then the maximal possible expressions are used.
		/// </summary>
		public bool UseAtomarPropositionsAsStateLabels { get; set; }

		/// <summary>
		/// </summary>
		public MomentOfIndependentFaultActivation MomentOfIndependentFaultActivation { get; set; }
		
		/// <summary>
		/// </summary>
		public int? LimitOfActiveFaults{ get; set; }

		/// <summary>
		/// </summary>
		public RetraversalNormalizations RetraversalNormalizations { get; set; }

		/// <summary>
		/// </summary>
		public LtmcModelChecker LtmcModelChecker { get; set; }

		/// <summary>
		/// </summary>
		public LtmdpModelChecker LtmdpModelChecker { get; set; }
	}
}
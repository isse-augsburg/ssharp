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

namespace SafetySharp.Analysis.ModelChecking.ModelTraversal
{
	using System;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Provides context information for the traversal of a model.
	/// </summary>
	internal sealed class TraversalContext<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   The number of states that have to be found between two progress reports.
		/// </summary>
		public const int ReportStateCountDelta = 200000;

		/// <summary>
		///   The configuration values for the analysis.
		/// </summary>
		public readonly AnalysisConfiguration Configuration;

		/// <summary>
		///   The load balancer that balances the work of multiple <see cref="Worker" /> instances.
		/// </summary>
		public readonly LoadBalancer LoadBalancer;

		/// <summary>
		///   The action that should be invoked when output is generated.
		/// </summary>
		public readonly Action<string> Output;

		/// <summary>
		///   The parameters influencing the traversal process.
		/// </summary>
		public readonly TraversalParameters<TExecutableModel> TraversalParameters = new TraversalParameters<TExecutableModel>();

		/// <summary>
		///   The number of computed transitions checked by the model checker.
		/// </summary>
		internal long ComputedTransitionCount;

		/// <summary>
		///   The counter example that has been generated for the traversal, if any.
		/// </summary>
		public CounterExample<TExecutableModel> CounterExample;

		/// <summary>
		///   The exception that has been generated during the traversal, if any.
		/// </summary>
		public Exception Exception;

		/// <summary>
		///   Indicates whether the analyzed formula is valid.
		/// </summary>
		public bool FormulaIsValid;

		/// <summary>
		///   Indicates whether a counter examle is currently being generated.
		/// </summary>
		public int GeneratingCounterExample;

		/// <summary>
		///   The number of levels checked by the model checker.
		/// </summary>
		public int LevelCount;

		/// <summary>
		///   Indicates the number of states the next progress report is generated.
		/// </summary>
		public int NextReport;

		/// <summary>
		///   The number of states checked by the model checker.
		/// </summary>
		public int StateCount;

		/// <summary>
		///   The states that have previously been discovered by the traversal.
		/// </summary>
		public StateStorage States;

		/// <summary>
		///   The number of activation-minimal transitions checked by the model checker.
		/// </summary>
		public long TransitionCount;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="loadBalancer">The load balancer that balances the work of multiple <see cref="Worker" /> instances.</param>
		/// <param name="configuration">The configuration values for the analysis.</param>
		/// <param name="output">The action that should be invoked when output is generated.</param>
		public TraversalContext(LoadBalancer loadBalancer, AnalysisConfiguration configuration, Action<string> output)
		{
			Requires.NotNull(loadBalancer, nameof(loadBalancer));
			Requires.NotNull(output, nameof(output));

			LoadBalancer = loadBalancer;
			Configuration = configuration;
			Output = output;

			Reset();
		}

		/// <summary>
		///   Resets the context so that a new traversal can be started.
		/// </summary>
		public void Reset()
		{
			FormulaIsValid = true;
			ComputedTransitionCount = 0;
			CounterExample = null;
			Exception = null;
			GeneratingCounterExample = -1;
			LevelCount = 0;
			NextReport = ReportStateCountDelta;
			StateCount = 0;
			TransitionCount = 0;
		}

		/// <summary>
		///   Prints a progress report if necessary.
		/// </summary>
		public void ReportProgress()
		{
			if (InterlockedExtensions.ExchangeIfGreaterThan(ref NextReport, StateCount, NextReport + ReportStateCountDelta))
				Report();
		}

		/// <summary>
		///   Reports the number of states and transitions that have been checked.
		/// </summary>
		public void Report()
		{
			Output.Invoke($"Discovered {StateCount:n0} states, {TransitionCount:n0} transitions, {LevelCount} levels.");
		}
	}
}
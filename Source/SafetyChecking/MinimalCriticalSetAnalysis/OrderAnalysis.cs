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

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using Modeling;
	using ExecutableModel;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;

	/// <summary>
	///   Performs order analyses for minimal critical fault sets.
	/// </summary>
	public class OrderAnalysis<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		// Note: The faultOrderModifier reserves the first 4 bytes of the state vector
		private readonly FaultOptimizationBackend<TExecutableModel> _backend = new FaultOptimizationBackend<TExecutableModel>(stateHeaderBytes: 4);
		private readonly SafetyAnalysisResults<TExecutableModel> _results;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="results">The result of the safety analysis the order analysis should be conducted for.</param>
		/// <param name="configuration">The model checker's configuration that determines certain model checker settings.</param>
		internal OrderAnalysis(SafetyAnalysisResults<TExecutableModel> results, AnalysisConfiguration configuration)
		{
			Requires.NotNull(results, nameof(results));

			_results = results;
			_backend.InitializeModel(configuration, results.RuntimeModelCreator, results.Hazard);
		}

		/// <summary>
		///   Computes the order relationships for all minimal critical fault sets contained in the
		///   <paramref name="safetyAnalysisResults" />.
		/// </summary>
		/// <param name="safetyAnalysisResults">The results of the safety analysis the order relationships should be computed for.</param>
		public static OrderAnalysisResults<TExecutableModel> ComputeOrderRelationships(SafetyAnalysisResults<TExecutableModel> safetyAnalysisResults)
		{
			return ComputeOrderRelationships(safetyAnalysisResults, AnalysisConfiguration.Default);
		}

		/// <summary>
		///   Raised when the model checker has written an output.
		/// </summary>
		internal event Action<string> OutputWritten
		{
			add { _backend.OutputWritten += value; }
			remove { _backend.OutputWritten -= value; }
		}

		/// <summary>
		///   Computes the order relationships for all minimal critical fault sets contained in the
		///   <paramref name="safetyAnalysisResults" />.
		/// </summary>
		/// <param name="safetyAnalysisResults">The results of the safety analysis the order relationships should be computed for.</param>
		/// <param name="configuration">The configuration settings of the model checker that should be used.</param>
		public static OrderAnalysisResults<TExecutableModel> ComputeOrderRelationships(SafetyAnalysisResults<TExecutableModel> safetyAnalysisResults,
																	 AnalysisConfiguration configuration)
		{
			var analysis = new OrderAnalysis<TExecutableModel>(safetyAnalysisResults, configuration);
			return analysis.ComputeOrderRelationships();
		}

		/// <summary>
		///   Computes the order relationships for all minimal critical fault sets.
		/// </summary>
		internal OrderAnalysisResults<TExecutableModel> ComputeOrderRelationships()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var relationships = _results
				.MinimalCriticalSets
				.Where(set => set.Count >= 2)
				.ToDictionary(faultSet => faultSet, faultSet => (IEnumerable<OrderRelationship<TExecutableModel>>)GetOrderRelationships(faultSet).ToArray());

			return new OrderAnalysisResults<TExecutableModel>(_results, stopwatch.Elapsed, relationships);
		}

		/// <summary>
		///   Gets the activation order relationships that exist for the faults contained in
		///   <paramref name="minimalCriticalFaultSet" />, if any.
		/// </summary>
		/// <param name="minimalCriticalFaultSet">The minimal critical fault set the order should be returned for.</param>
		private IEnumerable<OrderRelationship<TExecutableModel>> GetOrderRelationships(ISet<Fault> minimalCriticalFaultSet)
		{
			var checkedSet = new FaultSet(minimalCriticalFaultSet);
			var activation = _results.FaultActivationBehavior == FaultActivationBehavior.ForceOnly ? Activation.Forced : Activation.Nondeterministic;
			var checkedFaults = minimalCriticalFaultSet.ToArray();

			// Create all pairs of faults contained in minimalCriticalFaultSet such that
			// we don't get pairs (f,f) and we don't generate duplicate pairs (f1,f2) and (f2,f1)
			for (var i = 0; i < checkedFaults.Length; ++i)
			{
				for (var j = i + 1; j < checkedFaults.Length; ++j)
				{
					var fault1 = checkedFaults[i];
					var fault2 = checkedFaults[j];

					// Check if one can be activated strictly before the other
					var fault1BeforeFault2 = _backend.CheckOrder(fault1, fault2, checkedSet, activation, forceSimultaneous: false);
					var fault2BeforeFault1 = _backend.CheckOrder(fault2, fault1, checkedSet, activation, forceSimultaneous: false);

					// If both can be activated stritly before the other, there is no ordering
					if (!fault2BeforeFault1.FormulaHolds && !fault1BeforeFault2.FormulaHolds)
						continue;

					// Check for simultaneous activations
					var simultaneous = _backend.CheckOrder(fault1, fault2, checkedSet, activation, forceSimultaneous: true);

					// f1 == f2
					if (!simultaneous.FormulaHolds && fault1BeforeFault2.FormulaHolds && fault2BeforeFault1.FormulaHolds)
						yield return new OrderRelationship<TExecutableModel>(simultaneous, fault1, fault2, OrderRelationshipKind.Simultaneously);

					// f1 <= f2
					else if (!fault1BeforeFault2.FormulaHolds && !simultaneous.FormulaHolds)
						yield return new OrderRelationship<TExecutableModel>(simultaneous, fault1, fault2, OrderRelationshipKind.Precedes);

					// f1 < f2
					else if (!fault1BeforeFault2.FormulaHolds && simultaneous.FormulaHolds)
						yield return new OrderRelationship<TExecutableModel>(simultaneous, fault1, fault2, OrderRelationshipKind.StrictlyPrecedes);

					// f2 <= f1
					else if (!fault2BeforeFault1.FormulaHolds && !simultaneous.FormulaHolds)
						yield return new OrderRelationship<TExecutableModel>(simultaneous, fault2, fault1, OrderRelationshipKind.Precedes);

					// f2 < f1
					else if (!fault2BeforeFault1.FormulaHolds && simultaneous.FormulaHolds)
						yield return new OrderRelationship<TExecutableModel>(simultaneous, fault2, fault1, OrderRelationshipKind.StrictlyPrecedes);
				}
			}
		}
	}
}
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

namespace ISSE.SafetyChecking.StateGraphModel
{
	using System;
	using ExecutableModel;
	using Modeling;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutedModel;
	using FaultMinimalKripkeStructure;
	using Formula;
	using MinimalCriticalSetAnalysis;

	/// <summary>
	///   Pre-builts the model's entire state graph, subsequently taking advantage of the fault-removal optimization.
	/// </summary>
	internal class StateGraphBackend<TExecutableModel> : AnalysisBackend<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private InvariantChecker<TExecutableModel> _checker;

		/// <summary>
		///   Initializes the model that should be analyzed.
		/// </summary>
		/// <param name="configuration">The configuration that should be used for the analyses.</param>
		/// <param name="hazard">The hazard that should be analyzed.</param>
		protected override void InitializeModel(AnalysisConfiguration configuration, Formula hazard)
		{
			var checker = new QualitativeChecker<TExecutableModel> { Configuration = configuration };
			checker.Configuration.ProgressReportsOnly = false;
			checker.OutputWritten += OnOutputWritten;

			var invariant = new UnaryFormula(hazard, UnaryOperator.Not);

			var stateGraph = checker.GenerateStateGraph(RuntimeModelCreator);

			configuration.StateCapacity = Math.Max(1024, (int)(stateGraph.StateCount * 1.5));
			_checker = new InvariantChecker<TExecutableModel>(() => new StateGraphModel<TExecutableModel>(stateGraph, configuration.SuccessorCapacity), OnOutputWritten,
				configuration, invariant);
		}

		/// <summary>
		///   Checks the <see cref="faults" /> for criticality using the <see cref="activation" /> mode.
		/// </summary>
		/// <param name="faults">The fault set that should be checked for criticality.</param>
		/// <param name="activation">The activation mode of the fault set.</param>
		internal override AnalysisResult<TExecutableModel> CheckCriticality(FaultSet faults, Activation activation)
		{
			var suppressedFaults = new FaultSet();
			foreach (var fault in RuntimeModelCreator.FaultsInBaseModel)
			{
				if (GetEffectiveActivation(fault, faults, activation) == Activation.Suppressed)
					suppressedFaults = suppressedFaults.Add(fault);
			}

			_checker.Context.TraversalParameters.TransitionModifiers.Clear();
			_checker.Context.TraversalParameters.TransitionModifiers.Add(() => new FaultSuppressionModifier<TExecutableModel>(suppressedFaults));

			return _checker.Check();
		}

		/// <summary>
		///   Checks the order of <see cref="firstFault" /> and <see cref="secondFault" /> for the
		///   <see cref="minimalCriticalFaultSet" /> using the <see cref="activation" /> mode.
		/// </summary>
		/// <param name="firstFault">The first fault that should be checked.</param>
		/// <param name="secondFault">The second fault that should be checked.</param>
		/// <param name="minimalCriticalFaultSet">The minimal critical fault set that should be checked.</param>
		/// <param name="activation">The activation mode of the fault set.</param>
		/// <param name="forceSimultaneous">Indicates whether both faults must occur simultaneously.</param>
		internal override AnalysisResult<TExecutableModel> CheckOrder(Fault firstFault, Fault secondFault, FaultSet minimalCriticalFaultSet,
													Activation activation, bool forceSimultaneous)
		{
			throw new NotImplementedException();
		}
	}
}
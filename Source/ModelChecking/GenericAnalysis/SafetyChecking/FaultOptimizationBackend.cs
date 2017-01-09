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

namespace SafetySharp.Analysis.SafetyChecking
{
	using System;
	using System.Linq;
	using ModelChecking;
	using ModelChecking.ModelTraversal.TraversalModifiers;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;

	/// <summary>
	///   Checks all formulas individually on the model taking advantage of the fault-removal optimization.
	/// </summary>
	internal class FaultOptimizationBackend<TExecutableModel> : AnalysisBackend<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly int _stateHeaderBytes;
		private InvariantChecker<TExecutableModel> _invariantChecker;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="stateHeaderBytes">The number of bytes to reserve in the state vector for analysis purposes.</param>
		public FaultOptimizationBackend(int stateHeaderBytes = 0)
		{
			_stateHeaderBytes = stateHeaderBytes;
		}

		/// <summary>
		///   Initizializes the model that should be analyzed.
		/// </summary>
		/// <param name="configuration">The configuration that should be used for the analyses.</param>
		/// <param name="hazard">The hazard that should be analyzed.</param>
		protected override void InitializeModel(AnalysisConfiguration configuration, Formula hazard)
		{
			Func<AnalysisModel<TExecutableModel>> createModel = () =>
				new ActivationMinimalExecutedModel<TExecutableModel>(RuntimeModel.DeriveExecutableModelGenerator(hazard), configuration.SuccessorCapacity);

			_invariantChecker = new InvariantChecker<TExecutableModel>(createModel, OnOutputWritten, configuration, formulaIndex: 0);
		}

		/// <summary>
		///   Checks the <see cref="faults" /> for criticality using the <see cref="activation" /> mode.
		/// </summary>
		/// <param name="faults">The fault set that should be checked for criticality.</param>
		/// <param name="activation">The activation mode of the fault set.</param>
		internal override AnalysisResult<TExecutableModel> CheckCriticality(FaultSet faults, Activation activation)
		{
			ChangeFaultActivations(faults, activation);
			return _invariantChecker.Check();
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
			ChangeFaultActivations(minimalCriticalFaultSet, activation);

			_invariantChecker.Context.TraversalParameters.TransitionModifiers.Clear();
			_invariantChecker.Context.TraversalParameters.TransitionModifiers.Add(
				() => new FaultOrderModifier<TExecutableModel>(firstFault, secondFault, forceSimultaneous));

			return _invariantChecker.Check();
		}

		/// <summary>
		///   Updates the activation modes of the faults contained in the model.
		/// </summary>
		private void ChangeFaultActivations(FaultSet faults, Activation activation)
		{
			foreach (var model in _invariantChecker.AnalyzedModels.Cast<ExecutedModel<TExecutableModel>>())
				model.ChangeFaultActivations(GetUpdateFaultActivations(faults, activation));
		}

		/// <summary>
		///   Creates a function that determines the activation state of a fault.
		/// </summary>
		private Func<Fault, Activation> GetUpdateFaultActivations(FaultSet faults, Activation activation)
		{
			return fault => GetEffectiveActivation(fault, faults, activation);
		}
	}
}
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

namespace ISSE.SafetyChecking.FaultMinimalKripkeStructure
{
	using System;
	using ExecutableModel;
	using AnalysisModel;
	using Utilities;
	using ExecutedModel;
	using System.Linq;
	using Formula;

	/// <summary>
	///   Represents an <see cref="AnalysisModel" /> that computes its state by executing a <see cref="SafetySharpRuntimeModel" /> with
	///   activation-minimal transitions.
	/// </summary>
	internal sealed class ActivationMinimalExecutedModel<TExecutableModel> : ExecutedModel<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly Func<bool>[] _stateConstraints;
		private readonly ActivationMinimalTransitionSetBuilder<TExecutableModel> _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModelCreator">A factory function that creates the model instance that should be executed.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal ActivationMinimalExecutedModel(CoupledExecutableModelCreator<TExecutableModel> runtimeModelCreator, int stateHeaderBytes, AnalysisConfiguration configuration)
			: this(runtimeModelCreator, stateHeaderBytes, null, configuration)
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModelCreator">A factory function that creates the model instance that should be executed.</param>
		/// <param name="formulas">The formulas that should be evaluated for each state.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		internal ActivationMinimalExecutedModel(CoupledExecutableModelCreator<TExecutableModel> runtimeModelCreator, int stateHeaderBytes, Func<bool>[] formulas, AnalysisConfiguration configuration)
			: base(runtimeModelCreator, stateHeaderBytes)
		{
			formulas = formulas ?? RuntimeModel.Formulas.Select(formula => FormulaCompilationVisitor<TExecutableModel>.Compile(RuntimeModel,formula)).ToArray();

			_transitions = new ActivationMinimalTransitionSetBuilder<TExecutableModel>(RuntimeModel, configuration.SuccessorCapacity, formulas);
			_stateConstraints = RuntimeModel.StateConstraints;

			bool useForwardOptimization;
			switch (configuration.MomentOfIndependentFaultActivation)
			{
				case MomentOfIndependentFaultActivation.AtStepBeginning:
				case MomentOfIndependentFaultActivation.OnFirstMethodWithoutUndo:
					useForwardOptimization = false;
					break;
				case MomentOfIndependentFaultActivation.OnFirstMethodWithUndo:
					useForwardOptimization = true;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			ChoiceResolver = new NondeterministicChoiceResolver(useForwardOptimization);
			
			RuntimeModel.SetChoiceResolver(ChoiceResolver);
		}

		/// <summary>
		///   Gets the size of a single transition of the model in bytes.
		/// </summary>
		public override unsafe int TransitionSize => sizeof(CandidateTransition);

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
				_transitions.SafeDispose();

			base.OnDisposing(disposing);
		}

		/// <summary>
		///   Executes an initial transition of the model.
		/// </summary>
		protected override void ExecuteInitialTransition()
		{
			foreach (var fault in RuntimeModel.NondeterministicFaults)
				fault.Reset();

			RuntimeModel.ExecuteInitialStep();
		}

		/// <summary>
		///   Executes a transition of the model.
		/// </summary>
		protected override void ExecuteTransition()
		{
			foreach (var fault in RuntimeModel.NondeterministicFaults)
				fault.Reset();

			RuntimeModel.ExecuteStep();
		}

		/// <summary>
		///   Generates a transition from the model's current state.
		/// </summary>
		protected override void GenerateTransition()
		{
			// Ignore transitions leading to a state with one or more violated state constraints
			foreach (var constraint in _stateConstraints)
			{
				if (!constraint())
					return;
			}

			_transitions.Add(RuntimeModel);
		}

		/// <summary>
		///   Invoked before the execution of the next step is started on the model, i.e., before a set of initial
		///   states or successor states is computed.
		/// </summary>
		protected override void BeginExecution()
		{
			_transitions.Clear();
		}

		/// <summary>
		///   Invoked after the execution of a step is completed on the model, i.e., after a set of initial
		///   states or successor states have been computed.
		/// </summary>
		protected override TransitionCollection EndExecution()
		{
			return _transitions.ToCollection();
		}
	}
}
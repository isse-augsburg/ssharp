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

namespace SafetySharp.Analysis.ModelChecking
{
	using System;
	using System.Linq;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Transitions;
	using Utilities;

	/// <summary>
	///   Represents an <see cref="AnalysisModel" /> that computes its state by executing a <see cref="RuntimeModel" /> with
	///   activation-minimal transitions.
	/// </summary>
	internal sealed class ActivationMinimalExecutedModel : ExecutedModel
	{
		private readonly Func<bool>[] _stateConstraints;
		private readonly ActivationMinimalTransitionSetBuilder _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModelCreator">A factory function that creates the model instance that should be executed.</param>
		/// <param name="successorStateCapacity">The maximum number of successor states supported per state.</param>
		internal ActivationMinimalExecutedModel(Func<RuntimeModel> runtimeModelCreator, int successorStateCapacity)
			: base(runtimeModelCreator)
		{
			var formulas = RuntimeModel.Formulas.Select(CompilationVisitor.Compile).ToArray();
			_transitions = new ActivationMinimalTransitionSetBuilder(RuntimeModel, successorStateCapacity, formulas);
			_stateConstraints = CollectStateConstraints();
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="runtimeModel">The model instance that should be executed.</param>
		/// <param name="successorStateCapacity">The maximum number of successor states supported per state.</param>
		internal ActivationMinimalExecutedModel(RuntimeModel runtimeModel, int successorStateCapacity)
			: base(runtimeModel)
		{
			_transitions = new ActivationMinimalTransitionSetBuilder(RuntimeModel, successorStateCapacity);
			_stateConstraints = CollectStateConstraints();
		}

		/// <summary>
		///   Gets the size of a single transition of the model in bytes.
		/// </summary>
		public override unsafe int TransitionSize => sizeof(CandidateTransition);

		/// <summary>
		///   Collects all state constraints contained in the model.
		/// </summary>
		private Func<bool>[] CollectStateConstraints()
		{
			return RuntimeModel.Model.Components.Cast<Component>().SelectMany(component => component.StateConstraints).ToArray();
		}

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
			RuntimeModel.ExecuteInitialStep();
		}

		/// <summary>
		///   Executes a transition of the model.
		/// </summary>
		protected override void ExecuteTransition()
		{
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

		/// <summary>
		///   Creates a counter example from the <paramref name="path" />.
		/// </summary>
		/// <param name="path">
		///   The path the counter example should be generated from. A value of <c>null</c> indicates that no
		///   transitions could be generated for the model.
		/// </param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public override CounterExample CreateCounterExample(byte[][] path, bool endsWithException)
		{
			if (RuntimeModelCreator == null)
				throw new InvalidOperationException("Counter example generation is not supported in this context.");

			return RuntimeModel.CreateCounterExample(RuntimeModelCreator, path, endsWithException);
		}
	}
}
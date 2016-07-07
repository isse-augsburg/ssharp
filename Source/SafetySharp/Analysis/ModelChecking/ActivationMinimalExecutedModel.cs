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
	using Runtime;
	using Transitions;
	using Utilities;

	/// <summary>
	///   Represents an <see cref="AnalysisModel" /> that computes its state by executing a <see cref="RuntimeModel" /> with
	///   activation-minimal transitions.
	/// </summary>
	internal sealed class ActivationMinimalExecutedModel : ExecutedModel
	{
		private readonly ActivationMinimalTransitionSet _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">A factory function that creates the model instance that should be executed.</param>
		/// <param name="successorStateCapacity">The maximum number of successor states supported per state.</param>
		internal ActivationMinimalExecutedModel(Func<RuntimeModel> createModel, int successorStateCapacity)
			: base(createModel)
		{
			var formulas = Model.Formulas.Select(CompilationVisitor.Compile).ToArray();
			_transitions = new ActivationMinimalTransitionSet(Model, successorStateCapacity, formulas);
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model instance that should be executed.</param>
		/// <param name="successorStateCapacity">The maximum number of successor states supported per state.</param>
		internal ActivationMinimalExecutedModel(RuntimeModel model, int successorStateCapacity)
			: base(model)
		{
			_transitions = new ActivationMinimalTransitionSet(Model, successorStateCapacity);
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
			Model.ExecuteInitialStep();
		}

		/// <summary>
		///   Executes a transition of the model.
		/// </summary>
		protected override void ExecuteTransition()
		{
			Model.ExecuteStep();
		}

		/// <summary>
		///   Generates a transition from the model's current state.
		/// </summary>
		protected override void GenerateTransition()
		{
			_transitions.Add(Model);
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
			if (CreateModel == null)
				throw new InvalidOperationException("Counter example generation is not supported in this context.");

			// We have to create new model instances to generate and initialize the counter example, otherwise hidden
			// state variables might prevent us from doing so if they somehow influence the state
			var replayModel = CreateModel();
			var counterExampleModel = CreateModel();

			Model.CopyFaultActivationStates(replayModel);
			Model.CopyFaultActivationStates(counterExampleModel);

			// Prepend the construction state to the path; if the path is null, at least one further state must be added
			// to enable counter example debugging.
			// Also, get the replay information, i.e., the nondeterministic choices that were made on the path; if the path is null,
			// we still have to get the choices that caused the problem.

			if (path == null)
			{
				path = new[] { Model.ConstructionState, new byte[Model.StateVectorSize] };
				return new CounterExample(counterExampleModel, path, new[] { Model.GetLastChoices() }, endsWithException);
			}

			path = new[] { Model.ConstructionState }.Concat(path).ToArray();
			var replayInfo = replayModel.GenerateReplayInformation(path, endsWithException);
			return new CounterExample(counterExampleModel, path, replayInfo, endsWithException);
		}
	}
}
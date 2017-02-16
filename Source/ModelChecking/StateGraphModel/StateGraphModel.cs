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
	using System.Runtime.InteropServices;
	using ISSE.SafetyChecking.ExecutableModel;
	using Runtime;
	using Transitions;
	using Utilities;

	/// <summary>
	///   Represents an <see cref="AnalysisModel" /> for <see cref="StateGraph" /> instances.
	/// </summary>
	internal sealed unsafe class StateGraphModel<TExecutableModel> : AnalysisModel<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly StateGraph<TExecutableModel> _stateGraph;
		private readonly StateGraphTransitionSetBuilder _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="stateGraph">The state graph that should be analyzed.</param>
		/// <param name="successorStateCapacity">The maximum number of successor states supported per state.</param>
		public StateGraphModel(StateGraph<TExecutableModel> stateGraph, long successorStateCapacity)
		{
			Requires.NotNull(stateGraph, nameof(stateGraph));

			_stateGraph = stateGraph;
			_transitions = new StateGraphTransitionSetBuilder(StateVectorSize, successorStateCapacity);
		}

		/// <summary>
		///   Gets the size of the model's state vector in bytes.
		/// </summary>
		public override int StateVectorSize => sizeof(int);

		/// <summary>
		///   Gets the size of a single transition of the model in bytes.
		/// </summary>
		public override int TransitionSize => _stateGraph.TransitionSize;

		/// <summary>
		///   Gets the runtime model that is directly or indirectly analyzed by this <see cref="AnalysisModel" />.
		/// </summary>
		public override TExecutableModel RuntimeModel => _stateGraph.RuntimeModel;

		/// <summary>
		///   Gets the factory function that was used to create the runtime model that is directly or indirectly analyzed by this
		///   <see cref="AnalysisModel" />.
		/// </summary>
		public override CoupledExecutableModelCreator<TExecutableModel> RuntimeModelCreator => _stateGraph.RuntimeModelCreator;

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			// Nothing to do here; ownership of _stateGraph is assumed to be shared
		}

		/// <summary>
		///   Gets all initial transitions of the model.
		/// </summary>
		public override TransitionCollection GetInitialTransitions()
		{
			return ConvertTransitions(_stateGraph.GetInitialTransitions());
		}

		/// <summary>
		///   Gets all transitions towards successor states of <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the successors should be returned for.</param>
		public override TransitionCollection GetSuccessorTransitions(byte* state)
		{
			return ConvertTransitions(_stateGraph.GetTransitions(*(int*)state));
		}

		/// <summary>
		///   Creates the appropriate <see cref="CandidateTransition" /> instances for the <paramref name="transitions" />.
		/// </summary>
		private TransitionCollection ConvertTransitions(TransitionCollection transitions)
		{
			_transitions.Clear();

			foreach (var transition in transitions)
				_transitions.Add((byte*)&transition->TargetState, transition->ActivatedFaults, transition->Formulas);

			return _transitions.ToCollection();
		}

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		public override void Reset()
		{
			// Nothing to do here
		}

		/// <summary>
		///   Creates a counter example from the <paramref name="path" />.
		/// </summary>
		/// <param name="path">
		///   The path the counter example should be generated from. A value of <c>null</c> indicates that no
		///   transitions could be generated for the model.
		/// </param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public override CounterExample<TExecutableModel> CreateCounterExample(byte[][] path, bool endsWithException)
		{
			var modelPath = path?.Select(state =>
			{
				var graphState = BitConverter.ToInt32(state, 0);
				var modelState = _stateGraph.GetState(graphState);
				var arrayState = new byte[_stateGraph.RuntimeModel.StateVectorSize];
				Marshal.Copy(new IntPtr(modelState), arrayState, 0, arrayState.Length);

				return arrayState;
			}).ToArray();

			return _stateGraph.RuntimeModel.CreateCounterExample(_stateGraph.RuntimeModelCreator, modelPath, endsWithException);
		}
	}
}
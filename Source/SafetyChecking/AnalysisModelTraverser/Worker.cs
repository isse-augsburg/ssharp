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
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using ExecutableModel;
	using AnalysisModel;
	using Utilities;

	/// <summary>
	///   Represents a thread that checks for invariant violations.
	/// </summary>
	internal sealed unsafe class Worker<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly TraversalContext<TExecutableModel> _context;
		private readonly int _index;
		private readonly StateStack _stateStack;
		private IBatchedTransitionAction<TExecutableModel>[] _batchedTransitionActions;
		private IStateAction<TExecutableModel>[] _stateActions;
		private ITransitionAction<TExecutableModel>[] _transitionActions;
		private ITransitionModifier<TExecutableModel>[] _transitionModifiers;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="index">The zero-based index of the worker.</param>
		/// <param name="context">The context the model is traversed in.</param>
		/// <param name="stateStack">The state stack that should be used by the worker.</param>
		/// <param name="model">The model that the worker should analyze.</param>
		public Worker(int index, TraversalContext<TExecutableModel> context, StateStack stateStack, AnalysisModel<TExecutableModel> model)
		{
			Requires.NotNull(context, nameof(context));
			Requires.NotNull(stateStack, nameof(stateStack));
			Requires.NotNull(model, nameof(model));

			_index = index;
			_context = context;
			_stateStack = stateStack;

			Model = model;
		}

		/// <summary>
		///   Gets the model that is analyzed by the worker.
		/// </summary>
		public AnalysisModel<TExecutableModel> Model { get; }

		/// <summary>
		///   Computes the model's initial states.
		/// </summary>
		public void ComputeInitialStates()
		{
			try
			{
				HandleTransitions(Model.GetInitialTransitions(), 0, isInitial: true);
			}
			catch (Exception e)
			{
				_context.LoadBalancer.Terminate();
				_context.Exception = e;

				if (!(e is OutOfMemoryException))
					_context.CounterExample = Model.CreateCounterExample(null, endsWithException: true);
			}
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal void Check()
		{
			try
			{
				while (_context.LoadBalancer.LoadBalance(_index))
				{
					int state;
					if (!_stateStack.TryGetState(out state))
						continue;

					HandleTransitions(Model.GetSuccessorTransitions(_context.States[state]), state, isInitial: false);

					InterlockedExtensions.ExchangeIfGreaterThan(ref _context.LevelCount, _stateStack.FrameCount, _stateStack.FrameCount);
					_context.ReportProgress();
				}
			}
			catch (Exception e)
			{
				_context.LoadBalancer.Terminate();
				_context.Exception = e;

				if (!(e is OutOfMemoryException))
					CreateCounterExample(endsWithException: true, addAdditionalState: true);
			}
		}

		/// <summary>
		///   Resets the worker so that a new check can be started.
		/// </summary>
		internal void Reset()
		{
			Model.Reset();
			_stateStack.Clear();

			// We have to initialize the following arrays here for two reasons:
			// 1) The context is not yet completely initialized when the worker's constructor executes
			// 2) We sometimes want to change some traversal parameters between multiple traversals
			_transitionActions = _context.TraversalParameters.TransitionActions.Select(a => a.Invoke()).ToArray();
			_batchedTransitionActions = _context.TraversalParameters.BatchedTransitionActions.Select(a => a.Invoke()).ToArray();
			_transitionModifiers = _context.TraversalParameters.TransitionModifiers.Select(a => a.Invoke()).ToArray();
			_stateActions = _context.TraversalParameters.StateActions.Select(a => a.Invoke()).ToArray();
		}

		/// <summary>
		///   Handles the <paramref name="transitions" />, adding newly discovered states so that they are not visited again.
		/// </summary>
		private void HandleTransitions(TransitionCollection transitions, int sourceState, bool isInitial)
		{
			try
			{
				var transitionCount = 0;
				var stateCount = 0;

				foreach (var modifier in _transitionModifiers)
					modifier.ModifyTransitions(_context, this, transitions, _context.States[sourceState], sourceState, isInitial);

				_stateStack.PushFrame();

				foreach (var transition in transitions)
				{
					int targetState;
					bool isNewState;

					if (TransitionFlags.IsToStutteringState(((CandidateTransition*)transition)->Flags))
					{
						isNewState = false;
						targetState = _context.StutteringStateIndex;
					}
					else
					{
						isNewState = _context.States.AddState(((CandidateTransition*)transition)->TargetStatePointer, out targetState);
					}

					// Replace the CandidateTransition.TargetState pointer with the unique indexes of the transition's source and target states
					transition->TargetStateIndex = targetState;
					transition->SourceStateIndex = sourceState;
					transition->Flags = TransitionFlags.SetIsStateTransformedToIndexFlag(transition->Flags);

					if (isNewState)
					{
						++stateCount;
						_stateStack.PushState(targetState);

						foreach (var action in _stateActions)
							action.ProcessState(_context, this, _context.States[targetState], targetState, isInitial);
					}

					foreach (var action in _transitionActions)
						action.ProcessTransition(_context, this, transition, isInitial);

					++transitionCount;
				}

				Interlocked.Add(ref _context.StateCount, stateCount);
				Interlocked.Add(ref _context.TransitionCount, transitionCount);
				Interlocked.Add(ref _context.ComputedTransitionCount, transitions.TotalCount);

				foreach (var action in _batchedTransitionActions)
					action.ProcessTransitions(_context, this, sourceState, transitions, transitionCount, isInitial);
			}
			catch (Exception e)
			{
				_context.LoadBalancer.Terminate();
				_context.Exception = e;

				if (!(e is OutOfMemoryException))
					CreateCounterExample(endsWithException: true, addAdditionalState: false);
			}
		}

		/// <summary>
		///   Creates a counter example for the current topmost state.
		/// </summary>
		public void CreateCounterExample(bool endsWithException, bool addAdditionalState)
		{
			if (Interlocked.CompareExchange(ref _context.GeneratingCounterExample, _index, -1) != -1)
				return;

			if (!_context.Configuration.GenerateCounterExample)
				return;

			var indexedPath = _stateStack.GetPath();
			var traceLength = addAdditionalState ? indexedPath.Length + 1 : indexedPath.Length;
			var path = new byte[traceLength][];

			if (addAdditionalState)
				path[path.Length - 1] = new byte[Model.StateVectorSize];

			for (var i = 0; i < indexedPath.Length; ++i)
			{
				path[i] = new byte[Model.StateVectorSize];
				Marshal.Copy(new IntPtr((int*)_context.States[indexedPath[i]]), path[i], 0, path[i].Length);
			}

			_context.CounterExample = Model.CreateCounterExample(path, endsWithException);
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			Model.SafeDispose();
			_stateStack.SafeDispose();
		}
	}
}
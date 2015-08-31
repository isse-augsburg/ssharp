// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace SafetySharp.Runtime
{
	using System.Threading;
	using JetBrains.Annotations;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Provides an interface for efficient enumeration of the state space of a S# <see cref="Model" />.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal sealed unsafe class StateSpaceEnumerator : DisposableObject
	{
		/// <summary>
		///   The thread-local choice resolver.
		/// </summary>
		private ThreadLocal<ChoiceResolver> _choiceResolver;

		/// <summary>
		///   The thread-local model state.
		/// </summary>
		private ThreadLocal<ModelState> _modelState;

		/// <summary>
		///   The thread-local cache of serialized states.
		/// </summary>
		private ThreadLocal<StateCache> _stateCache;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="modelInfo">The metadata about the model that should be checked.</param>
		public StateSpaceEnumerator(ModelInfo modelInfo)
		{
			Requires.NotNull(modelInfo, nameof(modelInfo));

			_choiceResolver = new ThreadLocal<ChoiceResolver>(() => new ChoiceResolver());
			_modelState = new ThreadLocal<ModelState>(() => new ModelState(_choiceResolver.Value, modelInfo));
			_stateCache = new ThreadLocal<StateCache>(() => new StateCache(StateVectorSize));
		}

		/// <summary>
		///   Gets the size of the state vector in state slots.
		/// </summary>
		public int StateVectorSize => _modelState.Value.StateVectorSize;

		/// <summary>
		///   Gets the number of transition groups.
		/// </summary>
		public int TransitionGroupCount => 1;

		/// <summary>
		///   Gets the serialized initial state of the model.
		/// </summary>
		public int* GetInitialState()
		{
			var state = _stateCache.Value.Allocate();
			_modelState.Value.Serialize(state);

			return _stateCache.Value.StateMemory;
		}

		/// <summary>
		///   Computes the next states for <paramref name="sourceState" />.
		/// </summary>
		/// <param name="sourceState">The source state the next states should be computed for.</param>
		/// <param name="transitionGroup">The number of the transition group the next states should be computed for.</param>
		public StateCache ComputeNextStates(int* sourceState, int transitionGroup)
		{
			var modelState = _modelState.Value;
			var stateCache = _stateCache.Value;
			var resolver = _choiceResolver.Value;

			stateCache.Clear();
			resolver.PrepareNextState();

//			Console.WriteLine();
//
//			for (var i = 0; i < StateVectorSize; ++i)
//			{
//				Console.Write(sourceState[i]);
//				Console.Write(",");
//			}

			while (resolver.PrepareNextPath())
			{
				modelState.Deserialize(sourceState);
				modelState.ExecuteStep();

				var targetState = stateCache.Allocate();
				modelState.Serialize(targetState);
			}

			return stateCache;
		}

		/// <summary>
		///   Checks whether the <paramref name="state" /> is marked with <paramref name="label" />.
		/// </summary>
		/// <param name="state">The state that should be checked.</param>
		/// <param name="label">The label that should be checked.</param>
		public bool CheckStateLabel(int* state, int label)
		{
			var modelState = _modelState.Value;
			modelState.Deserialize(state);

			return !modelState.CheckStateLabel(label);
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_choiceResolver.Values.SafeDisposeAll();
			_stateCache.Values.SafeDisposeAll();
		}
	}
}
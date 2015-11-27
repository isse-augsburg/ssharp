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
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Analysis;
	using CompilerServices;
	using JetBrains.Annotations;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	public sealed unsafe class RuntimeModel : DisposableObject
	{
		/// <summary>
		///   The <see cref="ChoiceResolver" /> used by the model.
		/// </summary>
		private readonly ChoiceResolver _choiceResolver;

		/// <summary>
		///   The construction state of the model.
		/// </summary>
		private readonly int[] _constructionState;

		/// <summary>
		///   Indicates whether a state is the model's construction state.
		/// </summary>
		private readonly ConstructionStateIndicator _constructionStateIndicator = new ConstructionStateIndicator();

		/// <summary>
		///   Deserializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   The objects contained in the model that require nondeterministic initialization.
		/// </summary>
		private readonly INondeterministicInitialization[] _nondeterministicInitializations;

		/// <summary>
		///   Serializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _serialize;

		/// <summary>
		///   The <see cref="StateCache" /> used by the model.
		/// </summary>
		private readonly StateCache _stateCache;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="rootComponents">The root components of the model.</param>
		/// <param name="objectTable">The table of objects referenced by the model.</param>
		/// <param name="stateFormulas">The state formulas of the model.</param>
		internal RuntimeModel(Component[] rootComponents, ObjectTable objectTable, StateFormula[] stateFormulas)
		{
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(stateFormulas, nameof(stateFormulas));

			// Create a local object table just for the objects referenced by the model; only these objects
			// have to be serialized and deserialized. The local object table does not contain, for instance,
			// the closure types of the state formulas
			var objects = rootComponents.SelectMany(obj => SerializationRegistry.Default.GetReferencedObjects(obj, SerializationMode.Optimized));
			objects = new[] { _constructionStateIndicator }.Concat(objects);

			// The construction state indicator is the first object in the table; its corresponding state slot will be 0
            var localObjectTable = new ObjectTable(objects);

			RootComponents = rootComponents;
			Faults = localObjectTable.OfType<Fault>().ToArray();
			StateFormulas = stateFormulas;
			StateSlotCount = SerializationRegistry.Default.GetStateSlotCount(localObjectTable, SerializationMode.Optimized);

			_deserialize = SerializationRegistry.Default.CreateStateDeserializer(localObjectTable, SerializationMode.Optimized);
			_serialize = SerializationRegistry.Default.CreateStateSerializer(localObjectTable, SerializationMode.Optimized);

			_stateCache = new StateCache(StateSlotCount);
			_choiceResolver = new ChoiceResolver(objectTable);
			_nondeterministicInitializations = localObjectTable.OfType<INondeterministicInitialization>().ToArray();

			PortBinding.BindAll(objectTable);

			_constructionState = new int[StateSlotCount];
			fixed (int* state = _constructionState)
				Serialize(state);
		}

		/// <summary>
		///   Gets the number of slots in the state vector.
		/// </summary>
		internal int StateSlotCount { get; }

		/// <summary>
		///   Gets the root components of the model.
		/// </summary>
		public Component[] RootComponents { get; }

		/// <summary>
		///   Gets the faults contained in the model.
		/// </summary>
		public Fault[] Faults { get; }

		/// <summary>
		///   Gets the state labels of the model.
		/// </summary>
		internal StateFormula[] StateFormulas { get; }

		/// <summary>
		///   Gets the number of transition groups.
		/// </summary>
		internal int TransitionGroupCount => 1;

		/// <summary>
		///   Deserializes the model's state from <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be deserialized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Deserialize(int* serializedState)
		{
			_deserialize(serializedState);
		}

		/// <summary>
		///   Serializes the model's state to <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The memory region the model's state should be serialized into.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Serialize(int* serializedState)
		{
			_serialize(serializedState);
		}

		/// <summary>
		///   Resets the model to one of its initial states.
		/// </summary>
		public void Reset()
		{
			fixed (int* state = _constructionState)
			{
				Deserialize(state);

				foreach (var obj in _nondeterministicInitializations)
					obj.Initialize();
			}
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ExecuteStep()
		{
			foreach (var fault in Faults)
				fault.Update();

			foreach (var component in RootComponents)
				component.Update();
		}

		/// <summary>
		///   Checks whether the state expression identified by the <paramref name="label" /> holds for the model's current state.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be used to check the <paramref name="label" />.</param>
		/// <param name="label">The label that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool CheckStateLabel(int* serializedState, int label)
		{
			Deserialize(serializedState);
			return StateFormulas[label].Expression();
		}

		/// <summary>
		///   Gets the serialized construction state of the model if the model checker does not support multiple initial states.
		///   The construction state is guaranteed to be different from all other model states.
		/// </summary>
		internal int* GetConstructionState()
		{
			fixed (int* state = _constructionState)
				Deserialize(state);

			_stateCache.Clear();
			Serialize(_stateCache.Allocate());

			return _stateCache.StateMemory;
		}

		/// <summary>
		///   Computes the initial states of the model.
		/// </summary>
		internal StateCache ComputeInitialStates()
		{
			_stateCache.Clear();
			_choiceResolver.PrepareNextState();

			fixed (int* state = _constructionState)
			{
				while (_choiceResolver.PrepareNextPath())
				{
					Deserialize(state);

					foreach (var obj in _nondeterministicInitializations)
						obj.Initialize();

					_constructionStateIndicator.RequiresInitialization = false;
					Serialize(_stateCache.Allocate());
				}
			}

			return _stateCache;
		}

		/// <summary>
		///   Computes the successor states for <paramref name="sourceState" />.
		/// </summary>
		/// <param name="sourceState">The source state the next states should be computed for.</param>
		/// <param name="transitionGroup">The transition group the next states should be computed for.</param>
		internal StateCache ComputeSuccessorStates(int* sourceState, int transitionGroup)
		{
			_stateCache.Clear();
			_choiceResolver.PrepareNextState();

			while (_choiceResolver.PrepareNextPath())
			{
				Deserialize(sourceState);
				ExecuteStep();
				Serialize(_stateCache.Allocate());
			}

			return _stateCache;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			_stateCache.SafeDispose();
			_choiceResolver.SafeDispose();
		}

		/// <summary>
		///   Represents a state value that is unique for the unique construction state of the model.
		/// </summary>
		private class ConstructionStateIndicator
		{
			/// <summary>
			///   Indicates whether the model has not yet been initialized.
			/// </summary>
			[UsedImplicitly]
			public bool RequiresInitialization = true;
		}
	}
}
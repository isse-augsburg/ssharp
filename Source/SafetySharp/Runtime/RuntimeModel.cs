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

namespace SafetySharp.Runtime
{
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Analysis;
	using CompilerServices;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	public sealed unsafe class RuntimeModel : DisposableObject
	{
		/// <summary>
		///   The unique name of the construction state.
		/// </summary>
		public const string ConstructionStateName = "constructionState259C2EE0D9884B92989DF442BA268E8E";

		/// <summary>
		///   The <see cref="ChoiceResolver" /> used by the model.
		/// </summary>
		private readonly ChoiceResolver _choiceResolver;

		/// <summary>
		///   The construction state of the model.
		/// </summary>
		private readonly byte[] _constructionState;

		/// <summary>
		///   Deserializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   The objects referenced by the model.
		/// </summary>
		private readonly ObjectTable _objects;

		/// <summary>
		///   Serializes a state of the model.
		/// </summary>
		private readonly SerializationDelegate _serialize;

		/// <summary>
		///   The <see cref="StateCache" /> used by the model.
		/// </summary>
		private readonly StateCache _stateCache;

		/// <summary>
		///   The number of bytes reserved at the beginning of each state vector by the model checker tool.
		/// </summary>
		private readonly int _stateHeaderBytes;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="buffer">The buffer the model was deserialized from.</param>
		/// <param name="rootComponents">The root components of the model.</param>
		/// <param name="objectTable">The table of objects referenced by the model.</param>
		/// <param name="formulas">The formulas that are checked on the model.</param>
		/// <param name="stateHeaderBytes">
		///   The number of bytes that should be reserved at the beginning of each state vector for the model checker tool.
		/// </param>
		internal RuntimeModel(byte[] buffer, Component[] rootComponents, ObjectTable objectTable, Formula[] formulas, int stateHeaderBytes)
		{
			Requires.NotNull(buffer, nameof(buffer));
			Requires.NotNull(rootComponents, nameof(rootComponents));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(formulas, nameof(formulas));
			Requires.That(stateHeaderBytes % 4 == 0, nameof(stateHeaderBytes), "Expected a multiple of 4.");

			SerializedModel = buffer;
			RootComponents = rootComponents;
			Faults = objectTable.OfType<Fault>().Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();
			StateFormulas = objectTable.OfType<StateFormula>().ToArray();
			Formulas = formulas;

			// Create a local object table just for the objects referenced by the model; only these objects
			// have to be serialized and deserialized. The local object table does not contain, for instance,
			// the closure types of the state formulas
			var objects = rootComponents.SelectMany(obj => SerializationRegistry.Default.GetReferencedObjects(obj, SerializationMode.Optimized));
			objects = objects.Except(objectTable.OfType<Fault>().Where(fault => fault.Activation != Activation.Nondeterministic));
			_objects = new ObjectTable(objects);

			StateVectorLayout = SerializationRegistry.Default.GetStateVectorLayout(_objects, SerializationMode.Optimized);

			_deserialize = StateVectorLayout.CreateDeserializer();
			_serialize = StateVectorLayout.CreateSerializer();

			_stateHeaderBytes = stateHeaderBytes;
			_stateCache = new StateCache(StateVectorSize);
			_choiceResolver = new ChoiceResolver(objectTable);

			PortBinding.BindAll(objectTable);

			_constructionState = new byte[StateVectorSize];
			fixed (byte* state = _constructionState)
				Serialize(state);
		}

		/// <summary>
		///   Gets the buffer the model was deserialized from.
		/// </summary>
		internal byte[] SerializedModel { get; }

		/// <summary>
		///   Gets the model's <see cref="StateVectorLayout" />.
		/// </summary>
		internal StateVectorLayout StateVectorLayout { get; }

		/// <summary>
		///   The formulas that are checked on the model.
		/// </summary>
		public Formula[] Formulas { get; }

		/// <summary>
		///   Gets the size of the state vector in bytes. The size is always a multiple of 4.
		/// </summary>
		internal int StateVectorSize => StateVectorLayout.SizeInBytes + _stateHeaderBytes;

		/// <summary>
		///   Gets the root components of the model.
		/// </summary>
		public Component[] RootComponents { get; }

		/// <summary>
		///   Gets the faults contained in the model.
		/// </summary>
		public Fault[] Faults { get; }

		/// <summary>
		///   Gets the state formulas of the model.
		/// </summary>
		internal StateFormula[] StateFormulas { get; }

		/// <summary>
		///   Deserializes the model's state from <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be deserialized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Deserialize(byte* serializedState)
		{
			_deserialize(serializedState + _stateHeaderBytes);
		}

		/// <summary>
		///   Serializes the model's state to <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The memory region the model's state should be serialized into.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Serialize(byte* serializedState)
		{
			_serialize(serializedState + _stateHeaderBytes);
		}

		/// <summary>
		///   Resets the model to one of its initial states.
		/// </summary>
		internal void Reset()
		{
			fixed (byte* state = _constructionState)
			{
				Deserialize(state);

				foreach (var obj in _objects.OfType<IInitializable>())
					obj.Initialize();
			}
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ExecuteStep()
		{
			foreach (var fault in Faults)
				fault.Reset();

			foreach (var component in RootComponents)
				component.Update();
		}

		/// <summary>
		///   Checks whether the state formula identified by the zero-based <paramref name="formulaIndex" /> holds for the model's
		///   current state.
		/// </summary>
		/// <param name="formulaIndex">The zero-based index of the formula that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CheckStateFormula(int formulaIndex)
		{
			return StateFormulas[formulaIndex].Expression();
		}

		/// <summary>
		///   Checks whether the state formula identified by the zero-based <paramref name="formulaIndex" /> holds for the model's
		///   <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be used to check the formula.</param>
		/// <param name="formulaIndex">The zero-based index of the formula that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool CheckStateLabel(byte* serializedState, int formulaIndex)
		{
			Deserialize(serializedState);
			return StateFormulas[formulaIndex].Expression();
		}

		/// <summary>
		///   Gets the serialized construction state of the model if the model checker does not support multiple initial states.
		///   The construction state is guaranteed to be different from all other model states.
		/// </summary>
		internal byte* GetConstructionState()
		{
			fixed (byte* state = _constructionState)
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

			fixed (byte* state = _constructionState)
			{
				while (_choiceResolver.PrepareNextPath())
				{
					Deserialize(state);

					foreach (var obj in _objects.OfType<IInitializable>())
						obj.Initialize();

					Serialize(_stateCache.Allocate());
				}
			}

			return _stateCache;
		}

		/// <summary>
		///   Computes the successor states for <paramref name="sourceState" />.
		/// </summary>
		/// <param name="sourceState">The source state the next states should be computed for.</param>
		internal StateCache ComputeSuccessorStates(byte* sourceState)
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
			if (!disposing)
				return;

			_choiceResolver.SafeDispose();
			_stateCache.SafeDispose();
		}
	}
}
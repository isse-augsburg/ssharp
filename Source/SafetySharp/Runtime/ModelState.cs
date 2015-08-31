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
	using System;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes and deserializes the state of a model.
	/// </summary>
	internal sealed unsafe class ModelState
	{
		/// <summary>
		///   The delegate for the generated method that deserializes the state of the model.
		/// </summary>
		private readonly SerializationDelegate _deserialize;

		/// <summary>
		///   The model that is being checked.
		/// </summary>
		private readonly Model _model;

		/// <summary>
		///   The delegate for the generated method that serializes the state of the model.
		/// </summary>
		private readonly SerializationDelegate _serialize;

		/// <summary>
		///   The expressions that have to be evaluated for different states of the model.
		/// </summary>
		private readonly Func<bool>[] _stateExpressions = null;

		/// <summary>
		///   The delegate for the generated method that updates the state of the model.
		/// </summary>
		private Action<object[]> _update;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="choiceResolver">The choice resolver that should be used to resolve nondeterministic choices.</param>
		/// <param name="modelInfo">The metadata about the model that should be checked.</param>
		public ModelState(ChoiceResolver choiceResolver, ModelInfo modelInfo)
		{
			Requires.NotNull(choiceResolver, nameof(choiceResolver));
			Requires.NotNull(modelInfo, nameof(modelInfo));

			_model = modelInfo.Model;
			_deserialize = _model.SerializationRegistry.GenerateDeserializationDelegate();
			_serialize = _model.SerializationRegistry.GenerateSerializationDelegate();

			ComputeStateVectorSize();
			GenerateUpdateCode();
			SetChoiceResolver(choiceResolver);
		}

		/// <summary>
		///   Gets the size of the state vector in state slots.
		/// </summary>
		public int StateVectorSize { get; private set; }

		/// <summary>
		///   Deserializes the model's state from <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The state of the model that should be deserialized.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Deserialize(int* serializedState)
		{
			_deserialize(serializedState, _model.ObjectTable.Objects);
		}

		/// <summary>
		///   Serializes the model's state to <paramref name="serializedState" />.
		/// </summary>
		/// <param name="serializedState">The memory region the model's state should be serialized into.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Serialize(int* serializedState)
		{
			_serialize(serializedState, _model.ObjectTable.Objects);
		}

		/// <summary>
		///   Updates the state of the model by executing a single step.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ExecuteStep()
		{
			_update(_model.ObjectTable.Objects);
		}

		/// <summary>
		///   Checks whether the state expression identified by the <paramref name="label" /> holds for the model's current state.
		/// </summary>
		/// <param name="label">The label that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool CheckStateLabel(int label)
		{
			return _stateExpressions[label]();
		}

		/// <summary>
		///   Computes the size of the state vector.
		/// </summary>
		private void ComputeStateVectorSize()
		{
			StateVectorSize = _model.ObjectTable.Objects.Sum(obj => _model.SerializationRegistry.GetStateSlotCount(obj));
		}

		/// <summary>
		///   Generates the code that updates the model's state.
		/// </summary>
		private void GenerateUpdateCode()
		{
			var generator = new ExecuteStepGenerator(_model);
			_update = generator.Compile();
		}

		/// <summary>
		///   Sets the choice resolver for all <see cref="Choice" /> instances within <see cref="_model" />.
		/// </summary>
		private void SetChoiceResolver(ChoiceResolver choiceResolver)
		{
			// TODO: That's a hack
			foreach (var choice in from obj in _model.ObjectTable.Objects
								   from field in obj.GetType().GetFields(typeof(object))
								   where field.FieldType == typeof(Choice)
								   select (Choice)field.GetValue(obj))
			{
				choice.Resolver = choiceResolver;
			}
		}
	}
}
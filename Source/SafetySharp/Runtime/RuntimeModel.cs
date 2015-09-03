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
	using Modeling;
	using Analysis;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a runtime model that can be used for model checking or simulation.
	/// </summary>
	internal sealed class RuntimeModel : DisposableObject
	{
		/// <summary>
		///   The deserializer for the model.
		/// </summary>
		private SerializationDelegate _deserializer;

		/// <summary>
		///   The  serializer for the model.
		/// </summary>
		private SerializationDelegate _serializer;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The underlying <see cref="Modeling.Model" /> instance.</param>
		/// <param name="objectTable">The table of objects referenced by the model.</param>
		/// <param name="stateFormulas">The state formulas of the model.</param>
		public unsafe RuntimeModel(Model model, ObjectTable objectTable, StateFormula[] stateFormulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(stateFormulas, nameof(stateFormulas));

			Model = model;
			StateFormulas = stateFormulas;
			_deserializer = model.SerializationRegistry.CreateStateDeserializer(objectTable, SerializationMode.Optimized);
			_serializer = model.SerializationRegistry.CreateStateSerializer(objectTable, SerializationMode.Optimized);
		}

		/// <summary>
		///   Gets the underlying model.
		/// </summary>
		public Model Model { get; }

		/// <summary>
		///   Gets the state labels of the model.
		/// </summary>
		public StateFormula[] StateFormulas { get; }

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
		}
	}
}
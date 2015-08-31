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

namespace SafetySharp.Modeling
{
	using Runtime;
	using Utilities;

	/// <summary>
	///   Represents a model of a safety-critical system.
	/// </summary>
	public class Model
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Model()
		{
			SerializationRegistry = new SerializationRegistry(this);
		}

		/// <summary>
		///   Gets the serialization registry that can be used to register customized state serializers.
		/// </summary>
		public SerializationRegistry SerializationRegistry { get; }

		/// <summary>
		///   Gets the object lookup table that can be used to map between serialized objects and identifiers.
		/// </summary>
		internal ObjectTable ObjectTable { get; private set; }

		/// <summary>
		///   Adds the <see cref="rootComponent" /> to the model.
		/// </summary>
		/// <param name="rootComponent">The root component that should be added.</param>
		public void AddRootComponent(IComponent rootComponent)
		{
			Requires.CompilationTransformation();
		}

		/// <summary>
		///   Adds the <see cref="rootComponents" /> to the model.
		/// </summary>
		/// <param name="rootComponents">The root components that should be added.</param>
		public void AddRootComponents(params IComponent[] rootComponents)
		{
			Requires.CompilationTransformation();
		}
	}
}
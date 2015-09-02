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
	using System.Collections.Generic;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model of a safety-critical system.
	/// </summary>
	public class Model
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="rootComponents">The model's root components.</param>
		public Model(params IComponent[] rootComponents)
		{
			Requires.NotNull(rootComponents, nameof(rootComponents));
			RootComponents.AddRange(rootComponents);
		}

		/// <summary>
		///   Gets the model's root components.
		/// </summary>
		public List<IComponent> RootComponents { get; } = new List<IComponent>();

		/// <summary>
		///   Gets the <see cref="SerializationRegistry" /> that can be used to register customized state serializers.
		/// </summary>
		public SerializationRegistry SerializationRegistry { get; } = new SerializationRegistry();
	}
}
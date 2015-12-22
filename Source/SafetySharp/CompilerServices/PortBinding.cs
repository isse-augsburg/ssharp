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

namespace SafetySharp.CompilerServices
{
	using System.Collections.Generic;
	using System.Linq;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Used by the S# compiler to establish bindings between <see cref="Component" /> ports.
	/// </summary>
	[Hidden, NonDiscoverable]
	public sealed class PortBinding
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="requiredPort">The reference to the required port that should be bound.</param>
		/// <param name="providedPort">The reference to the provided port that should be bound.</param>
		public PortBinding(PortReference requiredPort, PortReference providedPort)
		{
			Requires.NotNull(requiredPort, nameof(requiredPort));
			Requires.NotNull(providedPort, nameof(providedPort));

			RequiredPort = requiredPort;
			ProvidedPort = providedPort;

			var metadataAttribute = BindingMetadataAttribute.Get(RequiredPort.GetMethod());
			metadataAttribute.BindingField.SetValue(requiredPort.Component, this);
		}

		/// <summary>
		///   Gets the reference to the bound provided port.
		/// </summary>
		public PortReference ProvidedPort { get; }

		/// <summary>
		///   Gets the reference to the bound required port.
		/// </summary>
		public PortReference RequiredPort { get; }

		/// <summary>
		///   Establishes the binding.
		/// </summary>
		public void Bind()
		{
			var metadataAttribute = BindingMetadataAttribute.Get(RequiredPort.GetMethod());
			var delegateField = metadataAttribute.DelegateField;
			var providedPortDelegate = ProvidedPort.CreateDelegate(delegateField.FieldType);

			delegateField.SetValue(RequiredPort.Component, providedPortDelegate);
		}

		/// <summary>
		///   Binds all <see cref="PortBinding" /> instances found in the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects that should be bound.</param>
		internal static void BindAll(IEnumerable<object> objects)
		{
			Requires.NotNull(objects, nameof(objects));

			// Set all bindings that were initialized at model construction time
			foreach (var binding in objects.OfType<PortBinding>())
				binding.Bind();

			// Initialize all runtime bindings
			foreach (var component in objects.OfType<Component>())
				component.CreateBindings();
		}
	}
}
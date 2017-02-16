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

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using System.Linq;
	using System.Reflection;
	using Utilities;
	using ISSE.SafetyChecking.Utilities;

	/// <summary>
	///   Provides metadata about <see cref="Delegate" /> instances for serialization.
	/// </summary>
	internal class DelegateMetadata
	{
		[NonSerialized]
		private readonly Delegate _delegate;

		private readonly Type _delegateType;
		private readonly MethodInfo[] _methods;
		private readonly object[] _targets;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="d">The delegate the info object should be created for.</param>
		public DelegateMetadata(Delegate d)
		{
			Requires.NotNull(d, nameof(d));

			_delegate = d;
			_delegateType = d.GetType();

			var list = d.GetInvocationList();
			_targets = list.Select(info => info.Target).ToArray();
			_methods = list.Select(info => info.Method).ToArray();
		}

		/// <summary>
		///   Gets the delegate the metadata is provided for.
		/// </summary>
		public Delegate Delegate => _delegate;

		/// <summary>
		///   Gets or sets the identifier of the delegate object used to identify the delegate during serialization.
		/// </summary>
		public ushort ObjectIdentifier { get; set; }

		/// <summary>
		///   Creates a <see cref="Delegate" /> instance from the metadata.
		/// </summary>
		public Delegate CreateDelegate()
		{
			if (_targets.Length == 0)
				return null;

			var d = Delegate.CreateDelegate(_delegateType, _targets[0], _methods[0]);
			for (var i = 1; i < _targets.Length; ++i)
				d = Delegate.Combine(d, Delegate.CreateDelegate(_delegateType, _targets[i], _methods[i]));

			return d;
		}
	}
}
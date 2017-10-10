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

namespace SafetySharp.Odp
{
	using System;

	/// <summary>
	///  Provides a default base class for <see cref="ICapability"/> implementations.
	/// </summary>
	/// <typeparam name="T">The actual capability type, as in <c>class MyCapability : Capability&lt;MyCapability&gt;</c></typeparam>
	public abstract class Capability<T> : ICapability
		where T : Capability<T>
	{
		/// <summary>
		///  Executes the capability by assuming <paramref name="agent"/> implements <see cref="ICapabilityHandler{T}"/>
		///  and delegating to its <see cref="ICapabilityHandler{T}.ApplyCapability"/> method.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if <paramref name="agent"/> does not implement <see cref="ICapabilityHandler{T}"/>.</exception>
		public void Execute(BaseAgent agent)
		{
			var handler = agent as ICapabilityHandler<T>;
			if (handler == null)
				throw new InvalidOperationException($"Agent of type {agent.GetType().Name} cannot handle capability of type {typeof(T).Name}");
			handler.ApplyCapability((T)this);
		}

		public abstract CapabilityType CapabilityType { get; }
	}
}

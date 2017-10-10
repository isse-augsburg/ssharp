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
	/// <summary>
	///  Represents an action that is applied to a <see cref="Resource"/> as part of its processing.
	/// </summary>
	/// <remarks>
	///  Implementing classes should also override <c>Equals</c>.
	/// </remarks>
	public interface ICapability
	{
		/// <summary>
		///  Represents application of the capability by a <see cref="BaseAgent"/>, to the <paramref name="agent"/>'s <see cref="BaseAgent.Resource"/>.
		/// </summary>
		void Execute(BaseAgent agent);

		/// <summary>
		///  Indicates the type of capability.
		/// </summary>
		CapabilityType CapabilityType { get; }
	}
}

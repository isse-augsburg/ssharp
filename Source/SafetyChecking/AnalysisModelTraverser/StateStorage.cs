// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.AnalysisModelTraverser
{
	using System;
	using System.Threading;
	using Utilities;

	/// <summary>
	///   Stores the serialized states of an <see cref="AnalysisModel" />.
	/// </summary>
	/// <remarks>
	///   We store states in a contiguous array, indexed by the state's hash. The hashes are stored in a separate array,
	///   using open addressing, see Laarman, "Scalable Multi-Core Model Checking", Algorithm 2.3.
	/// </remarks>
	internal abstract unsafe class StateStorage : DisposableObject
	{

		/// <summary>
		///   Gets the state at the given zero-based <paramref name="index" />.
		/// </summary>
		/// <param name="index">The index of the state that should be returned.</param>
		public abstract byte* this[int index] { get; }

		/// <summary>
		///   Reserve a state index in StateStorage. Must not be called after AddState has been called.
		/// </summary>
		internal abstract int ReserveStateIndex();
		
		/// <summary>
		///   The length in bytes of the state vector of the analysis model with the extra bytes
		///   required for the traversal.
		/// </summary>
		public abstract int StateVectorSize { get; }

		/// <summary>
		///   Adds the <paramref name="state" /> to the cache if it is not already known. Returns <c>true</c> to indicate that the state
		///   has been added. This method can be called simultaneously from multiple threads.
		/// </summary>
		/// <param name="state">The state that should be added.</param>
		/// <param name="index">Returns the unique index of the state.</param>
		public abstract bool AddState(byte* state, out int index);

		/// <summary>
		///   Clears all stored states.
		/// </summary>
		internal abstract void Clear(int traversalModifierStateVectorSize);

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected abstract override void OnDisposing(bool disposing);
	}
}
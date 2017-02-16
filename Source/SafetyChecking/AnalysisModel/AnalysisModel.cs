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

namespace ISSE.SafetyChecking.AnalysisModel
{
	using ExecutableModel;
	using Utilities;

	/// <summary>
	///   Represents a common interface for models that can be analyzed with the model checking infrastructure.
	/// </summary>
	internal abstract unsafe class AnalysisModel<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   Gets the size of the model's state vector in bytes.
		/// </summary>
		public abstract int StateVectorSize { get; }

		/// <summary>
		///   Gets the size of a single transition of the model in bytes.
		/// </summary>
		public abstract int TransitionSize { get; }

		/// <summary>
		///   Gets the runtime model that is directly or indirectly analyzed by this <see cref="AnalysisModel" />.
		/// </summary>
		public abstract TExecutableModel RuntimeModel { get; }

		/// <summary>
		///   Gets the factory function that was used to create the runtime model that is directly or indirectly analyzed by this
		///   <see cref="AnalysisModel" />.
		/// </summary>
		public abstract CoupledExecutableModelCreator<TExecutableModel> RuntimeModelCreator { get; }

		/// <summary>
		///   Gets all initial transitions of the model.
		/// </summary>
		public abstract TransitionCollection GetInitialTransitions();

		/// <summary>
		///   Gets all transitions towards successor states of <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the successors should be returned for.</param>
		public abstract TransitionCollection GetSuccessorTransitions(byte* state);

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		///   Creates a counter example from the <paramref name="path" />.
		/// </summary>
		/// <param name="path">
		///   The path the counter example should be generated from. A value of <c>null</c> indicates that no
		///   transitions could be generated for the model.
		/// </param>
		/// <param name="endsWithException">Indicates whether the counter example ends with an exception.</param>
		public abstract CounterExample<TExecutableModel> CreateCounterExample(byte[][] path, bool endsWithException);
	}
}
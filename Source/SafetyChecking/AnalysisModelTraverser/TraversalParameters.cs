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

namespace SafetySharp.Analysis.ModelChecking.ModelTraversal
{
	using System;
	using System.Collections.Generic;
	using Runtime;
	using TraversalModifiers;

	/// <summary>
	///   Provides parameters for the model traversal process.
	/// </summary>
	internal sealed class TraversalParameters<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   Factory methods for <see cref="ITransitionAction" /> instances that should be executed by all <see cref="Worker" />
		///   instances.
		/// </summary>
		internal readonly List<Func<ITransitionAction<TExecutableModel>>> TransitionActions = new List<Func<ITransitionAction<TExecutableModel>>>();

		/// <summary>
		///   Factory methods for <see cref="IBatchedTransitionAction" /> instances that should be executed by all
		///   <see cref="Worker" /> instances.
		/// </summary>
		internal readonly List<Func<IBatchedTransitionAction<TExecutableModel>>> BatchedTransitionActions = new List<Func<IBatchedTransitionAction<TExecutableModel>>>();

		/// <summary>
		///   Factory methods for <see cref="ITransitionModifier" /> instances that should be executed by all <see cref="Worker" />
		///   instances.
		/// </summary>
		internal readonly List<Func<ITransitionModifier<TExecutableModel>>> TransitionModifiers = new List<Func<ITransitionModifier<TExecutableModel>>>();

		/// <summary>
		///   Factory methods for <see cref="IStateAction" /> instances that should be executed by all <see cref="Worker" />
		///   instances.
		/// </summary>
		internal readonly List<Func<IStateAction<TExecutableModel>>> StateActions = new List<Func<IStateAction<TExecutableModel>>>();
	}
}
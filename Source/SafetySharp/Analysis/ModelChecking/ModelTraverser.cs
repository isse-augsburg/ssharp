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

namespace SafetySharp.Analysis.ModelChecking
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using ModelTraversal;
	using Transitions;
	using Utilities;

	/// <summary>
	///   A base class for model traversers that travserse an <see cref="AnalysisModel" /> to carry out certain actions.
	/// </summary>
	internal abstract class ModelTraverser : DisposableObject
	{
		private readonly LoadBalancer _loadBalancer;
		private readonly StateStorage _states;
		private readonly Worker[] _workers;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal ModelTraverser(Func<AnalysisModel> createModel, Action<string> output, AnalysisConfiguration configuration)
		{
			Requires.NotNull(createModel, nameof(createModel));
			Requires.NotNull(output, nameof(output));
			TransitionCollection.ValidateTransitionSizes();

			var tasks = new Task[configuration.CpuCount];
			var stacks = new StateStack[configuration.CpuCount];

			_loadBalancer = new LoadBalancer(stacks);
			Context = new TraversalContext(_loadBalancer, configuration, output);
			_workers = new Worker[configuration.CpuCount];

			for (var i = 0; i < configuration.CpuCount; ++i)
			{
				var index = i;
				tasks[i] = Task.Factory.StartNew(() =>
				{
					stacks[index] = new StateStack(configuration.StackCapacity);
					_workers[index] = new Worker(index, Context, stacks[index], createModel());
				});
			}

			Task.WaitAll(tasks);

			_states = new StateStorage(_workers[0].Model.StateVectorSize, configuration.StateCapacity);
			Context.States = _states;
		}

		/// <summary>
		///   Gets the context of the traversal.
		/// </summary>
		public TraversalContext Context { get; }

		/// <summary>
		///   Gets the <see cref="AnalysisModel" /> instances analyzed by the checker's <see cref="Worker" /> instances.
		/// </summary>
		public IEnumerable<AnalysisModel> AnalyzedModels => _workers.Select(worker => worker.Model);

		/// <summary>
		///   Traverses the model.
		/// </summary>
		protected void TraverseModel()
		{
			Reset();

			_workers[0].ComputeInitialStates();

			var tasks = new Task[_workers.Length];
			for (var i = 0; i < _workers.Length; ++i)
				tasks[i] = Task.Factory.StartNew(_workers[i].Check);

			Task.WaitAll(tasks);
		}

		/// <summary>
		///   Rethrows any exception thrown during the traversal process, if any.
		/// </summary>
		protected void RethrowTraversalException()
		{
			if (Context.Exception != null)
				throw new AnalysisException(Context.Exception, Context.CounterExample);
		}

		/// <summary>
		///   Resets the checker so that a new invariant check can be started.
		/// </summary>
		private void Reset()
		{
			Context.Reset();

			_states.Clear();
			_loadBalancer.Reset();

			foreach (var worker in _workers)
				worker.Reset();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_workers.SafeDisposeAll();
		}
	}
}
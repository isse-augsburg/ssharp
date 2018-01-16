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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Runtime.ExceptionServices;
	using System.Threading.Tasks;
	using ExecutableModel;
	using AnalysisModel;
	using ExecutedModel;
	using Utilities;
	using System.Globalization;

	/// <summary>
	///   A base class for model traversers that travserse an <see cref="AnalysisModel" /> to carry out certain actions.
	/// </summary>
	internal sealed class ModelTraverser : DisposableObject
	{
		public const int DeriveTransitionSizeFromModel = -1;

		private readonly LoadBalancer _loadBalancer;
		private readonly StateStorage _states;
		private readonly Worker[] _workers;
		private readonly TimeSpan _initializationTime;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal ModelTraverser(AnalysisModelCreator createModel, AnalysisConfiguration configuration, int transitionSize, bool createStutteringState)
		{
			Requires.NotNull(createModel, nameof(createModel));
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			TransitionCollection.ValidateTransitionSizes();

			var tasks = new Task[configuration.CpuCount];
			var stacks = new StateStack[configuration.CpuCount];

			_loadBalancer = new LoadBalancer(stacks);
			Context = new TraversalContext(_loadBalancer, configuration);
			_workers = new Worker[configuration.CpuCount];

			for (var i = 0; i < configuration.CpuCount; ++i)
			{
				var index = i;
				tasks[i] = Task.Factory.StartNew(() =>
				{
					stacks[index] = new StateStack(configuration.StackCapacity);
					_workers[index] = new Worker(index, Context, stacks[index], createModel.Create());
				});
			}

			Task.WaitAll(tasks);

			var firstModel = _workers[0].Model;
			if (transitionSize == DeriveTransitionSizeFromModel)
			{
				transitionSize = firstModel.TransitionSize;
			}

			if (configuration.WriteStateVectorLayout)
				firstModel.WriteStateVectorLayout(configuration.DefaultTraceOutput);

			var modelCapacity = configuration.ModelCapacity.DeriveModelByteSize(firstModel.ModelStateVectorSize, transitionSize);
			Context.ModelCapacity = modelCapacity;
			if (configuration.UseCompactStateStorage)
				_states = new CompactStateStorage(modelCapacity.SizeOfState, modelCapacity.NumberOfStates);
			else
				_states = new SparseStateStorage(modelCapacity.SizeOfState, modelCapacity.NumberOfStates);
			Context.States = _states;
			if (createStutteringState)
				Context.StutteringStateIndex = _states.ReserveStateIndex();
			_initializationTime = stopwatch.Elapsed;
			stopwatch.Stop();
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
		private void TraverseModel()
		{
			Reset();
			Context.Output?.WriteLine($"State vector of transition modifiers vector has {_workers.First().TraversalModifierStateVectorSize} bytes."); // valid only after Reset()

			_workers[0].ComputeInitialStates();
			if (_loadBalancer.IsTerminated)
				return;

			var tasks = new Task[_workers.Length];
			for (var i = 0; i < _workers.Length; ++i)
				tasks[i] = Task.Factory.StartNew(_workers[i].Check);

			Task.WaitAll(tasks);
		}


		/// <summary>
		///   Traverses the model.
		/// </summary>
		public void TraverseModelAndReport()
		{
			Requires.That(IntPtr.Size == 8, "Model traversal is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			if (!Context.Configuration.ProgressReportsOnly)
			{
				Context.Output?.WriteLine($"Traverse model using {AnalyzedModels.Count()} CPU cores.");
				Context.Output?.WriteLine($"State vector of model has {AnalyzedModels.First().ModelStateVectorSize} bytes.");
			}

			try
			{
				TraverseModel();
				RethrowTraversalException();

				if (!Context.Configuration.ProgressReportsOnly)
					Context.Report();
			}
			finally
			{
				stopwatch.Stop();

				if (!Context.Configuration.ProgressReportsOnly)
				{
					var stateCount = Context.StateCount.ToString("N0", CultureInfo.InvariantCulture);
					var transitionCount = Context.TransitionCount.ToString("N0", CultureInfo.InvariantCulture);
					var computedTransitionCount = Context.ComputedTransitionCount.ToString("N0", CultureInfo.InvariantCulture);
					var statesPerSecond = ((long)(Context.TransitionCount / stopwatch.Elapsed.TotalSeconds)).ToString("N0", CultureInfo.InvariantCulture);
					var transitionsPerSecond = ((long)(Context.StateCount / stopwatch.Elapsed.TotalSeconds)).ToString("N0", CultureInfo.InvariantCulture);

					Context.Output?.WriteLine(String.Empty);
					Context.Output?.WriteLine("===============================================");
					Context.Output?.WriteLine($"Initialization time: {_initializationTime}");
					Context.Output?.WriteLine($"Model traversal time: {stopwatch.Elapsed}");

					Context.Output?.WriteLine($"Saved states: {stateCount}");
					Context.Output?.WriteLine($"Saved transitions: {transitionCount}");
					Context.Output?.WriteLine($"Computed transitions: {computedTransitionCount}");

					Context.Output?.WriteLine($"{statesPerSecond} states per second");
					Context.Output?.WriteLine($"{transitionsPerSecond} transitions per second");

					Context.Output?.WriteLine("===============================================");
					Context.Output?.WriteLine(String.Empty);
				}
			}
		}



		/// <summary>
		///   Rethrows any exception thrown during the traversal process, if any.
		/// </summary>
		private void RethrowTraversalException()
		{
			if (Context.Exception == null)
				return;

			if (Context.Exception is ModelException)
				throw new AnalysisException(Context.Exception.InnerException, Context.CounterExample);
			if (Context.Exception is OutOfMemoryException)
				throw Context.Exception;
			ExceptionDispatchInfo.Capture(Context.Exception).Throw();
		}

		/// <summary>
		///   Resets the checker so that a new invariant check can be started.
		/// </summary>
		private void Reset()
		{
			Context.Reset();

			foreach (var worker in _workers)
				worker.Reset();

			_states.Clear(_workers[0].TraversalModifierStateVectorSize);
			_loadBalancer.Reset();
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			// StateStorage must be freed manually. Reason is that invariant checker does not free up the
			// space, because it might be necessary for other usages of the ModelTraversers (e.g. StateGraphGenerator
			// which keeps the States for the StateGraph)

			if (!disposing)
				return;

			_workers.SafeDisposeAll();
		}
	}
}
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

namespace SafetySharp.Analysis
{
	using System;
	using System.Diagnostics;
	using ModelChecking;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model checker specifically created to check S# models.
	/// </summary>
	public class SSharpChecker
	{
		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		/// <summary>
		///   Checks the invariant encoded into the model created by <paramref name="createModel" />.
		/// </summary>
		internal AnalysisResult CheckInvariant(Func<AnalysisModel> createModel, int formulaIndex)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var checker = new InvariantChecker(createModel, OutputWritten, Configuration, formulaIndex))
			{
				var result = default(AnalysisResult);
				var initializationTime = stopwatch.Elapsed;
				stopwatch.Restart();

				try
				{
					result = checker.Check();
					return result;
				}
				finally
				{
					stopwatch.Stop();

					if (!Configuration.ProgressReportsOnly)
					{
						OutputWritten?.Invoke(String.Empty);
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke($"Initialization time: {initializationTime}");
						OutputWritten?.Invoke($"Model checking time: {stopwatch.Elapsed}");

						if (result != null)
						{
							OutputWritten?.Invoke($"{(long)(result.StateCount / stopwatch.Elapsed.TotalSeconds):n0} states per second");
							OutputWritten?.Invoke($"{(long)(result.TransitionCount / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
						}

						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke(String.Empty);
					}
				}
			}
		}

		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal StateGraph GenerateStateGraph(Func<AnalysisModel> createModel, Formula[] stateFormulas)
		{
			Requires.That(IntPtr.Size == 8, "State graph generation is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			using (var checker = new StateGraphGenerator(createModel, stateFormulas, OutputWritten, Configuration))
			{
				var stateGraph = default(StateGraph);
				var initializationTime = stopwatch.Elapsed;
				stopwatch.Restart();

				try
				{
					stateGraph = checker.GenerateStateGraph();
					return stateGraph;
				}
				finally
				{
					stopwatch.Stop();

					if (!Configuration.ProgressReportsOnly)
					{
						OutputWritten?.Invoke(String.Empty);
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke($"Initialization time: {initializationTime}");
						OutputWritten?.Invoke($"State graph generation time: {stopwatch.Elapsed}");

						if (stateGraph != null)
						{
							OutputWritten?.Invoke($"{(int)(stateGraph.StateCount / stopwatch.Elapsed.TotalSeconds):n0} states per second");
							OutputWritten?.Invoke($"{(int)(stateGraph.TransitionCount / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
						}

						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke(String.Empty);
					}
				}
			}
		}

		/// <summary>
		///   Checks the invariant encoded into the model created by <paramref name="createModel" />.
		/// </summary>
		internal AnalysisResult CheckInvariant(Func<RuntimeModel> createModel, int formulaIndex)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel model = null;
			Func<AnalysisModel> createAnalysisModel = () =>
				model = new ActivationMinimalExecutedModel(createModel, Configuration.SuccessorCapacity);

			var result = CheckInvariant(createAnalysisModel, formulaIndex);
			result.StateVectorLayout = model.StateVectorLayout;

			return result;
		}

		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal StateGraph GenerateStateGraph(Func<RuntimeModel> createModel, Formula[] stateFormulas)
		{
			return GenerateStateGraph(() => new ActivationMinimalExecutedModel(createModel, Configuration.SuccessorCapacity), stateFormulas);
		}

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public AnalysisResult CheckInvariant(ModelBase model, Formula invariant)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariant, nameof(invariant));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, invariant);

			return CheckInvariant((Func<RuntimeModel>)serializer.Load, formulaIndex: 0);
		}

		/// <summary>
		///   Checks whether the <paramref name="invariants" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariants">The invariants that should be checked.</param>
		public AnalysisResult[] CheckInvariants(ModelBase model, params Formula[] invariants)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariants, nameof(invariants));
			Requires.That(invariants.Length > 0, nameof(invariants), "Expected at least one invariant.");

			var stateGraph = GenerateStateGraph(model, invariants);
			var results = new AnalysisResult[invariants.Length];

			for (var i = 0; i < invariants.Length; ++i)
				results[i] = CheckInvariant(stateGraph, invariants[i]);

			return results;
		}

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="stateGraph" />.
		/// </summary>
		/// <param name="stateGraph">The state graph that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		internal AnalysisResult CheckInvariant(StateGraph stateGraph, Formula invariant)
		{
			Requires.NotNull(stateGraph, nameof(stateGraph));
			Requires.NotNull(invariant, nameof(invariant));

			var formulaIndex = Array.IndexOf(stateGraph.StateFormulas, invariant);

			Requires.That(formulaIndex != -1, nameof(invariant),
				"The invariant cannot be analyzed over the state graph. Use the same " +
				$"'{typeof(Formula).FullName}' instance as during the construction of the state graph.");

			return CheckInvariant(() => new StateGraphModel(stateGraph, Configuration.SuccessorCapacity), formulaIndex);
		}

		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model the state graph should be generated for.</param>
		/// <param name="stateFormulas">The state formulas that should be evaluated during state graph generation.</param>
		internal StateGraph GenerateStateGraph(ModelBase model, params Formula[] stateFormulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(stateFormulas, nameof(stateFormulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, stateFormulas);

			return GenerateStateGraph((Func<RuntimeModel>)serializer.Load, stateFormulas);
		}
	}
}
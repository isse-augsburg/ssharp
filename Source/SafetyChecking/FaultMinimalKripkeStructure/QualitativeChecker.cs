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

namespace ISSE.SafetyChecking.FaultMinimalKripkeStructure
{
	using System;
	using ExecutableModel;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;
	using Formula;
	using ExecutedModel;
	using StateGraphModel;

	/// <summary>
	///   Represents a model checker specifically created to check S# models.
	/// </summary>
	public class QualitativeChecker<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
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
		internal AnalysisResult<TExecutableModel> CheckInvariant(CoupledExecutableModelCreator<TExecutableModel> createModel, Formula formula)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel<TExecutableModel>> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(createModel, 0, Configuration.SuccessorCapacity);
			var createAnalysisModel = new AnalysisModelCreator<TExecutableModel>(createAnalysisModelFunc);

			using (var checker = new InvariantChecker<TExecutableModel>(createAnalysisModel, OutputWritten, Configuration, formula))
			{
				var result = checker.Check();
				return result;
			}
		}

		/// <summary>
		///   Checks the invariant encoded into the model created by <paramref name="createModel" />.
		/// </summary>
		internal AnalysisResult<TExecutableModel> CheckInvariant(CoupledExecutableModelCreator<TExecutableModel> createModel, int formulaIndex)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel<TExecutableModel>> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(createModel, 0, Configuration.SuccessorCapacity);
			var createAnalysisModel = new AnalysisModelCreator<TExecutableModel>(createAnalysisModelFunc);

			using (var checker = new InvariantChecker<TExecutableModel>(createAnalysisModel, OutputWritten, Configuration, formulaIndex))
			{
				var result = checker.Check();
				return result;
			}
		}
		

		/// <summary>
		///   Generates a <see cref="StateGraph{TExecutableModel}" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal StateGraph<TExecutableModel> GenerateStateGraph(CoupledExecutableModelCreator<TExecutableModel> createModel)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel<TExecutableModel>> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(createModel, 0, Configuration.SuccessorCapacity);
			var createAnalysisModel = new AnalysisModelCreator<TExecutableModel>(createAnalysisModelFunc);

			using (var checker = new StateGraphGenerator<TExecutableModel>(createAnalysisModel, OutputWritten, Configuration))
			{
				var stateGraph = checker.GenerateStateGraph();
				return stateGraph;
			}
		}


		/// <summary>
		///   Checks whether the <paramref name="invariants" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="createModel">The creator for the model that should be checked.</param>
		/// <param name="invariants">The invariants that should be checked.</param>
		public AnalysisResult<TExecutableModel>[] CheckInvariants(ExecutableModelCreator<TExecutableModel> createModel, params Formula[] invariants)
		{
			Requires.NotNull(createModel, nameof(createModel));
			Requires.NotNull(invariants, nameof(invariants));
			Requires.That(invariants.Length > 0, nameof(invariants), "Expected at least one invariant.");

			var modelGenerator = createModel.Create(invariants);

			var stateGraph = GenerateStateGraph(modelGenerator);
			var results = new AnalysisResult<TExecutableModel>[invariants.Length];

			for (var i = 0; i < invariants.Length; ++i)
				results[i] = CheckInvariant(stateGraph, invariants[i]);

			return results;
		}
		

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="stateGraph" />.
		/// </summary>
		/// <param name="stateGraph">The state graph that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		internal AnalysisResult<TExecutableModel> CheckInvariant(StateGraph<TExecutableModel> stateGraph, Formula invariant)
		{
			Requires.NotNull(stateGraph, nameof(stateGraph));
			Requires.NotNull(invariant, nameof(invariant));
			
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			AnalysisModel<TExecutableModel> model = null;
			Func<AnalysisModel<TExecutableModel>> createAnalysisModelFunc = () =>
					model = new StateGraphModel<TExecutableModel>(stateGraph, Configuration.SuccessorCapacity);
			var createAnalysisModel = new AnalysisModelCreator<TExecutableModel>(createAnalysisModelFunc);

			using (var checker = new InvariantChecker<TExecutableModel>(createAnalysisModel, OutputWritten, Configuration, invariant))
			{
				var result = checker.Check();
				return result;
			}
		}
	}
}
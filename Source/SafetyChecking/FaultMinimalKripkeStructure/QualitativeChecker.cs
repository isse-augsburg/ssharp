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
	///   Represents a model checker specifically created to check executable models.
	/// </summary>
	public class QualitativeChecker<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		public CoupledExecutableModelCreator<TExecutableModel> ModelCreator { get; }

		public QualitativeChecker(CoupledExecutableModelCreator<TExecutableModel> createModel)
		{
			ModelCreator = createModel;
		}

		/// <summary>
		///   Checks the invariant encoded into the model created by <paramref name="createModel" />.
		/// </summary>
		public InvariantAnalysisResult CheckInvariant(Formula formula)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(ModelCreator, 0, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			using (var checker = new InvariantChecker(createAnalysisModel, Configuration, formula))
			{
				var result = checker.Check();
				return result;
			}
		}

		/// <summary>
		///   Checks the invariant encoded into the model created by <paramref name="createModel" />.
		/// </summary>
		public InvariantAnalysisResult CheckInvariant(int formulaIndex)
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(ModelCreator, 0, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			using (var checker = new InvariantChecker(createAnalysisModel, Configuration, formulaIndex))
			{
				var result = checker.Check();
				return result;
			}
		}
		

		/// <summary>
		///   Generates a <see cref="StateGraph{TExecutableModel}" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal StateGraph<TExecutableModel> GenerateStateGraph()
		{
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			ExecutedModel<TExecutableModel> model = null;
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new ActivationMinimalExecutedModel<TExecutableModel>(ModelCreator, 0, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			using (var checker = new StateGraphGenerator<TExecutableModel>(createAnalysisModel, Configuration))
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
		public InvariantAnalysisResult[] CheckInvariants(params Formula[] invariants)
		{
			Requires.NotNull(invariants, nameof(invariants));
			Requires.That(invariants.Length > 0, nameof(invariants), "Expected at least one invariant.");
			
			var qualitativeChecker = new QualitativeChecker<TExecutableModel>(ModelCreator);
			qualitativeChecker.Configuration = Configuration;

			var stateGraph = qualitativeChecker.GenerateStateGraph();
			var results = new InvariantAnalysisResult[invariants.Length];

			for (var i = 0; i < invariants.Length; ++i)
				results[i] = qualitativeChecker.CheckInvariant(stateGraph, invariants[i]);

			return results;
		}
		

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="stateGraph" />.
		/// </summary>
		/// <param name="stateGraph">The state graph that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		internal InvariantAnalysisResult CheckInvariant(StateGraph<TExecutableModel> stateGraph, Formula invariant)
		{
			Requires.NotNull(stateGraph, nameof(stateGraph));
			Requires.NotNull(invariant, nameof(invariant));
			
			// We have to track the state vector layout here; this will nondeterministically store some model instance of
			// one of the workers; but since all state vectors are the same, we don't care
			AnalysisModel model = null;
			Func<AnalysisModel> createAnalysisModelFunc = () =>
					model = new StateGraphModel<TExecutableModel>(stateGraph, Configuration.SuccessorCapacity);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			using (var checker = new InvariantChecker(createAnalysisModel, Configuration, invariant))
			{
				var result = checker.Check();
				return result;
			}
		}
	}
}
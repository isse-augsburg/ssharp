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
	using System.Linq;
	using ModelTraversal.TraversalModifiers;
	using Runtime;

	/// <summary>
	///   Generates a <see cref="StateGraph" /> for an <see cref="AnalysisModel" />.
	/// </summary>
	internal sealed class StateGraphGenerator<TExecutableModel> : ModelTraverser<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly StateGraph<TExecutableModel> _stateGraph;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal StateGraphGenerator(Func<AnalysisModel<TExecutableModel>> createModel, 
									 Action<string> output, AnalysisConfiguration configuration)
			: base(createModel, output, configuration)
		{
			var analyzedModel = AnalyzedModels.First();

			_stateGraph = new StateGraph<TExecutableModel>(
				Context, analyzedModel.TransitionSize,
				analyzedModel.RuntimeModel, analyzedModel.RuntimeModelCreator);

			Context.TraversalParameters.BatchedTransitionActions.Add(() => new StateGraphBuilder<TExecutableModel>(_stateGraph));
		}

		/// <summary>
		///   Generates the state graph.
		/// </summary>
		internal StateGraph<TExecutableModel> GenerateStateGraph()
		{
			Context.Output($"Generating state graph.");
			TraverseModelAndReport();

			return _stateGraph;
		}
	}
}
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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System;
	using ExecutableModel;
	using Utilities;
	using AnalysisModel;
	using Formula;
	using System.Linq;
	using ExecutedModel;

	public class MarkovChainFromExecutableModelGenerator<TExecutableModel> : MarkovChainGenerator where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly ExecutableModelCreator<TExecutableModel> _runtimeModelCreator;

		public MarkovChainFromExecutableModelGenerator(ExecutableModelCreator<TExecutableModel> runtimeModelCreator)
		{
			Requires.NotNull(runtimeModelCreator, nameof(runtimeModelCreator));
			_runtimeModelCreator = runtimeModelCreator;
		}

		public LabeledTransitionMarkovChain GenerateLabeledMarkovChain()
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			ProbabilityMatrixCreationStarted = true;
			
			FormulaManager.Calculate(Configuration);
			var stateFormulasToCheckInBaseModel = FormulaManager.StateFormulasToCheckInBaseModel.ToArray();

			ExecutedModel<TExecutableModel> model = null;
			var modelCreator = _runtimeModelCreator.CreateCoupledModelCreator(stateFormulasToCheckInBaseModel);
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new LtmcExecutedModel<TExecutableModel>(modelCreator, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);
			
			var ltmc = GenerateLtmc(createAnalysisModel);

			return ltmc;
		}

		public DiscreteTimeMarkovChain GenerateMarkovChain()
		{
			var ltmc = GenerateLabeledMarkovChain();
			return ConvertToMarkovChain(ltmc);
		}
	}
}
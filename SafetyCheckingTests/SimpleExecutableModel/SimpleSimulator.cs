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


namespace Tests.SimpleExecutableModel
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutableModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Simulator;
	using Utilities;
	
	public sealed class SimpleSimulator : Simulator<SimpleExecutableModel>
	{
		/// <summary>
		///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
		/// </summary>
		public SimpleModelBase Model => RuntimeModel.Model;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be simulated.</param>
		/// <param name="formulas">The formulas that can be evaluated on the model.</param>
		public SimpleSimulator(SimpleModelBase model, params Formula[] formulas)
			: base(SimpleExecutableModel.CreateExecutedModelCreator(model, formulas).Create(0))
		{
		}

		public SimpleSimulator(ExecutableCounterExample<SimpleExecutableModel> counterExample)
			: base(counterExample)
		{
		}
	}
	
	public sealed class SimpleProbabilisticSimulator : ProbabilisticSimulator<SimpleExecutableModel>
	{
		/// <summary>
		///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
		/// </summary>
		public SimpleModelBase Model => RuntimeModel.Model;

		public static AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be simulated.</param>
		/// <param name="formulas">The formulas that can be evaluated on the model.</param>
		public SimpleProbabilisticSimulator(SimpleModelBase model, params Formula[] formulas)
			: base(SimpleExecutableModel.CreateExecutedModelFromFormulasCreator(model), formulas, Configuration)
		{
		}
	}
}

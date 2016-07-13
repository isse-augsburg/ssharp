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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using Modeling;
	using Modeling.Controllers;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	public class FunctionalTests
	{
		[Test]
		public void ReconfigurationFailed()
		{
			var model = new Model();
			model.InitializeDefaultInstance();
			model.CreateObserverController<FastObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void DepthFirstSearch()
		{
			var model = new Model();
			model.InitializeDefaultInstance();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 20 } };
			var result = modelChecker.CheckInvariant(model, true);

			Console.WriteLine(result);
		}

		[Test]
		public void EvalIctss1()
		{
			var model = new Model();
			model.Ictss1();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss2()
		{
			var model = new Model();
			model.Ictss2();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss3()
		{
			var model = new Model();
			model.Ictss3();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss4()
		{
			var model = new Model();
			model.Ictss4();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss5()
		{
			var model = new Model();
			model.Ictss5();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss6()
		{
			var model = new Model();
			model.Ictss6();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		[Test]
		public void EvalIctss7()
		{
			var model = new Model();
			model.Ictss7();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			Dcca(model);
		}

		private void Dcca(Model model)
		{
			var safetyAnalysis = new SafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 1,
					StateCapacity = 1 << 20,
					GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly
			};

			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.ObserverController.ReconfigurationState == ReconfStates.Failed);
			Console.WriteLine(result);
		}
	}
}
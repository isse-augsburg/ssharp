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
	using System.Collections;
	using System.Linq;
	using Modeling;
	using Modeling.Controllers.Reconfiguration;
	using NUnit.Framework;
	using Odp.Reconfiguration;
	using SafetySharp.Analysis;

	using FastConfigurationFinder = Modeling.Controllers.Reconfiguration.FastConfigurationFinder;

	internal class SafetyAnalysisTests
	{
		[TestCaseSource(nameof(CreateConfigurations))]
		public void ComputeStateVectorLayout(Model model)
		{
			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 12 } };
			var result = modelChecker.CheckInvariant(model, false);

			Console.WriteLine(result.StateVectorLayout);
		}

		[TestCaseSource(nameof(CreateConfigurations))]
		public void NoDamagedWorkpieces(Model model)
		{
			var modelChecker = new SafetyAnalysis { Configuration = { StateCapacity = 1 << 12, GenerateCounterExample = false } };
			var result = modelChecker.ComputeMinimalCriticalSets(model, model.Workpieces.Any(w => w.IsDamaged), maxCardinality: 2);

			Console.WriteLine(result);
		}

		[TestCaseSource(nameof(CreateConfigurations))]
		public void AllWorkpiecesCompleteEventually(Model model)
		{
			var modelChecker = new SafetyAnalysis { Configuration = { StateCapacity = 1 << 18, GenerateCounterExample = false } };

			Formula stepCountExceeded =
				(model.Controller as IntolerableAnalysisController)?.StepCount >= IntolerableAnalysisController.MaxSteps;

			var result = modelChecker.ComputeMinimalCriticalSets(model,
				stepCountExceeded &&
				!model.Workpieces.All(w => w.IsDamaged || w.IsDiscarded || w.IsComplete), maxCardinality: 2);

			Console.WriteLine(result);
		}

		private static IEnumerable CreateConfigurations()
		{
			return SampleModels.CreateDefaultConfigurations<CentralizedController>(new FastConfigurationFinder(), AnalysisMode.IntolerableFaults)
						.Select(model => new TestCaseData(model).SetName(model.Name));
		}
	}
}
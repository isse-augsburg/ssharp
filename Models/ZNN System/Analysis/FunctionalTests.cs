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

using System;
using ISSE.SafetyChecking.ExecutedModel;
using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;
using SafetySharp.ModelChecking;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
	public class FunctionalTests
	{
		[Test]
		public void DepthFirstSearch()
		{
			var model = new Model();

			var modelChecker = new SafetySharpQualitativeChecker(model) { Configuration = { CpuCount = 1, ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium) } };
			var result = modelChecker.CheckInvariant(true);

            Console.WriteLine(result);
		}

		[Test]
		public void Evaluation()
		{
			var model = new Model(10, 10);
			var safetyAnalysis = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 4,
				    ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium),
                    GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly,
				Heuristics = { new SubsumptionHeuristic(model.Faults) }
			};

			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.ProxyObserver.ReconfigurationState == ReconfStates.Failed);
			Console.WriteLine(result);
		}
	}
}
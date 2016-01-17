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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Tests
{
	using Model;
	using NUnit.Framework;
	using SafetySharp.Runtime;
	using SafetySharp.Analysis;
	using Utilities;

	public class SpecificationTests
	{
		[Test]
		public void Simulate_1Step()
		{
			var testModel = new Specification();
			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.RootComponents[0];
		}
		[Test]
		public void Simulate_10_Step()
		{
			var testModel = new Specification();
			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.RootComponents[0];
		}

		[Test]
		public void IncomingBloodIsContaminated_ModelChecking()
		{
			var specification = new Specification();

			var analysis = new SafetyAnalysis(new LtsMin(), Model.Create(specification));
			
			var result = analysis.ComputeMinimalCutSets(specification.IncomingBloodNotOk, $"counter examples/hdmachine");
			var percentage = result.CheckedSetsCount / (float)(1 << result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);
				
		}


		[Test]
		public void DialysisFinishedAndBloodNotCleaned_ModelChecking()
		{
			var specification = new Specification();

			var analysis = new SafetyAnalysis(new LtsMin(), Model.Create(specification));

			var result = analysis.ComputeMinimalCutSets(specification.BloodNotCleanedAndDialyzingFinished, $"counter examples/hdmachine");
			var percentage = result.CheckedSetsCount / (float)(1 << result.FaultCount) * 100;

			Console.WriteLine("Faults: {0}", String.Join(", ", result.Faults.Select(fault => fault.Name)));
			Console.WriteLine();

			Console.WriteLine("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", result.CheckedSetsCount, percentage);
			Console.WriteLine("Minimal Cut Sets: {0}", result.MinimalCutSetsCount);
			Console.WriteLine();

			var i = 1;
			foreach (var cutSet in result.MinimalCutSets)
				Console.WriteLine("   ({1}) {{ {0} }}", String.Join(", ", cutSet.Select(fault => fault.Name)), i++);

		}
	}
}

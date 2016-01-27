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

namespace Elbtunnel
{
	using System;
	using System.Linq;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Reflection;

	[TestFixture]
	public class Tests
	{
		[TestCase]
		public void CollisionDcca([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis((ModelChecker)Activator.CreateInstance(modelChecker), Model.Create(specification));

			var result = analysis.ComputeMinimalCutSets(specification.Collision, $"counter examples/elbtunnel/{modelChecker.Name}");
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

		public static void Main()
		{
			new Tests().Test();
		}

		[Test]
		public void Test()
		{
			var specification = new Specification();
			var model = Model.Create(specification);
			var faults = model.GetFaults();

			for (var i = 0; i < faults.Length; ++i)
				 faults[i].Activation = i < 5 ? Activation.Nondeterministic : Activation.Suppressed;
//				 faults[i].Activation = Activation.Suppressed;
//				faults[i].Activation = Activation.Nondeterministic;

			var checker = new SSharpChecker();
			checker.CheckInvariant(model, true);

//			var ltsMin = new LtsMin();
//			ltsMin.CheckInvariant(model, true);
		}
	}
}
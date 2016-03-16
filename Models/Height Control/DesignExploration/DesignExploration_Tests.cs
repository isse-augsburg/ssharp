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

namespace SafetySharp.CaseStudies.HeightControl.DesignExploration
{
	using System;
	using Analysis;
	using NUnit.Framework;

	[TestFixture]
	public class DesignExploration_Tests
	{
		[TestCase]
		public void DesignHighTubeWithLb_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignHighTubeWithLb_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignImprovedDetectionOfPreControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignImprovedDetectionOfPreControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignRemovedCounterInTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignRemovedCounterInTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void DesignTolerantMainControl_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new DesignTolerantMainControl_Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			Console.WriteLine(result);
		}

		[TestCase]
		public void OriginalDesign_CollisionSpof([Values(typeof(SSharpChecker), typeof(LtsMin))] Type modelChecker)
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Collision, maxCardinality: 1);
			result.SaveCounterExamples("counter examples/elbtunnel/");

			Console.WriteLine(result);
		}
	}
}
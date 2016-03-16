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

namespace SafetySharp.CaseStudies.RailroadCrossing
{
	using System;
	using Analysis;
	using NUnit.Framework;

	public class ModelCheckingTests
	{
		private Model _model;
		private Specification _specification;

		[SetUp]
		private void SetUp()
		{
			_specification = new Specification();
			_model = new Model(_specification);
		}

		[Test]
		public void CollisionDcca()
		{
			var analysis = new SafetyAnalysis();
			var result = analysis.ComputeMinimalCriticalSets(new Model(_specification), _specification.PossibleCollision);

			result.SaveCounterExamples("counter examples/railroad crossing/");
			Console.WriteLine(result);
		}

		[Test]
		public void EnumerateAllStates()
		{
			var checker = new SSharpChecker();
			checker.CheckInvariant(_model, true);
		}
	}
}
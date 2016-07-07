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

namespace SafetySharp.CaseStudies.RailroadCrossing.Analysis
{
	using System;
	using FluentAssertions;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	public class ModelCheckingTests
	{
		[Test]
		public void EnumerateAllStates()
		{
			var model = new Model();

			var result = ModelChecker.CheckInvariant(model, true);
			result.FormulaHolds.Should().BeTrue();
		}

		[Test]
		public void StateGraphAllStates()
		{
			var model = new Model();

			var result = ModelChecker.CheckInvariants(model, true, false, true);
			result[0].FormulaHolds.Should().BeTrue();
			result[1].FormulaHolds.Should().BeFalse();
			result[2].FormulaHolds.Should().BeTrue();
		}

		[Test]
		public void TrainCanStopBeforeCrossing()
		{
			var model = new Model();

			var result = ModelChecker.CheckInvariant(model, !(model.Train.Position < Model.CrossingPosition && model.Train.Speed == 0));
			result.FormulaHolds.Should().BeFalse();
			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/railroad crossing/train can stop before crossing");
		}

		[Test]
		public void TrainCanPassSecuredCrossing()
		{
			var model = new Model();

			var result = ModelChecker.CheckInvariant(model, !(model.TrainIsAtCrossing && model.CrossingIsSecured));
			result.FormulaHolds.Should().BeFalse();
			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/railroad crossing/train can pass secured crossing");
		}

		[Test]
		public void TrainCanPassUnsecuredCrossing()
		{
			var model = new Model();

			var result = ModelChecker.CheckInvariant(model, !(model.TrainIsAtCrossing && !model.CrossingIsSecured));
			result.FormulaHolds.Should().BeFalse();
			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/railroad crossing/train can pass unsecured crossing");
		}
	}
}
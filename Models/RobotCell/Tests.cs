// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace RobotCell
{
	using System;
	using FluentAssertions;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Simulation;
	using static SafetySharp.Analysis.Ltl;

	[TestFixture]
	public class Tests
	{
		private readonly RobotCellModel _model;
		private readonly Spin _spin;

		public Tests()
		{
			_model = new RobotCellModel();
			_spin = new Spin(_model);
		}

		[Test]
		public void ModelCheck()
		{
			_spin.ComputeMinimalCriticalSets(_model.Robots[0].State != Robot.States.AwaitingReconfiguration);
		}

		[Test]
		public void ShouldConfigureItself()
		{
			var simulator = new Simulator(_model);

			simulator.Simulate(TimeSpan.FromSeconds(1));

			foreach (var robot in _model.Robots)
				robot.RequiresReconfiguration().Should().BeFalse();

			foreach (var cart in _model.Carts)
				cart.RequiresReconfiguration().Should().BeFalse();
		}
	}
}
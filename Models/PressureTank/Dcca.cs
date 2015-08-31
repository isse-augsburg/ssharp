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

namespace PressureTank
{
	using System.Runtime.CompilerServices;
	using FluentAssertions;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Simulation;
	using SharedComponents;

	[TestFixture]
	public class Dcca
	{
		private class Model : PressureTankModel
		{
			private LtlFormula EmptySet()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Pump.IgnoreFault<Pump.SuppressPumping>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsFull()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Pump.IgnoreFault<Pump.SuppressPumping>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsEmpty()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Pump.IgnoreFault<Pump.SuppressPumping>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressPumping()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressTimeout()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Pump.IgnoreFault<Pump.SuppressPumping>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsFull_SuppressTimeout()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Pump.IgnoreFault<Pump.SuppressPumping>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsFull_SuppressIsEmpty()
			{
				Pump.IgnoreFault<Pump.SuppressPumping>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsFull_SuppressPumping()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsEmpty>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsEmpty_SuppressPumping()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsEmpty_SuppressTimeout()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();
				Pump.IgnoreFault<Pump.SuppressPumping>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressPumping_SuppressTimeout()
			{
				Pump.IgnoreFault<Pump.SuppressPumping>();
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsFull_SuppressIsEmpty_SuppressPumping()
			{
				Timer.IgnoreFault<Timer.SuppressTimeout>();

				return Tank.IsRuptured();
			}

			private LtlFormula SuppressIsEmpty_SuppressPumping_SuppressTimeout()
			{
				Sensor.IgnoreFault<Sensor.SuppressIsFull>();

				return Tank.IsRuptured();
			}
		}

		private readonly Model _model;
		private readonly LtsMin _ltsMin;

		public Dcca()
		{
			_model = new Model();
			_ltsMin = new LtsMin(_model);
		}

		private void Check([CallerMemberName] string factory = null)
		{
			_ltsMin.CheckInvariant(factory).Should().BeTrue();
		}

		[Test]
		public void EmptySet()
		{
			Check();
		}

		[Test]
		public void SuppressIsEmpty()
		{
			Check();
		}

		[Test]
		public void SuppressIsEmpty_SuppressPumping()
		{
			Check();
		}

		[Test]
		public void SuppressIsEmpty_SuppressPumping_SuppressTimeout()
		{
			Check();
		}

		[Test]
		public void SuppressIsEmpty_SuppressTimeout()
		{
			Check();
		}

		[Test]
		public void SuppressIsFull()
		{
			Check();
		}

		[Test]
		public void SuppressIsFull_SuppressIsEmpty()
		{
			Check();
		}

		[Test]
		public void SuppressIsFull_SuppressIsEmpty_SuppressPumping()
		{
			Check();
		}

		[Test]
		public void SuppressIsFull_SuppressPumping()
		{
			Check();
		}

		[Test]
		public void SuppressIsFull_SuppressTimeout()
		{
			Check();
		}

		[Test]
		public void SuppressPumping()
		{
			Check();
		}

		[Test]
		public void SuppressPumping_SuppressTimeout()
		{
			Check();
		}

		[Test]
		public void SuppressTimeout()
		{
			Check();
		}
	}
}
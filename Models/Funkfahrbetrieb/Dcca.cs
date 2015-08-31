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

namespace Funkfahrbetrieb
{
	using System.Runtime.CompilerServices;
	using Context;
	using CrossingController;
	using FluentAssertions;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Simulation;
	using TrainController;

	[TestFixture]
	public class Dcca
	{
		private class Model : FunkfahrbetriebModel
		{
			private LtlFormula EmptySet()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped()
			{
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForTrainDropped()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BarrierStuck()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BarrierSensorBroken()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula ErroneousTrainDetection()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BrakesUnresponsive()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped_MessageForTrainDropped()
			{
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped_BarrierStuck()
			{
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped_BarrierSensorBroken()
			{
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped_ErroneousTrainDetection()
			{
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForCrossingDropped_BrakesUnresponsive()
			{
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();

				return Hazard();
			}

			private LtlFormula MessageForTrainDropped_BarrierStuck()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForTrainDropped_BarrierSensorBroken()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForTrainDropped_ErroneousTrainDetection()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula MessageForTrainDropped_BrakesUnresponsive()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();

				return Hazard();
			}

			private LtlFormula BarrierStuck_BarrierSensorBroken()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BarrierStuck_ErroneousTrainDetection()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BarrierStuck_BrakesUnresponsive()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();

				return Hazard();
			}

			private LtlFormula BarrierSensorBroken_ErroneousTrainDetection()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				Brakes.IgnoreFault<Brakes.Unresponsive>();

				return Hazard();
			}

			private LtlFormula BarrierSensorBroken_BrakesUnresponsive()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				TrainSensor.IgnoreFault<TrainSensor.ErroneousDetection>();

				return Hazard();
			}

			private LtlFormula ErroneousTrainDetection_BrakesUnresponsive()
			{
				Channel1.IgnoreFault<RadioChannel.Dropped>();
				Channel2.IgnoreFault<RadioChannel.Dropped>();
				BarrierMotor.IgnoreFault<BarrierMotor.Stuck>();
				BarrierSensor.IgnoreFault<BarrierSensor.Broken>();

				return Hazard();
			}

			private LtlFormula Hazard()
			{
				return Train.GetPosition() >= TrainControl.CrossingPosition &&
					   Train.GetPosition() <= TrainSensor.Position &&
					   Barrier.GetAngle() != 0;
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
		public void BarrierSensorBroken()
		{
			Check();
		}

		[Test]
		public void BarrierSensorBroken_ErroneousTrainDetection()
		{
			Check();
		}

		[Test]
		public void BarrierSensorBroken_BrakesUnresponsive()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped_BarrierSensorBroken()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped_MessageForTrainDropped()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped_ErroneousTrainDetection()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped_BarrierStuck()
		{
			Check();
		}

		[Test]
		public void MessageForCrossingDropped_BrakesUnresponsive()
		{
			Check();
		}

		[Test]
		public void MessageForTrainDropped()
		{
			Check();
		}

		[Test]
		public void MessageForTrainDropped_BarrierSensorBroken()
		{
			Check();
		}

		[Test]
		public void MessageForTrainDropped_ErroneousTrainDetection()
		{
			Check();
		}

		[Test]
		public void MessageForTrainDropped_BarrierStuck()
		{
			Check();
		}

		[Test]
		public void MessageForTrainDropped_BrakesUnresponsive()
		{
			Check();
		}

		[Test]
		public void EmptySet()
		{
			Check();
		}

		[Test]
		public void ErroneousTrainDetection()
		{
			Check();
		}

		[Test]
		public void ErroneousTrainDetection_BrakesUnresponsive()
		{
			Check();
		}

		[Test]
		public void BarrierStuck()
		{
			Check();
		}

		[Test]
		public void BarrierStuck_BarrierSensorBroken()
		{
			Check();
		}

		[Test]
		public void BarrierStuck_ErroneousTrainDetection()
		{
			Check();
		}

		[Test]
		public void BarrierStuck_BrakesUnresponsive()
		{
			Check();
		}

		[Test]
		public void BrakesUnresponsive()
		{
			Check();
		}
	}
}
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

namespace SafetySharp.CaseStudies.RailroadCrossing.Modeling
{
	using System;
	using Environment;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class Model : ModelBase
	{
		public const int EndPosition = 1000;
		public const int SensorPosition = 900;
		public const int CrossingPosition = 500;

		public const int SafetyMargin = 50;
		public const int CommunicationDelay = 1;
		public const int ClosingDelay = 10;
		public const int CloseTimeout = 21;
		public const int MaxSpeed = 10;
		public const int Decelaration = -1;
		public const int MaxSpeedOffset = 7;
		public const int MaxPositionOffset = 60;

		public Model()
		{
			CrossingController = new CrossingController
			{
				Sensor = new BarrierSensor(),
				Motor = new BarrierMotor(),
				Radio = new RadioModule(),
				Timer = new Timer(),
				TrainSensor = new TrainSensor()
			};

			TrainController = new TrainController
			{
				Brakes = new Brakes(),
				Odometer = new Odometer(),
				Radio = new RadioModule()
			};

			Bind(nameof(Barrier.Speed), nameof(CrossingController.Motor.Speed));
			Bind(nameof(CrossingController.Sensor.BarrierAngle), nameof(Barrier.Angle));

			Bind(nameof(Train.Acceleration), nameof(TrainController.Brakes.Acceleration));
			Bind(nameof(CrossingController.TrainSensor.TrainPosition), nameof(Train.Position));

			Bind(nameof(TrainController.Radio.RetrieveFromChannel), nameof(Channel.Receive));
			Bind(nameof(TrainController.Radio.DeliverToChannel), nameof(Channel.Send));

			Bind(nameof(CrossingController.Radio.RetrieveFromChannel), nameof(Channel.Receive));
			Bind(nameof(CrossingController.Radio.DeliverToChannel), nameof(Channel.Send));

			Bind(nameof(TrainController.Odometer.TrainPosition), nameof(Train.Position));
			Bind(nameof(TrainController.Odometer.TrainSpeed), nameof(Train.Speed));
		}

		[Root(RootKind.Controller)]
		public CrossingController CrossingController { get; }

		[Root(RootKind.Controller)]
		public TrainController TrainController { get; }

		[Root(RootKind.Plant)]
		public Train Train { get; } = new Train();

		[Root(RootKind.Plant)]
		public Barrier Barrier { get; } = new Barrier();

		[Root(RootKind.Plant)]
		public RadioChannel Channel { get; } = new RadioChannel();

		public Formula PossibleCollision => !CrossingIsSecured && TrainIsAtCrossing;

		public Formula TrainIsAtCrossing => Train.Position <= CrossingPosition && Train.Position + Train.Speed > CrossingPosition;

		public Formula CrossingIsSecured => Barrier.Angle == 0;
	}
}
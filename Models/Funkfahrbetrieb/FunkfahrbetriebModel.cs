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
	using Context;
	using CrossingController;
	using SafetySharp.Modeling;
	using TrainController;

	public class FunkfahrbetriebModel : Model
	{
		public FunkfahrbetriebModel()
		{
			var barrierRadio = new RadioModule();
			var crossingControl = new CrossingControl(BarrierMotor, BarrierSensor, TrainSensor, barrierRadio);

			var odometer = new Odometer();
			var trainRadio = new RadioModule();
			var trainControl = new TrainControl(odometer, Brakes, trainRadio);

			AddRootComponents(Channel1, Channel2, Train, Barrier, crossingControl, trainControl);

			Bind(Barrier.RequiredPorts.Speed = BarrierMotor.ProvidedPorts.GetSpeed);
			Bind(BarrierSensor.RequiredPorts.BarrierAngle = Barrier.ProvidedPorts.GetAngle);

			Bind(Train.RequiredPorts.Acceleration = Brakes.ProvidedPorts.GetAcceleration);
			Bind(TrainSensor.RequiredPorts.TrainPosition = Train.ProvidedPorts.GetPosition);

			Bind(trainRadio.RequiredPorts.RetrieveFromChannel = Channel2.ProvidedPorts.Receive);
			Bind(trainRadio.RequiredPorts.DeliverToChannel = Channel1.ProvidedPorts.Send);

			Bind(barrierRadio.RequiredPorts.RetrieveFromChannel = Channel1.ProvidedPorts.Receive);
			Bind(barrierRadio.RequiredPorts.DeliverToChannel = Channel2.ProvidedPorts.Send);

			Bind(odometer.RequiredPorts.TrainPosition = Train.ProvidedPorts.GetPosition);
			Bind(odometer.RequiredPorts.TrainSpeed = Train.ProvidedPorts.GetSpeed);
		}

		public RadioChannel Channel1 { get; } = new RadioChannel();
		public RadioChannel Channel2 { get; } = new RadioChannel();
		public TrainSensor TrainSensor { get; } = new TrainSensor(position: 950);
		public BarrierMotor BarrierMotor { get; } = new BarrierMotor();
		public Brakes Brakes { get; } = new Brakes();
		public Train Train { get; } = new Train();
		public Barrier Barrier { get; } = new Barrier();
		public BarrierSensor BarrierSensor { get; } = new BarrierSensor();
	}
}
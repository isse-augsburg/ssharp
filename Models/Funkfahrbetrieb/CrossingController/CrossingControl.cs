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

namespace Funkfahrbetrieb.CrossingController
{
	using SafetySharp.Modeling;

	public class CrossingControl : Component
	{
		private readonly BarrierMotor _motor;
		private readonly RadioModule _radio;
		private readonly BarrierSensor _sensor;
		private readonly Timer _timer;
		private readonly TrainSensor _trainSensor;

		public CrossingControl(BarrierMotor motor, BarrierSensor sensor, TrainSensor trainSensor, RadioModule radio)
		{
			_timer = new Timer(20);
			_motor = motor;
			_sensor = sensor;
			_trainSensor = trainSensor;
			_radio = radio;
		}

		public override void Update()
		{
			if (_sensor.IsOpen() || _sensor.IsClosed())
				_motor.Stop();

			if (_timer.HasElapsed() || _trainSensor.HasTrainPassed())
				_motor.Open();

			if (_radio.Receive() == Message.Close)
			{
				_motor.Close();
				_timer.Start();
			}

			if (_radio.Receive() == Message.Query && _sensor.IsClosed())
				_radio.Send(Message.Closed);
		}
	}
}
// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.RailroadCrossing.Modeling.Controllers
{
	using SafetySharp.Modeling;

	public class CrossingController : Component
	{
		private readonly StateMachine<State> _stateMachine = State.Open;

		[Hidden, Subcomponent]
		public BarrierMotor Motor;

		[Hidden, Subcomponent]
		public RadioModule Radio;

		[Hidden, Subcomponent]
		public BarrierSensor Sensor;

		[Hidden, Subcomponent]
		public Timer Timer;

		[Hidden, Subcomponent]
		public TrainSensor TrainSensor;

		public override void Update()
		{
			Update(Motor, Radio, Sensor, Timer);

			_stateMachine
				.Transition(
					from: State.Open,
					to: State.Closing,
					guard: Radio.Receive() == Message.Close,
					action: () =>
					{
						Motor.Close();
						Timer.Start();
					})
				.Transition(
					from: State.Closing,
					to: State.Closed,
					guard: Sensor.IsClosed,
					action: Motor.Stop)
				.Transition(
					from: State.Closed,
					to: State.Opening,
					guard: Timer.HasElapsed || TrainSensor.HasTrainPassed,
					action: Motor.Open)
				.Transition(
					from: State.Opening,
					to: State.Open,
					guard: Sensor.IsOpen,
					action: Motor.Stop);

			if (Radio.Receive() == Message.Query && _stateMachine == State.Closed)
				Radio.Send(Message.Closed);
		}

		private enum State
		{
			Open,
			Closing,
			Closed,
			Opening
		}
	}
}
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

namespace Funkfahrbetrieb.TrainController
{
	using SafetySharp.Modeling;

	public class TrainControl : Component
	{
		public const int CrossingPosition = 900;
		private readonly int _brakeAcceleration = 1;
		private readonly Brakes _brakes;
		private readonly int _closingTime = 10;
		private readonly int _maxCommunicationDelay = 1;
		private readonly Odometer _odometer;
		private readonly RadioModule _radio;
		private readonly int _safetyMargin = 100;

		public TrainControl(Odometer odometer, Brakes brakes, RadioModule radio)
		{
			_brakes = brakes;
			_radio = radio;
			_odometer = odometer;

			InitialState(States.Approaching);

			Transition(
				from: States.Approaching,
				to: States.WaitingForClosure,
				guard: () => _odometer.GetPosition() > ActivatePosition(),
				action: () => _radio.Send(Message.Close));

			Transition(
				from: States.WaitingForClosure,
				to: States.WaitingForResponse,
				guard: () => _odometer.GetPosition() > QueryPosition(),
				action: () => _radio.Send(Message.Query));

			Transition(
				from: States.WaitingForResponse,
				to: States.Proceeding,
				guard: () => _radio.Receive() == Message.Closed);

			Transition(
				from: States.WaitingForResponse,
				to: States.Stopping,
				guard: () => _odometer.GetPosition() >= StopPosition(),
				action: _brakes.Engage);
		}

		private int ActivatePosition()
		{
			return QueryPosition() - _odometer.GetSpeed() * (_maxCommunicationDelay + _closingTime);
		}

		private int QueryPosition()
		{
			return StopPosition() - 2 * _maxCommunicationDelay * _odometer.GetSpeed();
		}

		private int StopPosition()
		{
			return CrossingPosition - _safetyMargin - _odometer.GetSpeed() * _odometer.GetSpeed() / (2 * _brakeAcceleration);
		}

		private enum States
		{
			Approaching,
			WaitingForClosure,
			WaitingForResponse,
			Stopping,
			Proceeding,
		}
	}
}
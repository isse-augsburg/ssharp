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
		private readonly StateMachine<State> _stateMachine = new StateMachine<State>(State.Approaching);

		[Hidden]
		public Brakes Brakes;

		[Hidden]
		public int ClosingTime = 10;

		[Hidden]
		public int CrossingPosition = 900;

		[Hidden]
		public int MaxCommunicationDelay = 1;

		[Hidden]
		public Odometer Odometer;

		[Hidden]
		public RadioModule Radio;

		[Hidden]
		public int SafetyMargin = 100;

		private int ActivatePosition => QueryPosition - Odometer.Speed * (MaxCommunicationDelay + ClosingTime);
		private int QueryPosition => StopPosition - 2 * MaxCommunicationDelay * Odometer.Speed;
		private int StopPosition => CrossingPosition - SafetyMargin + Odometer.Speed * Odometer.Speed / (2 * Brakes.MaxAcceleration);

		public override void Update()
		{
			Update(Odometer, Radio, Brakes);

			_stateMachine
				.Transition(
					from: State.Approaching,
					to: State.WaitingForClosure,
					guard: Odometer.Position > ActivatePosition,
					action: () => Radio.Send(Message.Close))
				.Transition(
					from: State.WaitingForClosure,
					to: State.WaitingForResponse,
					guard: Odometer.Position > QueryPosition,
					action: () => Radio.Send(Message.Query))
				.Transition(
					from: State.WaitingForResponse,
					to: State.Proceeding,
					guard: Radio.Receive() == Message.Closed)
				.Transition(
					from: State.WaitingForResponse,
					to: State.Stopping,
					guard: Odometer.Position >= StopPosition,
					action: Brakes.Engage);
		}

		private enum State
		{
			Approaching,
			WaitingForClosure,
			WaitingForResponse,
			Stopping,
			Proceeding
		}
	}
}
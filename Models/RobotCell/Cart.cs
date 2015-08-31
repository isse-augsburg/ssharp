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
	using SafetySharp.Modeling;
	using SafetySharp.Modeling.Faults;

	public class Cart : Component
	{
		private Position _destination;
		private Position _pointOfOrigin;

		public Cart()
		{
			InitialState(States.AwaitingReconfiguration);

			Transition(
				from: States.AwaitingReconfiguration,
				to: States.AwaitWorkpiece,
				guard: _destination != Position.Unknown && _pointOfOrigin != Position.Unknown,
				action: () => MoveTo(_pointOfOrigin));

			Transition(
				from: States.AwaitWorkpiece,
				to: States.AwaitCompletion,
				guard: true,
				action: () => MoveTo(_destination));

			Transition(
				from: States.AwaitCompletion,
				to: States.AwaitWorkpiece,
				guard: WorkpieceProcessed(_destination),
				action: () => MoveTo(_pointOfOrigin));

			Transition(
				from: States.AwaitCompletion | States.AwaitWorkpiece,
				to: States.AwaitingReconfiguration,
				guard: _destination == Position.Unknown || _pointOfOrigin == Position.Unknown);
		}

		public void Reconfigure(Position pointOfOrigin, Position destination)
		{
			_pointOfOrigin = pointOfOrigin;
			_destination = destination;
		}

		public bool RequiresReconfiguration()
		{
			return State == States.AwaitingReconfiguration;
		}

		private void Move(Position position)
		{
			// TODO: Remove - this is a temporary work-around as the SCM does not yet support faults injected into required ports
			MoveTo(position);
		}

		public extern void MoveTo(Position position);
		public extern bool WorkpieceProcessed(Position position);

		[Persistent]
		public class MovementFailure : Fault
		{
			public void Move(Position position)
			{
			}
		}

		private enum States
		{
			AwaitingReconfiguration,
			AwaitWorkpiece,
			AwaitCompletion
		}
	}
}
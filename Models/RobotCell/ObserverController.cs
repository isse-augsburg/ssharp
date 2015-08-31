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

	public class ObserverController : Component
	{
		private readonly Cart _cart1;
		private readonly Cart _cart2;
		private readonly Robot _robot1;
		private readonly Robot _robot2;
		private readonly Robot _robot3;

		public ObserverController(Robot robot1, Robot robot2, Robot robot3, Cart cart1, Cart cart2)
		{
			_robot1 = robot1;
			_robot2 = robot2;
			_robot3 = robot3;
			_cart1 = cart1;
			_cart2 = cart2;

			Bind(_cart1.RequiredPorts.WorkpieceProcessed = ProvidedPorts.WorkpieceProcessed);
			Bind(_cart2.RequiredPorts.WorkpieceProcessed = ProvidedPorts.WorkpieceProcessed);

			InitialState(States.RequiresReconfiguration);

			Transition(
				from: States.ValidConfiguration,
				to: States.RequiresReconfiguration,
				guard: _robot1.RequiresReconfiguration() ||
					   _robot2.RequiresReconfiguration() ||
					   _robot3.RequiresReconfiguration() ||
					   _cart1.RequiresReconfiguration() ||
					   _cart2.RequiresReconfiguration(),
				action: () =>
				{
					_robot1.Reconfigure(RobotTask.None);
					_robot2.Reconfigure(RobotTask.None);
					_robot3.Reconfigure(RobotTask.None);

					_cart1.Reconfigure(Position.Unknown, Position.Unknown);
					_cart2.Reconfigure(Position.Unknown, Position.Unknown);
				});

			Transition(
				from: States.RequiresReconfiguration,
				to: States.ValidConfiguration,
				action: () =>
				{
					_robot1.Reconfigure(Choose.Value(RobotTask.Drill, RobotTask.Insert, RobotTask.Tighten));
					_robot2.Reconfigure(Choose.Value(RobotTask.Drill, RobotTask.Insert, RobotTask.Tighten));
					_robot3.Reconfigure(Choose.Value(RobotTask.Drill, RobotTask.Insert, RobotTask.Tighten));

					_cart1.Reconfigure(Choose.Value(Position.Robot + 0, Position.Robot + 1), Choose.Value(Position.Robot + 1, Position.Robot + 2));
					_cart2.Reconfigure(Choose.Value(Position.Robot + 0, Position.Robot + 1), Choose.Value(Position.Robot + 1, Position.Robot + 2));
				});
		}

		private bool WorkpieceProcessed(Position position)
		{
			if (_robot1.GetPosition() == position)
				return _robot1.WorkpieceProcessed();

			if (_robot2.GetPosition() == position)
				return _robot1.WorkpieceProcessed();

			if (_robot3.GetPosition() == position)
				return _robot1.WorkpieceProcessed();

			return false;
		}

		private enum States
		{
			RequiresReconfiguration,
			ValidConfiguration
		}
	}
}
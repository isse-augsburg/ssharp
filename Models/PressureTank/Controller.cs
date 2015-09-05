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
	using SafetySharp.Modeling;

	/// <summary>
	///   The software controller that enables and disables the pump.
	/// </summary>
	public class Controller : Component
	{
		/// <summary>
		///   Describes the state of the controller.
		/// </summary>
		public enum State
		{
			/// <summary>
			///   Indicates that the controller is inactive.
			/// </summary>
			Inactive,

			/// <summary>
			///   Indicates that the tank is currently being filled.
			/// </summary>
			Filling,

			/// <summary>
			///   Indicates that the last fill cycle was stopped because of the pressure sensor.
			/// </summary>
			StoppedBySensor,

			/// <summary>
			///   Indicates that the last fill cycle was stopped because of a timeout.
			/// </summary>
			StoppedByTimer
		}

		/// <summary>
		///   The pump that is used to fill the tank.
		/// </summary>
		private readonly Pump _pump;

		/// <summary>
		///   The sensor that is used to sense the pressure level within the tank.
		/// </summary>
		private readonly Sensor _sensor;

		/// <summary>
		///   The timer that is used to determine whether the pump should be disabled to prevent tank ruptures.
		/// </summary>
		private readonly Timer _timer;

		/// <summary>
		///   Gets the state machine that manages the state of the controller.
		/// </summary>
		public readonly StateMachine<State> StateMachine = new StateMachine<State>(State.Inactive);

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="sensor">The sensor that is used to sense the pressure level within the tank.</param>
		/// <param name="pump">The pump that is used to fill the tank.</param>
		/// <param name="timer">The timer that is used to determine whether the pump should be disabled.</param>
		public Controller(Sensor sensor, Pump pump, Timer timer)
		{
			_pump = pump;
			_sensor = sensor;
			_timer = timer;
		}

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public override void Update()
		{
			StateMachine
				.Transition(
					from: State.Filling,
					to: State.StoppedByTimer,
					guard: _timer.HasElapsed(),
					action: _pump.Disable)
				.Transition(
					from: State.Filling,
					to: State.StoppedBySensor,
					guard: _sensor.IsFull,
					action: () =>
					{
						_pump.Disable();
						_timer.Stop();
					})
				.Transition(
					from: new[] { State.StoppedByTimer, State.StoppedBySensor, State.Inactive },
					to: State.Filling,
					guard: _sensor.IsEmpty,
					action: () =>
					{
						_timer.Start();
						_pump.Enable();
					});
		}
	}
}
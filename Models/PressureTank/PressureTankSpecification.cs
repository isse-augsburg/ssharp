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
	///   Represents the specification of the pressure tank case study.
	/// </summary>
	public class PressureTankSpecification
	{
		/// <summary>
		///   The maximum allowed pressure level within the tank.
		/// </summary>
		public const int MaxPressure = 60;

		/// <summary>
		///   The pressure level that triggers the sensor.
		/// </summary>
		public const int SensorPressure = 58;

		/// <summary>
		///   The controller's timeout in seconds.
		/// </summary>
		public const int Timeout = 59;

		public Model Model { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public PressureTankSpecification()
		{
			Controller = new Controller(Sensor, Pump, Timer);

			Model = new Model { RootComponents = { Tank, Controller } };
//
//			AddRootComponents(Tank, Controller);
//
//			Bind(Sensor.RequiredPorts.CheckPhysicalPressure = Tank.ProvidedPorts.PressureLevel);
//			Bind(Tank.RequiredPorts.IsBeingFilled = Pump.ProvidedPorts.IsEnabled);
		}

		/// <summary>
		///   Gets the sensor that is used to determine the pressure level within the tank.
		/// </summary>
		public Sensor Sensor { get; } = new Sensor { TriggerPressure = SensorPressure };

		/// <summary>
		///   Gets the pump that fills the tank.
		/// </summary>
		public Pump Pump { get; } = new Pump();

		/// <summary>
		///   Gets the tank that is being filled.
		/// </summary>
		public Tank Tank { get; } = new Tank(MaxPressure);

		/// <summary>
		///   The timer that is used to determine whether the pump should be disabled.
		/// </summary>
		public Timer Timer { get; } = new Timer(Timeout);

		/// <summary>
		///   Gets the controller that stops filling the tank when it is full.
		/// </summary>
		public Controller Controller { get; }
	}
}
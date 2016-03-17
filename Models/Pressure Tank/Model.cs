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

namespace SafetySharp.CaseStudies.PressureTank
{
	using Analysis;
	using ModelElements;
	using Modeling;

	/// <summary>
	///   The model representing the pressure tank case study.
	/// </summary>
	public class Model : ModelBase
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

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Model()
		{
			Controller = new Controller
			{
				Sensor = new PressureSensor(),
				Pump = new Pump(),
				Timer = new Timer()
			};

			Bind(nameof(Sensor.PhysicalPressure), nameof(Tank.PressureLevel));
			Bind(nameof(Tank.IsBeingFilled), nameof(Pump.IsEnabled));
		}

		[Root(Role.SystemContext)]
		public Tank Tank { get; } = new Tank();

		[Root(Role.SystemOfInterest)]
		public Controller Controller { get; }

		public PressureSensor Sensor => Controller.Sensor;
		public Pump Pump => Controller.Pump;
		public Timer Timer => Controller.Timer;
	}
}
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Modeling
{
	using SafetySharp.Modeling;
	/// <summary>
	///   Represents the sensor that monitors the pressure within the pressure tank.
	/// </summary>
	public class PressureSensor : Component
	{
		public PressureSensor()
		{
			ContactIsClosed = true;
			// Whether the Main Circuit is powered depends on the value of IsClosed set in the last step
			MainCircuit = new CurrentInToOut(() => ContactIsClosed);
		}

		public bool ContactIsClosed;

		public readonly CurrentInToOut MainCircuit;

		/// <summary>
		///   The fault that prevents the sensor from triggering when the tank has become empty.
		/// </summary>
		public readonly Fault SuppressIsFull = new TransientFault();

		/// <summary>
		///   Gets a value indicating whether the triggering pressure level has been reached or exceeded.
		/// </summary>
		protected virtual bool IsFull => PhysicalPressure >= Model.SensorFullPressure;
		
		/// <summary>
		///   Senses the physical pressure level within the tank.
		/// </summary>
		public extern int PhysicalPressure { get; }


		/// <summary>
		///   Updates the sensors's state.
		/// </summary>
		public override void Update()
		{
			// The value of IsClosed in the next step, is determined by the pressure in the current step
			if (IsFull)
				ContactIsClosed = false;
			else
				ContactIsClosed = true;
		}

		/// <summary>
		///   Prevents the sensor from triggering when the tank has reached or exceeded its maximum allowed pressure level.
		/// </summary>
		[FaultEffect(Fault = nameof(SuppressIsFull))]
		public class SuppressIsFullEffect : PressureSensor
		{
			protected override bool IsFull => false;
		}
	}
}

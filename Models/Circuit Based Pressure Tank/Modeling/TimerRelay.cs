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

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Modeling
{
	using SafetySharp.Modeling;
	public class TimerRelay : Component
	{
		/// <summary>
		///   The fault that leads a relay to get stuck
		/// </summary>
		public readonly Fault StuckFault = new PermanentFault();

		// Control Circuit
		public readonly CurrentInToOut ControlCircuit;

		// Load Circuit
		public readonly CurrentInToOut LoadCircuit;
		
		private readonly bool _openOnTimeout;

		public bool ContactIsClosed;

		public int RemainingTime()
		{
			return _remainingTime;
		}

		/// <summary>
		///   The remaining time before the relay opens.
		/// </summary>
		[Range(0, Model.Timeout, OverflowBehavior.Clamp)]
		private int _remainingTime = Model.Timeout;
		
		/// <summary>
		///   Updates the timer's state.
		/// </summary>
		public override void Update()
		{
			if (ControlCircuit.IsPowered())
				--_remainingTime;
			else
				_remainingTime = Model.Timeout;

			// The value of IsClosed in the next step, is determined by the remainingTime in the current step
			var timedOut = _remainingTime <= 0;
			if (_openOnTimeout)
			{
				if (timedOut)
					ContactIsClosed = false;
				else
					ContactIsClosed = true;
			}
			else
			{
				if (timedOut)
					ContactIsClosed = true;
				else
					ContactIsClosed = false;
			}
		}

		public TimerRelay(bool openOnTimeout)
		{
			_openOnTimeout = openOnTimeout;
			ContactIsClosed = _openOnTimeout; //if coil opens on timeout then the contact is initially closed
			// Control Circuit keeps standard implementation (forwarding)
			ControlCircuit = new CurrentInToOut();
			// Whether the Load Circuit is powered depends on the value of IsClosed set in the last step
			LoadCircuit = new CurrentInToOut(() => ContactIsClosed);
		}


		/// <summary>
		///   Prevents the timer from reporting a timeout.
		/// </summary>
		[FaultEffect(Fault = nameof(StuckFault))]
		public class StuckFaultEffect : TimerRelay
		{
			public StuckFaultEffect(bool openOnTimeout)
				: base(openOnTimeout)
			{
				
			}

			public override void Update()
			{
			}
		}
	}
}

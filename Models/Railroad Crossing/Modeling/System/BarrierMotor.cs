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

namespace SafetySharp.CaseStudies.RailroadCrossing.Modeling.System
{
	using SafetySharp.Modeling;

	public class BarrierMotor : Component
	{
		public readonly Fault BarrierMotorStuck = new TransientFault();

		[Range(-1, 1, OverflowBehavior.Clamp)]
		private int _currentSpeed;

		public virtual int Speed => _currentSpeed;

		public void Open()
		{
			_currentSpeed = 1;
		}

		public void Close()
		{
			_currentSpeed = -1;
		}

		public void Stop()
		{
			_currentSpeed = 0;
		}

		[FaultEffect(Fault = nameof(BarrierMotorStuck))]
		public class StuckEffect : BarrierMotor
		{
			public override int Speed => 0;
		}
	}
}
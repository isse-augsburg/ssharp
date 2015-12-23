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

	public class Odometer : Component
	{
		[Hidden]
		public int MaxPositionOffset;

		[Hidden]
		public int MaxSpeedOffset;

		public readonly Fault OdometerPositionOffset = new TransientFault();
		public readonly Fault OdometerSpeedOffset = new TransientFault();

		public virtual int Position => TrainPosition;
		public virtual int Speed => TrainSpeed;

		public extern int TrainPosition { get; }
		public extern int TrainSpeed { get; }

		[FaultEffect(Fault = nameof(OdometerPositionOffset))]
		public class PositionOffsetEffect : Odometer
		{
			public override int Position => base.Position + MaxPositionOffset * Choose(-1, 1);
		}

		[FaultEffect(Fault = nameof(OdometerSpeedOffset))]
		public class SpeedOffsetEffect : Odometer
		{
			public override int Speed => base.Speed + MaxSpeedOffset * Choose(-1, 1);
		}
	}
}
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
		//		private readonly int _maxPositionOffset;
		//		private readonly int _maxSpeedOffset;
		//
		//		public Odometer(int maxPositionOffset, int maxSpeedOffset)
		//		{
		//			_maxPositionOffset = maxPositionOffset;
		//			_maxSpeedOffset = maxSpeedOffset;
		//		}

		public int GetPosition() => TrainPosition();
		public int GetSpeed() => TrainSpeed();

		public extern int TrainPosition();
		public extern int TrainSpeed();

		// TODO: Fault effects referring back to the component are not supported at the moment by the model checker transformations
		//		[Transient]
		//		public class PositionOffset : Fault<Odometer>
		//		{
		//			public decimal Position
		//			{
		//				get { return Component.Position + ChooseFromRange(-Component.maxPositionOffset, Component.maxPositionOffset); }
		//			}
		//		}
		//
		//		[Transient]
		//		public class SpeedOffset : Fault<Odometer>
		//		{
		//			public decimal Speed
		//			{
		//				get { return Component.Speed + ChooseFromRange(-Component.maxSpeedOffset, Component.maxSpeedOffset); }
		//			}
		//		}
	}
}
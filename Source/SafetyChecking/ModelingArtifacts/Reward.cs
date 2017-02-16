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

namespace SafetySharp.Modeling
{
	public struct Reward
	{
		public Reward(bool mightBeNegative = false)
		{
			_valueNegative = 0;
			_value = 0;
			MightBeNegative = mightBeNegative;
		}
		
		public readonly bool MightBeNegative;
		
		//TODO: Change to [AutoReset] (NotImplementedYet) which we may introduce for value types. Values are reseted after each time step. Makes behavior more deterministic compared to [NonSerializable] when a value is read before written even if it is permitted.
		private int _value;
		
		//TODO: Change to [AutoReset] (NotImplementedYet) which we may introduce for value types. Values are reseted after each time step. Makes behavior more deterministic compared to [NonSerializable] when a value is read before written even if it is permitted.
		private int _valueNegative;

		public int Value()
		{
			return _value;
		}

		public int ValueNegative()
		{
			return _valueNegative;
		}


		public void Negative(int value)
		{
			if (MightBeNegative)
				_valueNegative += value;
			else
				_value -= value;
		}

		public void Positive(int value)
		{
			_value += value;
		}

		internal void Reset()
		{

		}
	}

	public struct RewardResult
	{
		public double Value;

		public bool Is(double expected, double tolerance)
		{
			var minimum = expected - tolerance;
			var maximum = expected + tolerance;
			return (Value >= minimum && Value <= maximum);
		}
	}
}


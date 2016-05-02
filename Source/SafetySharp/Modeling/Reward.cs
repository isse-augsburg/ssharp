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
		public Reward(bool mightBeNegative)
		{
			_valueNegative = 0;
			_valuePositive = 0;
			MightBeNegative = mightBeNegative;
		}

		[Hidden]
		public readonly bool MightBeNegative;

		[NonSerializable] //TODO: Change to [AutoReset] (NotImplementedYet) which we may introduce for value types. Values are reseted after each time step. Makes behavior more deterministic compared to [NonSerializable] when a value is read before written even if it is permitted.
		private int _valuePositive;

		[NonSerializable] //TODO: Change to [AutoReset] (NotImplementedYet) which we may introduce for value types. Values are reseted after each time step. Makes behavior more deterministic compared to [NonSerializable] when a value is read before written even if it is permitted.
		private int _valueNegative;

		public int ValuePositive()
		{
			return _valuePositive;
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
				_valuePositive -= value;
		}

		public void Positive(int value)
		{
			_valuePositive += value;
		}

		internal void Reset()
		{

		}
	}
}

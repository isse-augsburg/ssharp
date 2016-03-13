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

namespace Tests.Normalization.LineCounts
{
	using SafetySharp.Modeling;

	public class ProvidedPorts : LineCountTestObject
	{
		private class C : Component
		{
			public int P0 => 1;
			public int P1 { get; } = 1;
			public int P2 { get; set; } = 9;

			public int P3
			{
				set
				{
					var i = 0;
					++i;
				}
			}

			public int P4
			{
				get { return 1; }
			}

			public int P5
			{
				get { return 1; }
				set
				{
					var i = 0;
					++i;
				}
			}

			public int M1() => 1;

			public int M2()
			{
				return 2;
			}

			public virtual void M3(int f)
			{
			}

			public virtual int M4(int f, out int r)
			{
				r = 0;
				return 0;
			}

			public class Z
			{
			}
		}

		protected override void CheckLines()
		{
			CheckProperty("P0", expectedLine: 31, occurrence: 0);
			CheckProperty("P1", expectedLine: 32, occurrence: 0);
			CheckProperty("P2", expectedLine: 33, occurrence: 0);
			CheckProperty("P3", expectedLine: 35, occurrence: 0);
			CheckProperty("P4", expectedLine: 44, occurrence: 0);
			CheckProperty("P5", expectedLine: 49, occurrence: 0);

			CheckGetter("P1", expectedLine: 32, occurrence: 0);
			CheckGetter("P2", expectedLine: 33, occurrence: 0);
			CheckSetter("P2", expectedLine: 33, occurrence: 0);
			CheckSetter("P3", expectedLine: 37, occurrence: 0);
			CheckGetter("P4", expectedLine: 46, occurrence: 0);
			CheckGetter("P5", expectedLine: 51, occurrence: 0);
			CheckSetter("P5", expectedLine: 52, occurrence: 0);

			CheckMethod("M1", expectedLine: 59, occurrence: 0);
			CheckMethod("M2", expectedLine: 61, occurrence: 0);
			CheckMethod("M3", expectedLine: 66, occurrence: 0);
			CheckMethod("M4", expectedLine: 70, occurrence: 0);

			CheckClass("Z", expectedLine: 76, occurrence: 0);
			CheckClass("ProvidedPorts", expectedLine: 27, occurrence: 0);
			CheckClass("C", expectedLine: 29, occurrence: 0);
		}
	}
}
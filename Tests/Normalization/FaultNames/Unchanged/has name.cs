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

namespace Tests.Normalization.FaultNames.Unchanged
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	internal class F : Fault
	{
		public int X;
		public int Y;

		protected override Activation CheckActivation()
		{
			return Activation.Suppressed;
		}

		public F()
			: base(false)
		{
		}
	}

	public class In4
	{
		public Fault X1 = new F() { Name = "x" };
		public Fault X2 = new F() { X = 1, Name = "x" };
		public Fault X3 = new F() { Name = "x", X = 2 };
		public Fault X4 = new F() { X = 2, Name = "x", Y = 3 };

		public Fault Y1 { get; } = new F() { Name = "x" };
		public Fault Y2 { get; } = new F() { X = 1, Name = "x" };
		public Fault Y3 { get; } = new F() { Name = "x", X = 2 };
		public Fault Y4 { get; } = new F() { X = 2, Name = "x", Y = 3 };

		private void M()
		{
			var x1 = new F() { Name = "x" };
			var x2 = new F() { X = 1, Name = "x" };
			var x3 = new F() { Name = "x", X = 2 };
			var x4 = new F() { X = 2, Name = "x", Y = 3 };
		}
	}
}
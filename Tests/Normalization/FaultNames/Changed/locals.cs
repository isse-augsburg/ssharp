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

namespace Tests.Normalization.FaultNames.Changed
{
	using SafetySharp.Modeling;

	public class In1
	{
		private void M()
		{
			var f1 = new TransientFault();
			Fault f2 = new TransientFault();

			TransientFault f3 = new TransientFault(), f4 = new TransientFault(), 
				f5 = new TransientFault();

			var f6 = new TransientFault { Activation = Activation.Forced };
		}
	}

	public class Out1
	{
		private void M()
		{
			var f1 = new TransientFault() { Name = "f1" };
			Fault f2 = new TransientFault() { Name = "f2" };

			TransientFault f3 = new TransientFault() { Name = "f3" }, f4 = new TransientFault() { Name = "f4" }, 
				f5 = new TransientFault() { Name = "f5" };

			var f6 = new TransientFault { Activation = Activation.Forced, Name = "f6" };
		}
	}
}
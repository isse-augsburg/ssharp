// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	public class In2
	{
		private Fault f1 = new TransientFault() { Activation = Activation.Forced };
		private Fault f2 = new TransientFault();
		private TransientFault f3 = new TransientFault();
		private TransientFault f4 = new TransientFault();
		private TransientFault f5 = new TransientFault();
		private Fault f6;

		public In2()
		{
			f6 = new TransientFault();
		}
	}

	public class Out2
	{
		private Fault f1 = new TransientFault() { Activation = Activation.Forced, Name = "f1" };
		private Fault f2 = new TransientFault() { Name = "f2" };
		private TransientFault f3 = new TransientFault() { Name = "f3" };
		private TransientFault f4 = new TransientFault() { Name = "f4" };
		private TransientFault f5 = new TransientFault() { Name = "f5" };
		private Fault f6;

		public Out2()
		{
			f6 = new TransientFault() { Name = "f6" };
		}
	}
}
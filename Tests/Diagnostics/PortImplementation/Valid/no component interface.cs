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

namespace Tests.Diagnostics.PortImplementation.Valid
{
	using SafetySharp.Modeling;

	public class X4
	{
		private interface X1
		{
			[Provided]
			void T();
		}

		private class C1 : X1
		{
			[Required]
			public void T()
			{
			}
		}

		private interface X2
		{
			[Provided]
			void T();
		}

		private class C2 : X2
		{
			[Required]
			void X2.T()
			{
			}
		}

		private interface X3
		{
			[Provided]
			void T();
		}

		private class C3 : Component, X3
		{
			[Required]
			public void T()
			{
			}
		}

		private interface X4b
		{
			[Provided]
			void T();
		}

		private class C4 : Component, X4b
		{
			[Required]
			void X4b.T()
			{
			}
		}

		private interface X5
		{
			[Provided]
			int T { get; set; }
		}

		private class C5 : X5
		{
			[Required]
			public int T { get; set; }
		}

		private interface X6
		{
			[Provided]
			int T { get; set; }
		}

		private class C6 : X6
		{
			[Required]
			int X6.T { get; set; }
		}

		private interface X7
		{
			[Provided]
			int T { get; set; }
		}

		private class C7 : Component, X7
		{
			[Required]
			public int T { get; set; }
		}

		private interface X8
		{
			[Provided]
			int T { get; set; }
		}

		private class C8 : Component, X8
		{
			[Required]
			int X8.T { get; set; }
		}
	}
}
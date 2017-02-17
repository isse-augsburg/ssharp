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

namespace Tests.Diagnostics.PortImplementation.Valid
{
	using SafetySharp.Modeling;

	public class X1
	{
		private interface I : IComponent
		{
			[Required]
			void In();

			[Provided]
			void Out();
		}

		private interface J : IComponent
		{
			[Required]
			int In { get; set; }

			[Provided]
			int Out { get; set; }
		}

		private class C1 : Component, I
		{
			public extern void In();

			public void Out()
			{
			}
		}

		private class C2 : Component, I
		{
			extern void I.In();

			void I.Out()
			{
			}
		}

		private class C3 : Component, J
		{
			public extern int In { get; set; }
			public int Out { get; set; }
		}

		private class C4 : Component, J
		{
			extern int J.In { get; set; }
			int J.Out { get; set; }
		}
	}
}
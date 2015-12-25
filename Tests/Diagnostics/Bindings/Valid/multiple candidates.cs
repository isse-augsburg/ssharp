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

namespace Tests.Diagnostics.Bindings.Valid
{
	using SafetySharp.Modeling;

	internal class X2 : Component
	{
		private X2()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private extern void N();
		private extern void N(int i);
	}

	internal class X3 : Component
	{
		private X3()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(int i)
		{
		}

		private extern void N();
	}

	internal class X4 : Component
	{
		private X4()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M(int i)
		{
		}

		private extern void N();
		private extern void N(int i);
	}

	internal class X5 : Component
	{
		private X5()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(int i)
		{
		}

		private extern void N(int i);
	}

	internal class X6 : Component
	{
		private X6()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(bool b)
		{
		}

		private extern void N();
		private extern void N(int i);
	}

	internal class X7 : Component
	{
		private X7()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(int i)
		{
		}

		private extern void N();
		private extern void N(bool b);
	}

	internal class X8 : Component
	{
		private X8()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(ref bool b)
		{
		}

		private extern void N(ref bool b);
		private extern void N(int i);
	}

	internal class X9 : Component
	{
		private X9()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M(ref int i)
		{
		}

		private void M(int i)
		{
		}

		private extern void N();
		private extern void N(ref int i);
	}
}
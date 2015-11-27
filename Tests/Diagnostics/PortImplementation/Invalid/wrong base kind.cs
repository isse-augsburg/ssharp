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

namespace Tests.Diagnostics.PortImplementation.Invalid
{
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Modeling;

	public class X2b
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

		private class C1 : Component
		{
			public extern void In();
			public extern void Out();
		}

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 56, 23, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.C1.Out()", "Tests.Diagnostics.PortImplementation.Invalid.X2b.I.Out()")]
		private class X1 : C1, I
		{
		}

		private class C2 : Component
		{
			[Provided]
			public void In()
			{
			}

			[Provided]
			public void Out()
			{
			}
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 75, 23, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.C2.In()", "Tests.Diagnostics.PortImplementation.Invalid.X2b.I.In()")]
		private class X2 : C2, I
		{
		}

		private class C3 : Component
		{
			public extern int In { get; set; }

			[Required]
			public extern int Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 90, 23, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.C3.Out", 
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.J.Out")]
		private class X3 : C3, J
		{
		}

		private class C4 : Component
		{
			[Provided]
			public int In { get; set; }

			[Provided]
			public int Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 106, 23, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.C4.In",
			"Tests.Diagnostics.PortImplementation.Invalid.X2b.J.In")]
		private class X4 : C4, J
		{
		}
	}
}
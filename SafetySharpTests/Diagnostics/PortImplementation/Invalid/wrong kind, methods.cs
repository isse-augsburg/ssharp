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

namespace Tests.Diagnostics.PortImplementation.Invalid
{
	using SafetySharp.Compiler.Analyzers;
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

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 44, 32, 3,
			"Tests.Diagnostics.PortImplementation.Invalid.X1.C1.Out()", "Tests.Diagnostics.PortImplementation.Invalid.X1.I.Out()")]
		private class C1 : Component, I
		{
			public extern void In();
			public extern void Out();
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 52, 25, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X1.C2.In()", "Tests.Diagnostics.PortImplementation.Invalid.X1.I.In()")]
		private class C2 : Component, I
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

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 68, 27, 3,
			"Tests.Diagnostics.PortImplementation.Invalid.X1.C3.Tests.Diagnostics.PortImplementation.Invalid.X1.I.Out()",
			"Tests.Diagnostics.PortImplementation.Invalid.X1.I.Out()")]
		private class C3 : Component, I
		{
			extern void I.In();
			extern void I.Out();
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 77, 20, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X1.C4.Tests.Diagnostics.PortImplementation.Invalid.X1.I.In()",
			"Tests.Diagnostics.PortImplementation.Invalid.X1.I.In()")]
		private class C4 : Component, I
		{
			[Provided]
			void I.In()
			{
			}

			[Provided]
			void I.Out()
			{
			}
		}
	}
}
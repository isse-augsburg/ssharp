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

	public class X3
	{
		private interface J : IComponent
		{
			[Required]
			int In { get; set; }

			[Provided]
			int Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 46, 31, 3,
			"Tests.Diagnostics.PortImplementation.Invalid.X3.C5.Out", "Tests.Diagnostics.PortImplementation.Invalid.X3.J.Out")]
		private class C5 : Component, J
		{
			public extern int In { get; set; }

			[Required]
			public extern int Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 54, 24, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X3.C6.In", "Tests.Diagnostics.PortImplementation.Invalid.X3.J.In")]
		private class C6 : Component, J
		{
			[Provided]
			public int In { get; set; }

			[Provided]
			public int Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.ProvidedPortImplementedAsRequiredPort, 68, 26, 3,
			"Tests.Diagnostics.PortImplementation.Invalid.X3.C7.Tests.Diagnostics.PortImplementation.Invalid.X3.J.Out",
			"Tests.Diagnostics.PortImplementation.Invalid.X3.J.Out")]
		private class C7 : Component, J
		{
			extern int J.In { get; set; }

			[Required]
			extern int J.Out { get; set; }
		}

		[Diagnostic(DiagnosticIdentifier.RequiredPortImplementedAsProvidedPort, 77, 19, 2,
			"Tests.Diagnostics.PortImplementation.Invalid.X3.C8.Tests.Diagnostics.PortImplementation.Invalid.X3.J.In",
			"Tests.Diagnostics.PortImplementation.Invalid.X3.J.In")]
		private class C8 : Component, J
		{
			[Provided]
			int J.In { get; set; }

			[Provided]
			int J.Out { get; set; }
		}
	}
}
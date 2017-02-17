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

namespace Tests.Diagnostics.PortKinds.Invalid
{
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Modeling;

	[Diagnostic(DiagnosticIdentifier.StaticPort, 35, 28, 1, "Tests.Diagnostics.PortKinds.Invalid.StaticPorts.A")]
	[Diagnostic(DiagnosticIdentifier.StaticPort, 38, 35, 1, "Tests.Diagnostics.PortKinds.Invalid.StaticPorts.B")]
	[Diagnostic(DiagnosticIdentifier.StaticPort, 41, 29, 1, "Tests.Diagnostics.PortKinds.Invalid.StaticPorts.M()")]
	[Diagnostic(DiagnosticIdentifier.StaticPort, 46, 36, 1, "Tests.Diagnostics.PortKinds.Invalid.StaticPorts.N()")]
	internal class StaticPorts : Component
	{
		[Provided]
		private static int A { get; set; }

		[Required]
		private static extern int B { get; set; }

		[Provided]
		private static void M()
		{
		}

		[Required]
		private static extern void N();
	}
}
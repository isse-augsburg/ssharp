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
	using System;
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Modeling;

	[Diagnostic(DiagnosticIdentifier.EventPort, 36, 37, 1, "Tests.Diagnostics.PortKinds.Invalid.EventPorts.A")]
	[Diagnostic(DiagnosticIdentifier.EventPort, 39, 37, 1, "Tests.Diagnostics.PortKinds.Invalid.EventPorts.B")]
	[Diagnostic(DiagnosticIdentifier.EventPort, 42, 30, 1, "Tests.Diagnostics.PortKinds.Invalid.EventPorts.C")]
	[Diagnostic(DiagnosticIdentifier.EventPort, 48, 22, 1, "Tests.Diagnostics.PortKinds.Invalid.IEventPorts.A")]
	[Diagnostic(DiagnosticIdentifier.EventPort, 51, 22, 1, "Tests.Diagnostics.PortKinds.Invalid.IEventPorts.B")]
	internal abstract class EventPorts : Component
	{
		private extern event Action A;

		[Required]
		private extern event Action B;

		[Provided]
		private event Action C;
	}

	internal interface IEventPorts : IComponent
	{
		[Required]
		event Action A;

		[Provided]
		event Action B;
	}
}
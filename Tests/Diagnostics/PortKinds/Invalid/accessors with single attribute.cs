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

namespace Tests.Diagnostics.PortKinds.Invalid
{
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Modeling;

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 34, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y1.M.get")]
	internal class Y1 : Component
	{
		private int M
		{
			[Required]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 45, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y2.M.get")]
	internal class Y2 : Component
	{
		private int M
		{
			[Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 57, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y3.M.set")]
	internal class Y3 : Component
	{
		private int M
		{
			get;
			[Required]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 68, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y4.M.set")]
	internal class Y4 : Component
	{
		private int M
		{
			get;
			[Provided]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 79, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y5.M.get")]
	internal interface Y5 : IComponent
	{
		[Required]
		int M
		{
			[Required]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 91, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y6.M.get")]
	internal interface Y6 : IComponent
	{
		[Provided]
		int M
		{
			[Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 104, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y7.M.set")]
	internal interface Y7 : IComponent
	{
		[Required]
		int M
		{
			get;
			[Required]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 116, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Y8.M.set")]
	internal interface Y8 : IComponent
	{
		[Provided]
		int M
		{
			get;
			[Provided]
			set;
		}
	}
}
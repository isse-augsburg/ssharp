// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 34, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z1.M.get")]
	internal class Z1 : Component
	{
		private int M
		{
			[Required, Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 46, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z2.M.get")]
	internal class Z2 : Component
	{
		private int M
		{
			[Required]
			[Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 58, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z3.M.set")]
	internal class Z3 : Component
	{
		private int M
		{
			get;
			[Required, Provided]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 70, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z4.M.set")]
	internal class Z4 : Component
	{
		private int M
		{
			get;
			[Required]
			[Provided]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 81, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z5.M.get")]
	internal interface Z5 : IComponent
	{
		[Required]
		int M
		{
			[Required, Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 94, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z6.M.get")]
	internal interface Z6 : IComponent
	{
		[Required]
		int M
		{
			[Required]
			[Provided]
			get;
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 107, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z7.M.set")]
	internal interface Z7 : IComponent
	{
		[Required]
		int M
		{
			get;
			[Required, Provided]
			set;
		}
	}

	[Diagnostic(DiagnosticIdentifier.PortPropertyAccessor, 120, 13, 3, "Tests.Diagnostics.PortKinds.Invalid.Z8.M.set")]
	internal interface Z8 : IComponent
	{
		[Required]
		int M
		{
			get;
			[Required]
			[Provided]
			set;
		}
	}
}
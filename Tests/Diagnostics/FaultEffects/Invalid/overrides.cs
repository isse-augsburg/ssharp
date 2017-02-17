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

namespace Tests.Diagnostics.FaultEffects.Invalid
{
	using SafetySharp.Compiler.Analyzers;
	using SafetySharp.Modeling;

	[Diagnostic(DiagnosticIdentifier.AbstractFaultEffectOverride, 41, 33, 1, 
		"Tests.Diagnostics.FaultEffects.Invalid.Overrides.E.X", "Tests.Diagnostics.FaultEffects.Invalid.Overrides.X")]
	[Diagnostic(DiagnosticIdentifier.AbstractFaultEffectOverride, 43, 34, 1,
		"Tests.Diagnostics.FaultEffects.Invalid.Overrides.E.M()", "Tests.Diagnostics.FaultEffects.Invalid.Overrides.M()")]
	public abstract class Overrides : Component
	{
		public abstract int X { get; set; }

		public abstract void M();

		[FaultEffect]
		public class E : Overrides
		{
			public override int X { get; set; }

			public override void M()
			{
			}
		}
	}
}
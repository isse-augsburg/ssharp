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

	[Diagnostic(DiagnosticIdentifier.MultipleFaultEffectsWithoutPriority, 41, 33, 1,
		"Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.M.get",
		"'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E1', 'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E2', " +
		"'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E5'")]
	[Diagnostic(DiagnosticIdentifier.MultipleFaultEffectsWithoutPriority, 42, 28, 1,
		"Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.N()",
		"'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E1', 'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E2', " +
		"'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E6'")]
	[Diagnostic(DiagnosticIdentifier.MultipleFaultEffectsWithoutPriority, 39, 18, 22,
		"SafetySharp.Modeling.Component.Update()",
		"'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E7', 'Tests.Diagnostics.FaultEffects.Invalid.ImplicitNondeterminism.E8'")]
	public class ImplicitNondeterminism : Component
	{
		public virtual int M => 1;
		public virtual int N() => 2;

		[FaultEffect]
		public class E1 : ImplicitNondeterminism
		{
			public override int M => 2;
			public override int N() => 3;
		}

		[FaultEffect]
		public class E2 : ImplicitNondeterminism
		{
			public override int M => 3;
			public override int N() => 3;
		}

		[FaultEffect, Priority(0)]
		public class E3 : ImplicitNondeterminism
		{
			public override int M => 3;
			public override int N() => 3;
		}

		[FaultEffect, Priority(1)]
		public class E4 : ImplicitNondeterminism
		{
			public override int M => 3;
			public override int N() => 3;
		}

		[FaultEffect]
		public class E5 : ImplicitNondeterminism
		{
			public override int M => 3;
		}

		[FaultEffect]
		public class E6 : ImplicitNondeterminism
		{
			public override int N() => 3;
		}

		[FaultEffect]
		public class E7 : ImplicitNondeterminism
		{
			public override void Update()
			{
			}
		}

		[FaultEffect]
		public class E8 : ImplicitNondeterminism
		{
			public override void Update()
			{
			}
		}
	}
}
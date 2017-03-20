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

namespace Tests.Execution.Faults.Binding
{
	using System.Reflection;
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class NestedInheritedFault : TestModel
	{
		protected override void Check()
		{
			Create(new D.C());
			var c = (D.C)RootComponents[0];

			c.FaultEffects[0].GetFault(typeof(D.C.Effect)).ShouldBe(typeof(D).GetField("_f", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c));
		}

		private class D : Component
		{
			private readonly Fault _f = new TransientFault();

			public class C : D
			{
				[FaultEffect(Fault = nameof(_f))]
				internal class Effect : C
				{
				}
			}
		}
	}
}
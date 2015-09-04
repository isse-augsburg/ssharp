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

namespace Tests.Serialization.RuntimeModels
{
	using System.Linq;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class Faults : RuntimeModelTest
	{
		private static bool _hasConstructorRun;

		protected override void Check()
		{
			var c = new C ();
			var m = new Model(c);

			((C.Effect1)c.FaultEffects[0]).F = 17;
			((C.Effect1)c.FaultEffects[1]).F = 18;
			((C.Effect2)c.FaultEffects[2]).F = 19;

			_hasConstructorRun = false;
			Create(m);

			StateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();
			root.Subcomponents.ShouldBeEmpty();

			((C)root).F1.ShouldBeOfType<TransientFault>();
			((C)root).F2.ShouldBeOfType<PersistentFault>();

			root.FaultEffects.Count.ShouldBe(3);
			((C.Effect1)root.FaultEffects[0]).F = 17;
			((C.Effect1)root.FaultEffects[1]).F = 18;
			((C.Effect2)root.FaultEffects[2]).F = 19;

			_hasConstructorRun.ShouldBe(false);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PersistentFault();

			public C()
			{
				_hasConstructorRun = true;

				F1.AddEffect<Effect1>(this);
				F2.AddEffect<Effect1>(this);
				F2.AddEffect<Effect2>(this);
			}

			public virtual void M() { }

			[FaultEffect]
			public sealed class Effect1 : C, IFaultEffect
			{
				public int F;

				public override void M()
				{
				}

				/// <summary>
				///   Gets or sets the <see cref="Component" /> instance that is affected by the fault effect.
				/// </summary>
				public Component Component { get; set; }

				/// <summary>
				///   Gets or sets the <see cref="Fault" /> instance that determines whether the fault effect is active.
				/// </summary>
				public Fault Fault { get; set; }
			}

			[FaultEffect]
			public sealed class Effect2 : C, IFaultEffect
			{
				public int F;

				public override void M()
				{
				}/// <summary>
				 ///   Gets or sets the <see cref="Component" /> instance that is affected by the fault effect.
				 /// </summary>
				public Component Component { get; set; }

				/// <summary>
				///   Gets or sets the <see cref="Fault" /> instance that determines whether the fault effect is active.
				/// </summary>
				public Fault Fault { get; set; }
			}

			// Unused
			[FaultEffect]
			public sealed class Effect3 : C, IFaultEffect
			{
				/// <summary>
				///   Gets or sets the <see cref="Component" /> instance that is affected by the fault effect.
				/// </summary>
				public Component Component { get; set; }

				/// <summary>
				///   Gets or sets the <see cref="Fault" /> instance that determines whether the fault effect is active.
				/// </summary>
				public Fault Fault { get; set; }
			}
		}
	}
}
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

namespace Tests.Serialization.RuntimeModels
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class LinqDelegate : TestModel
	{
		private static bool _hasConstructorRun;

		protected override void Check()
		{
			var c = new C();
			var a = new[] { 1, 2, 3 };
			var m = InitializeModel(c);

			_hasConstructorRun = false;
			Create(m);

			ExecutableStateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();
			c = (C)root;

			c.AnyWithoutPredicate(a).ShouldBe(true);
			c.Any(a, x => x > 1).ShouldBe(true);
			c.Any(a, x => x > 10).ShouldBe(false);
			c.All(a, x => x > 1).ShouldBe(false);
			c.All(a, x => x < 10).ShouldBe(true);

			_hasConstructorRun.ShouldBe(false);
		}

		private class C : Component
		{
			public readonly Func<IEnumerable<int>, Func<int, bool>, bool> All;
			public readonly Func<IEnumerable<int>, Func<int, bool>, bool> Any;
			public readonly Func<IEnumerable<int>, bool> AnyWithoutPredicate;

			public C()
			{
				_hasConstructorRun = true;

				All = Enumerable.All;
				Any = Enumerable.Any;
				AnyWithoutPredicate = Enumerable.Any;
			}
		}
	}
}
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

namespace Tests.Normalization.LiftedExpressions.Lifted
{
	using System;
	using System.Linq.Expressions;
	using SafetySharp.CompilerServices;

	public class Test3
	{
		protected void N([LiftExpression] int i, [LiftExpression] bool j, [LiftExpression] int k)
		{
		}

		protected void N(Expression<Func<int>> i, Expression<Func<bool>> j, Expression<Func<int>> k)
		{
		}

		protected void P(int i, [LiftExpression] bool j, int k)
		{
		}

		protected void P(int i, Expression<Func<bool>> j, int k)
		{
		}
	}

	public class In3 : Test3
	{
		private void Q(int x)
		{
			N(1, true, 4);
			N(1 + x / 54 + (true == false ? 17 : 33 + 1), 3 > 5 ? true : false, 33 + 11 / x);

			P(1, true, 17);
			P(1, true || false, 33 << 2);
			P(1 - 0, false, 22 / 2);
		}
	}

	public class Out3 : Test3
	{
		private void Q(int x)
		{
			N(() => 1, () => true, () => 4);
			N(() => 1 + x / 54 + (true == false ? 17 : 33 + 1), () => 3 > 5 ? true : false, () => 33 + 11 / x);

			P(1, () => true, 17);
			P(1, () => true || false, 33 << 2);
			P(1 - 0, () => false, 22 / 2);
		}
	}
}
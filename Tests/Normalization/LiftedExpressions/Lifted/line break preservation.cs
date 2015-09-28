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

	public class Test5
	{
		public Test5()
		{
		}

		public Test5([LiftExpression] int i)
		{
		}

		public Test5(Expression<Func<int>> i)
		{
		}

		protected int M(int i)
		{
			return 0;
		}

		protected int N([LiftExpression] int i)
		{
			return 0;
		}

		protected int N(Expression<Func<int>> i)
		{
			return 0;
		}

		protected int O([LiftExpression] int i, [LiftExpression] int j)
		{
			return 0;
		}

		protected int O(Expression<Func<int>> i, Expression<Func<int>> j)
		{
			return 0;
		}
	}

	[CheckTrivia(TriviaType.All)]
	public class In5 : Test5
	{
		private void M()
		{
			new Test5(1
					  + 1);

			O(M(2 -
				1)
			  + 0,
				3
				- N(
					2 *
					5));
		}
	}

	[CheckTrivia(TriviaType.All)]
	public class Out5 : Test5
	{
		private void M()
        {
            new Test5(()=>1
                      + 1);

            O(()=>M(2 -
                1)
              + 0,
                ()=>                3
                - N(
                    ()=>                    2 *
                    5));
        }
	}
}
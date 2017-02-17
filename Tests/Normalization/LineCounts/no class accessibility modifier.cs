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

// ReSharper disable ArrangeTypeMemberModifiers
namespace Tests.Normalization.LineCounts
{
	using SafetySharp.Modeling;

	class NoClassAccessibilityModifier : LineCountTestObject
	{
		class C : Component
		{
			public int P0 => 1;

			class Z
			{
			}
		}

		protected override void CheckLines()
		{
			CheckProperty("P0", expectedLine: 32, occurrence: 0);
			CheckMethod("CheckLines", expectedLine: 39, occurrence: 0);

			CheckClass("Z", expectedLine: 34, occurrence: 0);
			CheckClass("NoClassAccessibilityModifier", expectedLine: 28, occurrence: 0);
			CheckClass("C", expectedLine: 30, occurrence: 0);
		}
	}
}
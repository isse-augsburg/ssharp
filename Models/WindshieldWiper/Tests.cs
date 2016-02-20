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

namespace Wiper
{
	using System;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using WiperModel = Model;
	using SafetySharpModel = SafetySharp.Analysis.Model;

	[TestFixture]
	public class Tests
	{
		public static void Main()
		{
			new Tests().Test();
		}

		[Test]
		public void CollisionDcca()
		{
			var specification = new Specification(null);
			var analysis = new SafetyAnalysis(SafetySharpModel.Create(specification));

			var result = analysis.ComputeMinimalCutSets(specification.InvalidScenario);
			result.SaveCounterExamples("counter examples/wiper/");

			Console.WriteLine(result);
		}

		[Test]
		public void Test()
		{
			var specification = new Specification(null);
			var model = SafetySharpModel.Create(specification);

			var checker = new SSharpChecker() { CpuCount = 4 };
			checker.CheckInvariant(model, true);
		}
	}
}
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

namespace SafetySharp.CaseStudies.RobotCell.Modeling
{
	using System;
	using Odp;
	using Controllers;

	public static class ModelBuilderHelper
	{
		public static ICapability Produce => new ProduceCapability();
		public static ICapability Insert => new ProcessCapability(ProductionAction.Insert);
		public static ICapability Drill => new ProcessCapability(ProductionAction.Drill);
		public static ICapability Tighten => new ProcessCapability(ProductionAction.Tighten);
		public static ICapability Polish => new ProcessCapability(ProductionAction.Polish);
		public static ICapability Consume => new ConsumeCapability();

		public static Tuple<int, int> Route(int from, int to) => Tuple.Create(from, to);
	}
}

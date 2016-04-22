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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;
	using Utilities.BidirectionalFlow;

	public enum SuctionType
	{
		SourceDependentSuction,
		CustomSuction
	}

	public class Suction : IFlowElement<Suction>
	{
		[Hidden]
		public SuctionType SuctionType = SuctionType.SourceDependentSuction;

		[Hidden,Range(0, 8, OverflowBehavior.Error)]
		public int CustomSuctionValue = 0;

		public void CopyValuesFrom(Suction from)
		{
			SuctionType = from.SuctionType;
			CustomSuctionValue = from.CustomSuctionValue;
		}

		public void PrintSuctionValues(string description)
		{
			System.Console.Out.WriteLine("\t" + description);
			System.Console.Out.WriteLine("\t\tSuction Type: " + SuctionType.ToString());
			System.Console.Out.WriteLine("\t\tCustomSuctionValue: " + CustomSuctionValue);
		}
	}
}

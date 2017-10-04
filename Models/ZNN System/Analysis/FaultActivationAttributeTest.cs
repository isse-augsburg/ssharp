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

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
	[TestFixture]
	public class FaultActivationAttributeTest
	{
		[Test]
		public void TestCanFaultActivate()
		{
			var failure = new ProxyT();
			ServerT.GetNewServer(failure);
			ServerT.GetNewServer(failure);
			Assert.AreEqual(0, failure.ActiveServerCount);

			//var attribute =
			//(FaultActivationAttribute)Attribute.GetCustomAttribute(typeof(ProxyT.ServerSelectionFailsEffect), typeof(FaultActivationAttribute));
			//Assert.IsFalse((bool) attribute.ActivationProperty.GetValue(failure));
			var properties = failure.GetType().GetFields().Where(p => p.IsDefined(typeof(FaultActivationAttribute), false));
			var prop = properties.First(p => p.Name == "ServerSelectionFails");
			var attr = (FaultActivationAttribute)prop.GetCustomAttribute(typeof(FaultActivationAttribute), false);
			var canAct = (bool)attr.ActivationProperty.GetValue(failure);
			//var canActivate = properties.Count(v => (bool) v.GetValue(failure)) > 0;
			Assert.IsFalse(canAct);

			failure.IncrementServerPool();
			failure.IncrementServerPool();
			Assert.AreEqual(2, failure.ActiveServerCount);
			//Assert.IsTrue((bool) attribute.ActivationProperty.GetValue(failure));
			//canActivate = properties.Count(v => (bool) v.GetValue(failure)) > 0;
			canAct = (bool)attr.ActivationProperty.GetValue(failure);
			Assert.IsTrue(canAct);
		}
	}
}
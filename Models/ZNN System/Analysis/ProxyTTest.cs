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

using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
	/// <summary>
	/// Provides some unit tests for <see cref="ProxyT"/>
	/// </summary>
	[TestFixture]
	public class ProxyTTest
	{
		private ProxyT _Proxy;

		/// <summary>
		/// Setups the tests
		/// </summary>
		[SetUp]
		public void Prepare()
		{
			_Proxy = new ProxyT();
			ServerT.GetNewServer(_Proxy);
			ServerT.GetNewServer(_Proxy);
			ServerT.GetNewServer(_Proxy);
			ServerT.GetNewServer(_Proxy);
		}

		/// <summary>
		/// Tests the server activation
		/// </summary>
		[Test]
		public void TestIncrementServerPool()
		{
			_Proxy.IncrementServerPool();

			Assert.AreEqual(1, _Proxy.ActiveServerCount);
			Assert.IsInstanceOf(typeof(ServerT), _Proxy.ConnectedServers[0]);
		}

		/// <summary>
		/// Tests the server deactivation
		/// </summary>
		[Test]
		public void TestDecrementServerPool()
		{
			_Proxy.IncrementServerPool();
			_Proxy.IncrementServerPool();

			_Proxy.DecrementServerPool();

			Assert.AreEqual(1, _Proxy.ActiveServerCount);

			_Proxy.DecrementServerPool(); // Note: Cannot deactivate last server

			Assert.AreEqual(1, _Proxy.ActiveServerCount);
		}

		/// <summary>
		/// Tests the server adjusting mechanism
		/// </summary>
		[Test]
		public void TestAdjustServers()
		{
			// High Time, low costs
			for(int i = 0; i < Model.LastResponseCountForAvgTime; i++)
			{
				_Proxy.UpdateAvgResponseTime(Model.HighResponseTimeValue + 5);
			}

			_Proxy.AdjustServers();
			Assert.AreEqual(2, _Proxy.ActiveServerCount);

			// High Time, Max Costs
			_Proxy.AdjustServers();
			_Proxy.AdjustServers();
			Assert.AreEqual(3, _Proxy.ActiveServerCount);
			Assert.AreEqual(EServerFidelity.Low, _Proxy.ConnectedServers[0].Fidelity);

			// Low Time, High costs
			for(int i = 0; i < Model.LastResponseCountForAvgTime; i++)
			{
				_Proxy.UpdateAvgResponseTime(Model.LowResponseTimeValue - 5);
			}

			_Proxy.AdjustServers();
			Assert.AreEqual(2, _Proxy.ActiveServerCount);
			Assert.AreEqual(EServerFidelity.High, _Proxy.ConnectedServers[0].Fidelity);
		}
	}
}

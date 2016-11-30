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
		/// <summary>
		/// Tests the server activation
		/// </summary>
		[Test]
		public void TestIncrementServerPool()
		{
			var proxy = new ProxyT();
			proxy.IncrementServerPool();

			Assert.AreEqual(1, proxy.ActiveServerCount);
			Assert.IsInstanceOf(typeof(ServerT), proxy.ConnectedServers[0]);
		}

		/// <summary>
		/// Tests the server deactivation
		/// </summary>
		[Test]
		public void TestDecrementServerPool()
		{
			var proxy = new ProxyT();
			proxy.ConnectedServers.Add(ServerT.GetNewServer());
			proxy.ConnectedServers.Add(ServerT.GetNewServer());

			proxy.DecrementServerPool();

			Assert.AreEqual(1, proxy.ActiveServerCount);

			proxy.DecrementServerPool(); // Note: Cannot deactivate last server

			Assert.AreEqual(1, proxy.ActiveServerCount);
		}

		/// <summary>
		/// Tests the server adjusting mechanism
		/// </summary>
		[Test]
		public void TestAdjustServers()
		{
			var proxy = new ProxyT();
			var ran = new Random();
			
			// High Time, low costs
			for(int i = 0; i < Model.LastResponseCountForAvgTime; i++)
			{
				proxy.UpdateAvgResponseTime(Model.HighResponseTimeValue + 5);
			}

			proxy.AdjustServers();
			Assert.AreEqual(2, proxy.ActiveServerCount);

			// High Time, Max Costs
			proxy.AdjustServers();
			proxy.AdjustServers();
			Assert.AreEqual(3, proxy.ActiveServerCount);
			Assert.AreSame(ServerT.EFidelity.Low, proxy.ConnectedServers[0].FidelityStateMachine.State);
			
			// Low Time, High costs
			for(int i = 0; i < Model.LastResponseCountForAvgTime; i++)
			{
				proxy.UpdateAvgResponseTime(Model.LowResponseTimeValue - 5);
			}

			proxy.AdjustServers();
			Assert.AreEqual(2, proxy.ActiveServerCount);
			Assert.AreSame(ServerT.EFidelity.High, proxy.ConnectedServers[0].FidelityStateMachine.State);
		}
	}
}

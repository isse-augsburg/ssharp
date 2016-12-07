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

using System.Linq;
using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{

	/// <summary>
	/// Privides Unit Tests for <see cref="ServerT"/>
	/// </summary>
	[TestFixture]
	public class ServerTTest
	{
		/// <summary>
		/// Tests the setting of the content fidelity
		/// </summary>
		[Test]
		public void TestSetFidelity()
		{
			var server = ServerT.GetNewServer();

			server.Fidelity=EServerFidelity.High;
			Assert.AreEqual(EServerFidelity.High, server.Fidelity);

			server.Fidelity=EServerFidelity.Medium;
			Assert.AreEqual(EServerFidelity.Medium, server.Fidelity);

			server.Fidelity=EServerFidelity.Low;
			Assert.AreEqual(EServerFidelity.Low, server.Fidelity);
		}

		/// <summary>
		/// Tests the query execution
		/// </summary>
		[Test]
		public void TestExecuteQuery()
		{
			var proxy = new ProxyT();
			var server = ServerT.GetNewServer();
			for (int i = 0; i < server.AvailableServerUnits + 1; i++)
			{
				var client = new ClientT(proxy);
				var query = new Query(client);
				server.AddQuery(query);
			}

			var isExecutedFirst = server.ExecuteQueryStep(server.ExecutingQueries.First());
			var isExecutedLast = server.ExecuteQueryStep(server.ExecutingQueries.Last());

			Assert.True(isExecutedFirst);
			Assert.False(isExecutedLast);
		}
	}
}
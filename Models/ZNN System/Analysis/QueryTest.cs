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
	/// Provides some unit tests for <see cref="Query"/>
	/// </summary>
	[TestFixture]
	public class QueryTest
	{
		private ProxyT _Proxy;
		private ClientT _Client;
		private Query _Query;

		/// <summary>
		/// Test setup
		/// </summary>
		[SetUp]
		public void Prepare()
		{
			_Proxy = new ProxyT();
			_Client = new ClientT(_Proxy);
			_Query = new Query(_Client);
		}

		/// <summary>
		/// Tests the <see cref="Query.Update"/> method
		/// </summary>
		[Test]
		public void TestUpdateServerMultiMode()
		{
			Assert.True(_Query.State == EQueryState.Idle);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.QueryToProxy);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.QueryToServer);
			Assert.NotNull(_Query.SelectedServer);
			Assert.GreaterOrEqual(_Query.SelectedServer.ExecutingQueries.IndexOf(_Query), 0);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.OnServer);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.LowFidelityComplete);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.MediumFidelityComplete);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.HighFidelityComplete);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.ResToProxy);
			Assert.Less(_Query.SelectedServer.ExecutingQueries.IndexOf(_Query), 0);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.ResToClient);

			_Query.Update();
			Assert.True(_Query.State == EQueryState.Idle);
			Assert.Null(_Query.SelectedServer);
		}
	}
}
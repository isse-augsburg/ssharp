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

using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Represents the Client of the ZNN.com News System
	/// </summary>
	public class ClientT : Component
	{
		/// <summary>
		/// Response time of the last query to the server in steps
		/// </summary>
		private int _LastResponseTime;

		/// <summary>
		/// Response time of the current query to the server in steps
		/// </summary>
		private int _CurrentResponseTime;

		/// <summary>
		/// The connected Proxy
		/// </summary>
		private ProxyT _ConnectedProxy;

		/// <summary>
		/// Indicates if the client waits for a response.
		/// </summary>
		private bool _IsResponseWaiting;

		/// <summary>
		/// The current query
		/// </summary>
		public Query CurrentQuery { get; set; }

		/// <summary>
		/// Response time of the last query to the server in ms
		/// </summary>
		public int LastResponseTime => _LastResponseTime;

		/// <summary>
		/// The connected Proxy
		/// </summary>
		public ProxyT ConnectedProxy => _ConnectedProxy;

		/// <summary>
		/// Creates a new client instance and connects it to the proxy
		/// </summary>
		/// <param name="proxy">Connected Proxy</param>
		public ClientT(ProxyT proxy)
		{
			_ConnectedProxy = proxy;
			proxy.ConnectedClients.Add(this);
		}

		/// <summary>
		/// Initialize a new Query
		/// </summary>
		public void StartQuery()
		{
			_IsResponseWaiting = true;
			_CurrentResponseTime = 0;
			CurrentQuery = new Query(this);
		}

		/// <summary>
		/// Finalize a query
		/// </summary>
		public void GetResponse()
		{
			_LastResponseTime = _CurrentResponseTime;
			_IsResponseWaiting = false;
		}

		/// <summary>
		/// Waits for a query
		/// </summary>
		public override void Update()
		{
			if(_IsResponseWaiting)
			{
				_CurrentResponseTime++;
			}
		}

	}
}

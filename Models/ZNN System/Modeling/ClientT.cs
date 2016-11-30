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
		/// Response time of the last query to the server in ms
		/// </summary>
		public int LastResponseTime => _LastResponseTime;

		/// <summary>
		/// The connected Proxy
		/// </summary>
		public ProxyT ConnectedProxy => _ConnectedProxy;

		public ClientT(ProxyT proxy)
		{
			_ConnectedProxy = proxy;
		}

		public void StartQuery()
		{
			_IsResponseWaiting = true;
			_CurrentResponseTime = 0;
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
			else
			{
				StartQuery();
			}
		}

	}
}

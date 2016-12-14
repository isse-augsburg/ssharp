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

using System.Collections.Generic;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Represents the ZNN.com News System case study.
	/// </summary>
	public class Model : ModelBase
	{
		/// <summary>
		/// The cost of one server unit.
		/// </summary>
		public static int ServerUnitCost = 5;

		/// <summary>
		/// Available units per server
		/// </summary>
		public static int DefaultAvailableServerUnits = 10;

		/// <summary>
		/// Defines the value for high response time
		/// </summary>
		public static int HighResponseTimeValue = 20;

		/// <summary>
		/// Defines the value for low response time
		/// </summary>
		public static int LowResponseTimeValue = 10;

		/// <summary>
		/// Available Budget for server costs.
		/// </summary>
		public static int MaxBudget = 125;

		/// <summary>
		/// Count of latest response times to be used for calculating averange response time
		/// </summary>
		public static int LastResponseCountForAvgTime = 10;

		[Root(RootKind.Controller)]
		public ProxyT Proxy;

		public List<ClientT> Clients => Proxy.ConnectedClients;

		public List<ServerT> Servers => Proxy.ConnectedServers;

		/// <summary>
		/// Initializes a new instance
		/// </summary>
		public Model()
		{
			Proxy = new ProxyT();

			// Add a few clients
			for(int i = 0; i < 20; i++)
			{
				Clients.Add(new ClientT(Proxy));
			}
		}

	}
}

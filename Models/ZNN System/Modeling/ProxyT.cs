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
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Represents the Proxy of the ZNN.com News System
	/// </summary>
	public class ProxyT : Component
	{
		/// <summary>
		/// States for response time
		/// </summary>
		public enum EProxyStates
		{
			ResponseTimeHigh,
			ResponseTimeNormal,
			ResponseTimeLow,
			CostsOverLimit,
			CostsNearLimit,
			CostsLow
		}

		/// <summary>
		/// The connected Clients
		/// </summary>
		private List<ClientT> _ConnectedClients;

		/// <summary>
		/// The connected Servers
		/// </summary>
		private List<ServerT> _ConnectedServers;

		/// <summary>
		/// Latest Response Times, use <see cref="UpdateAvgResponseTime"/> to add new times!
		/// </summary>
		private List<int> _LatestResponeTimes;

		/// <summary>
		/// Gets the last round robin selected server for queries
		/// </summary>
		private int _LastSelectedServer = -1;

		/// <summary>
		/// The connected Clients
		/// </summary>
		public List<ClientT> ConnectedClients => _ConnectedClients;

		/// <summary>
		/// The connected Servers
		/// </summary>
		public List<ServerT> ConnectedServers => _ConnectedServers;

		/// <summary>
		/// Average response time of the servers from the last querys.
		/// </summary>
		public int AvgResponseTime => (int) _LatestResponeTimes.Average();

		/// <summary>
		/// Number of active servers
		/// </summary>
		public int ActiveServerCount => ConnectedServers.Where(s => s.Cost > 0).Count();

		/// <summary>
		/// Total costs of all Server
		/// </summary>
		public int TotalServerCosts => ConnectedServers.Sum(s => s.Cost);

		/// <summary>
		/// State Machine
		/// </summary>
		public StateMachine<EProxyStates> ProxyStateMachine = new[] { EProxyStates.ResponseTimeLow, EProxyStates.CostsLow };

		public ProxyT()
		{
			_ConnectedClients = new List<ClientT>();
			_ConnectedServers = new List<ServerT>();
			_LatestResponeTimes = new List<int>(Model.LastResponseCountForAvgTime);

			IncrementServerPool();
		}

		/// <summary>
		/// Activates a new server
		/// </summary>
		public void IncrementServerPool()
		{
			ConnectedServers.Add(ServerT.GetNewServer());
		}

		/// <summary>
		/// Dectivates the server with the lowest load
		/// </summary>
		public void DecrementServerPool()
		{
			if(ConnectedServers.Count > 1)
			{
				var server = ConnectedServers.Aggregate((currMin, x) => ((currMin == null || x.Load < currMin.Load) ? x : currMin));
				ConnectedServers.Remove(server);
			}
		}

		/// <summary>
		/// Switches the servers to text mode
		/// </summary>
		private void SwitchServerToTextMode()
		{
			foreach (var server in ConnectedServers)
				server.Fidelity = EServerFidelity.Low;
		}

		/// <summary>
		/// Switches the servers to multimedia mode
		/// </summary>
		private void SwitchServerToMultiMode()
		{
			foreach(var server in ConnectedServers)
				server.Fidelity = EServerFidelity.High;
		}

		/// <summary>
		/// Updates the averange response time
		/// </summary>
		/// <param name="lastTime">last response time</param>
		internal void UpdateAvgResponseTime(int lastTime)
		{
			if(_LatestResponeTimes.Count >= Model.LastResponseCountForAvgTime)
				_LatestResponeTimes.RemoveAt(0);

			_LatestResponeTimes.Add(lastTime);
		}

		/// <summary>
		/// Selects a server
		/// </summary>
		/// <param name="query">The query</param>
		public ServerT SelectServer(Query query)
		{
			AdjustServers();

			var server = RoundRobinServerSelection();
			server.AddQuery(query);
			return server;
		}

		/// <summary>
		/// Adjust the server pool (size and fidelity)
		/// </summary>
		internal void AdjustServers()
		{
			if(AvgResponseTime > Model.HighResponseTimeValue)
			{
				if(TotalServerCosts < Model.MaxBudget)
					IncrementServerPool();
				else
					SwitchServerToTextMode();
			}

			else
			{
				if(AvgResponseTime < Model.LowResponseTimeValue)
				{
					// Server costs near limit
					if(TotalServerCosts > (Model.MaxBudget * 0.75))
						DecrementServerPool();
				}
				else
				{
					// Random increment or decrement server pool
					if(new Random().Next(0, 2) < 1)
						IncrementServerPool();
					else
						DecrementServerPool();
				}

				SwitchServerToMultiMode();
			}
		}

		/// <summary>
		/// Selects the Server by round robin
		/// </summary>
		/// <returns>Selected Server</returns>
		private ServerT RoundRobinServerSelection()
		{
			if(ConnectedServers.Count > _LastSelectedServer - 1)
				_LastSelectedServer = -1;

			var selected = ConnectedServers[++_LastSelectedServer];
			return selected;
		}

		public override void Update()
		{
			foreach(var client in ConnectedClients)
			{
				Query query = client.CurrentQuery;
			}
		}
	}
}

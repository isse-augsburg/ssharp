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

using System;
using System.Collections.Generic;
using System.Linq;
using SafetySharp.Analysis;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Represents the Proxy of the ZNN.com News System
	/// </summary>
	public class ProxyT : Component
	{

		/// <summary>
		/// In this fault, the server selection for a query fails
		/// </summary>
		[FaultActivation(typeof(ServerSelectionFailsEffect), "CanActiviate")]
		public readonly Fault ServerSelectionFails = new TransientFault();

		/// <summary>
		/// Latest Response Times, use <see cref="UpdateAvgResponseTime"/> to add new times!
		/// </summary>
		private readonly List<int> _LatestResponeTimes;

		/// <summary>
		/// Gets the last round robin selected server for queries
		/// </summary>
		private int _LastSelectedServer = -1;

		/// <summary>
		/// The connected Clients
		/// </summary>
		public List<ClientT> ConnectedClients { get; }

		/// <summary>
		/// The connected Clients
		/// </summary>
		public List<Query> Queries { get; }

		/// <summary>
		/// The connected Servers
		/// </summary>
		public List<ServerT> ConnectedServers { get; }

		/// <summary>
		/// Average response time of the servers from the last querys.
		/// </summary>
		public int AvgResponseTime => (int) _LatestResponeTimes.Average();

		/// <summary>
		/// Number of active servers
		/// </summary>
		public int ActiveServerCount => ConnectedServers.Count(s => s.IsServerActive);

		/// <summary>
		/// Total costs of all Server
		/// </summary>
		public int TotalServerCosts => ConnectedServers.Sum(s => s.Cost);

		/// <summary>
		/// The current server fidelity set by <see cref="SetAllServerFidelity"/>
		/// </summary>
		internal EServerFidelity CurrentServerFidelity { get; set; }

		/// <summary>
		/// Creates a new ProxyT instance
		/// </summary>
		public ProxyT()
		{
			ConnectedClients = new List<ClientT>();
			ConnectedServers = new List<ServerT>();
			Queries = new List<Query>();
			_LatestResponeTimes = new List<int>(Model.LastResponseCountForAvgTime);
			UpdateAvgResponseTime(0); // Default start value

			//GenerateConstraints();
			//IncrementServerPool();
			
			CurrentServerFidelity = EServerFidelity.High;
		}

		/// <summary>
		/// Activates a new server
		/// </summary>
		public void IncrementServerPool()
		{
			var inactiveServer = ConnectedServers.FirstOrDefault(s => !s.IsServerActive);
			inactiveServer?.Activate();
		}

		/// <summary>
		/// Dectivates the server with the lowest load and adds the queries to the first active server
		/// </summary>
		public virtual void DecrementServerPool()
		{
			if(ActiveServerCount > 1)
			{
				var server = ConnectedServers.Where(s => s.IsServerActive).Aggregate((currMin, x) => ((currMin == null || x.Load < currMin.Load) ? x : currMin));
				server.Deactivate();

				// Add queries to active server
				var queries = server.ExecutingQueries;
				var newServer = ConnectedServers.First(s => s.IsServerActive);
				foreach(var query in queries)
				{
					newServer.ExecutingQueries.Add(query);
					query.SelectedServer = newServer;
				}
			}
		}

		/// <summary>
		/// Switches the servers to text mode
		/// </summary>
		internal void SwitchServerToTextMode()
		{
			SetAllServerFidelity(EServerFidelity.Low);
		}

		/// <summary>
		/// Switches the servers to multimedia mode
		/// </summary>
		internal void SwitchServerToMultiMode()
		{
			SetAllServerFidelity(EServerFidelity.High);
		}

		/// <summary>
		/// Sets the fidelity for each server
		/// </summary>
		/// <param name="fidelity">The server fidelity</param>
		internal void SetAllServerFidelity(EServerFidelity fidelity)
		{
			foreach(var server in ConnectedServers)
				server.Fidelity = fidelity;
			CurrentServerFidelity = fidelity;
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
		/// Selects a server, adds the query to its queue and sets <see cref="Query.SelectedServer"/>
		/// </summary>
		/// <param name="query">The query</param>
		public void SelectServer(Query query)
		{
			//AdjustServers();

			var server = RoundRobinServerSelection();
			if(server == null)
				throw new Exception("Cannot select a server");
			server.AddQuery(query);
			query.SelectedServer = server;
		}

		/// <summary>
		/// Adjust the server pool (size and fidelity)
		/// </summary>
		internal virtual void AdjustServers()
		{
			if(ActiveServerCount < 1)
				IncrementServerPool();

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
		/// Selects the Server by round robin and returns it, or null if no server available
		/// </summary>
		/// <returns>Selected Server or null if no server available</returns>
		protected virtual ServerT RoundRobinServerSelection()
		{
			if(ConnectedServers.Count > _LastSelectedServer - 1)
				_LastSelectedServer = -1;

			if(ActiveServerCount < 1)
				return null;

			ServerT selected;
			do
			{
				selected = ConnectedServers[++_LastSelectedServer];
			} while(!ConnectedServers[_LastSelectedServer].IsServerActive);

			return selected;
		}

		/// <summary>
		/// Update Method
		/// </summary>
		public override void Update()
		{
			base.Update();
		}

		/// <summary>
		/// In this fault, the server selection for a query fails
		/// </summary>
		[FaultEffect(Fault = nameof(ServerSelectionFails))]
		//[FaultActivation(typeof(ServerSelectionFailsEffect), "CanActiviate")]
		public class ServerSelectionFailsEffect : ProxyT
		{
			//public bool CanActivate()
			//{
			//	return ActiveServerCount > 1;
			//}
			//[FaultActivation]
			public bool CanActiviate => ActiveServerCount > 1;

			/// <summary>
			/// Selects the Server by round robin
			/// </summary>
			/// <returns>Selected Server</returns>
			protected override ServerT RoundRobinServerSelection()
			{
				return null;
			}
		}
	}
}

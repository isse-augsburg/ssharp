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
	/// Represents the Server of the ZNN.com News System
	/// </summary>
	public class ServerT : Component
	{
		/// <summary>
		/// This faults prevents the server activation to activate a server
		/// </summary>
		public readonly Fault ServerCannotActivated = new TransientFault();

		/// <summary>
		/// This fault prevents the server deactivation to deactivate a server
		/// </summary>
		public readonly Fault ServerCannotDeactivated = new TransientFault();

		/// <summary>
		/// This faults prevents the server to change the fidelity
		/// </summary>
		public readonly Fault SetServerFidelityFails = new TransientFault();

		/// <summary>
		/// This faults prevents the server to execute a query
		/// </summary>
		public readonly Fault CannotExecuteQueries = new TransientFault();

		/// <summary>
		/// Currently available units of the server. 0 Means the server is inactive.
		/// </summary>
		public int AvailableServerUnits { get; private set; }

		/// <summary>
		/// Is the server active and can execute queries
		/// </summary>
		public bool IsServerActive => AvailableServerUnits > 0;

		/// <summary>
		/// Maximum available units of the server.
		/// </summary>
		private int MaxServerUnits { get; set; }

		/// <summary>
		/// Current costs of the Server. 0 means the server is inactive.
		/// </summary>
		public int Cost => AvailableServerUnits * Model.ServerUnitCost;

		/// <summary>
		/// Currently units under use of the server.
		/// </summary>
		public int UsedServerUnits => ExecutingQueries.Count;

		/// <summary>
		/// Current load of the Server in %.
		/// </summary>
		[Range(0, 100, OverflowBehavior.Clamp)]
		public int Load => UsedServerUnits / AvailableServerUnits * 100;

		private EServerFidelity _Fidelity;

		/// <summary>
		/// Current content fidelity level of the server.
		/// </summary>
		public virtual EServerFidelity Fidelity
		{
			get { return _Fidelity;}
			internal set { _Fidelity = value; }
		}

		/// <summary>
		/// The current executing queries
		/// </summary>
		internal List<Query> ExecutingQueries { get; private set; }

		/// <summary>
		/// The connected Proxy
		/// </summary>
		public ProxyT ConnectedProxy { get; private set; }

		/// <summary>
		/// Indicates if the server is initialized
		/// </summary>
		public virtual bool IsInitialized { get; private set; }

		/// <summary>
		/// Counts the completed queries
		/// </summary>
		public int QueryCompleteCount { get; protected set; }

		/// <summary>
		/// Creates a new server instance
		/// </summary>
		private ServerT() { }

		/// <summary>
		/// Initialize a new server instance
		/// </summary>
		/// <param name="maxServerUnits">Max Server capacity units</param>
		/// <param name="proxy">The connected Proxy</param>
		private void Initialize(int maxServerUnits, ProxyT proxy)
		{
			MaxServerUnits = maxServerUnits;
			ExecutingQueries = new List<Query>(MaxServerUnits);
			Fidelity = EServerFidelity.High;
			ConnectedProxy = proxy;
			proxy.ConnectedServers.Add(this);
			IsInitialized = true;
		}

		/// <summary>
		/// Gets a new server and connects it to the proxy
		/// </summary>
		/// <param name="proxy">The connected Proxy</param>
		/// <returns>New Server Instance</returns>
		public static ServerT GetNewServer(ProxyT proxy = null)
		{
			var server = new ServerT();
			server.Initialize(Model.DefaultAvailableServerUnits, proxy);
			if(server.IsInitialized)
				return server;
			return null;
		}

		/// <summary>
		/// Activates the server
		/// </summary>
		/// <returns></returns>
		public virtual bool Activate()
		{
			AvailableServerUnits = MaxServerUnits;
			return true;
		}

		/// <summary>
		/// Deactivates the server
		/// </summary>
		/// <returns></returns>
		public virtual bool Deactivate()
		{
			AvailableServerUnits = 0;
			return true;
		}

		/// <summary>
		/// Adds the query to the list to be executing
		/// </summary>
		/// <param name="query">The query</param>
		public void AddQuery(Query query)
		{
			ExecutingQueries.Add(query);
		}

		/// <summary>
		/// Simulates an execution step of the query and returns true if the query was executed
		/// </summary>
		/// <param name="query">The query</param>
		/// <returns>True if the query was executed</returns>
		public virtual bool ExecuteQueryStep(Query query)
		{
			return ExecutingQueries.IndexOf(query) < AvailableServerUnits;
		}

		/// <summary>
		/// Remove a completly executed query from the list
		/// </summary>
		/// <param name="query"></param>
		public void QueryComplete(Query query)
		{
			QueryCompleteCount++;
			ExecutingQueries.Remove(query);
		}

		/// <summary>
		/// Prevents the server activation to activate a server
		/// </summary>
		[FaultEffect(Fault = "ServerCannotActivated")]
		public class ServerCannotActivatedEffect : ServerT
		{
			/// <summary>
			/// Activates the server
			/// </summary>
			/// <returns></returns>
			public override bool Activate()
			{
				return false;
			}
		}

		/// <summary>
		/// Prevents the server activation to deactivate a server
		/// </summary>
		[FaultEffect(Fault = "ServerCannotDeactivated")]
		public class ServerCannotDeactivatedEffect : ServerT
		{
			/// <summary>
			/// Deactivates the server
			/// </summary>
			/// <returns></returns>
			public override bool Deactivate()
			{
				return false;
			}
		}

		/// <summary>
		/// Prevents the server to change the fidelity
		/// </summary>
		[FaultEffect(Fault = "SetServerFidelityFails")]
		public class SetServerFidelityFailsEffect : ServerT
		{
			/// <summary>
			/// Current content fidelity level of the server.
			/// </summary>
			public override EServerFidelity Fidelity => _Fidelity;
		}

		/// <summary>
		/// Prevents the server to execute a query
		/// </summary>
		[FaultEffect(Fault = "CannotExecuteQueries")]
		public class CannotExecuteQueriesEffect : ServerT
		{
			/// <summary>
			/// Simulates an execution step of the query and returns true if the query was executed
			/// </summary>
			/// <param name="query">The query</param>
			/// <returns>True if the query was executed</returns>
			public override bool ExecuteQueryStep(Query query)
			{
				return false;
			}
		}
	}
}

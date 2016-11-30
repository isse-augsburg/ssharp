using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.ZNNSystem.Modeling
{
	/// <summary>
	/// Class for Query and Response
	/// </summary>
	public class Query
	{
		/// <summary>
		/// States for query states
		/// </summary>
		public enum State
		{
			Idle,
			QueryToProxy,
			QueryToServer,
			OnServer,
			LowFidelityComplete,
			MediumFidelityComplete,
			HighFidelityComplete,
			ResToProxy,
			ResToClient
		}

		/// <summary>
		/// State machine for query states
		/// </summary>
		public StateMachine<State> StateMachine = State.Idle;

		/// <summary>
		/// The server to execute the query
		/// </summary>
		public ServerT SelectedServer { get; set; }

		/// <summary>
		/// Creates a new query instance
		/// </summary>
		public Query()
		{

		}


	}
}

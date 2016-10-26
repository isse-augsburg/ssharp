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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;

	public class CoalitionReconfigurationAgent : IReconfigurationAgent
	{
		protected Coalition CurrentCoalition { get; set; }

		public void Acknowledge()
		{
			throw new NotImplementedException();
		}

		public void StartReconfiguration(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			//if (state.isRequest) // also handle (IsRequest && IsLocalViolation)
			{
				// if CurrentCoalition == null
				//	join
				// else
				//	merge
			}
			//else if (state.isLocalViolation)
			{
				// determine required capabilities
				// start coalition
				// invite coalition neighbors until required capabilities are satisfied
				// calculate TFR
				// invite edge agents where necessary
				// calculate new role assignment
				// all coalition members update base agent configurations
				// (synchronize)
				// all coalition members send Go() to base agents
				// coalition disbanded, reconf agents forgotten
			}
			//else // agent breakdown
			{
				// question (not invite) non-neighbors until successor/predecessor of failed agent discovered
				// (this requires knowledge of non-neighboring agents, or maybe a broadcast?)
				// follow resource flow until closest living neighbor of failed agent found
				// coalition clash -> merge, select new leader
				// continue as described above

				// idea: predecessor/successor search can also be achieved by broadcast ONLY among
				// instances of this class (easier to implement as well)
			}
			throw new NotImplementedException();
		}

		protected class Coalition
		{
			public CoalitionReconfigurationAgent Leader { get; }

			public List<CoalitionReconfigurationAgent> Members { get; }
				= new List<CoalitionReconfigurationAgent>();

			private int _ctfStart = -1;
			private int _ctfEnd = -1;

			private int _tfrStart = -1;
			private int _tfrEnd = -1;

			public Coalition(CoalitionReconfigurationAgent leader)
			{
				Leader = leader;
				Members.Add(leader);
			}

			public void Join(CoalitionReconfigurationAgent newMember)
			{
				Members.Add(newMember);
			}
		}
	}
}
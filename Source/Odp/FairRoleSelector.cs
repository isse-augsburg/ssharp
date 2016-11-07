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
	using System.Linq;

	using Modeling;

	/// <summary>
	///  A fair role selection algorithm
	///  (cf. Konstruktion selbst-organisierender Softwaresysteme, section 6.3)
	/// </summary>
	/// <remarks>
	///  The implementation slightly deviates from the original algorithm:
	///  Counting up the current time (for timestamps) and number of role
	///  applications results in an infinite (or very large) number of states.
	///  Instead, the order in which the roles were last applied is used as
	///  pseudo-application times. Similarly, the (insertion) order of resource
	///  requests is used in place of timestamps.
	/// </remarks>
	public class FairRoleSelector : IRoleSelector
	{
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<Role, int> _roleIndex = new Dictionary<Role, int>();

		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<BaseAgent.ResourceRequest, uint> _timeStamps = new Dictionary<BaseAgent.ResourceRequest, uint>();

		[Hidden]
		private uint _currentTime;

		private readonly byte[] _applicationTimes = new byte[BaseAgent.MaximumRoleCount];

		protected BaseAgent Agent { get; }

		public FairRoleSelector(BaseAgent agent)
		{
			Agent = agent;
		}

		public virtual Role? ChooseRole(IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			var candidateRoles = Agent.AllocatedRoles.Where(CanExecute).ToArray();
			if (!candidateRoles.Any())
				return null;

			ComputeCachedData(resourceRequests);

			var chosenRole = candidateRoles.Aggregate((r, s) => ChooseRole(r, s, resourceRequests));
			UpdateRoleOrder(chosenRole, Agent.AllocatedRoles.Count());

			// cleanup
			_roleIndex.Clear();
			_timeStamps.Clear();

			return chosenRole;
		}

		protected virtual bool CanExecute(Role role) => Agent.CanExecute(role);

		private void UpdateRoleOrder(Role chosenRole, int roleCount)
		{
			var oldPosition = _applicationTimes[_roleIndex[chosenRole]];
			// move the roles which were previously behind chosenRole in the list up one slot
			for (var i = 0; i < roleCount; ++i)
			{
				if (_applicationTimes[i] > oldPosition)
					_applicationTimes[i]--;
			}
			// move chosenRole to the back of the list
			_applicationTimes[_roleIndex[chosenRole]] = checked((byte)roleCount);
		}

		private void ComputeCachedData(IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			// cache role index to avoid multiple IndexOf() calls
			var j = 0;
			foreach (var role in Agent.AllocatedRoles)
				_roleIndex[role] = j++;

			// compute pseudo-timestamps for resource requests
			_currentTime = 0;
			foreach (var request in resourceRequests)
				_timeStamps[request] = _currentTime++;
		}

		private Role ChooseRole(Role role1, Role role2, IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			var fitness1 = Fitness(role1);
			var fitness2 = Fitness(role2);

			// role with higher fitness wins
			if (fitness1 > fitness2)
				return role1;
			else if (fitness1 < fitness2)
				return role2;
			else
			{
				// same fitness => older resource request wins
				var timeStamp1 = GetTimeStamp(role1, resourceRequests);
				var timeStamp2 = GetTimeStamp(role2, resourceRequests);

				if (timeStamp1 <= timeStamp2)
					return role1;
				return role2;
			}
		}

		private uint GetTimeStamp(Role role, IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			// for roles without request (production roles) use current time
			return (from request in resourceRequests
					where role.Equals(request.Role)
					select _timeStamps[request]
				).DefaultIfEmpty(_currentTime).Single();
		}

		private int Fitness(Role role)
		{
			return -_applicationTimes[_roleIndex[role]]; // prefer roles with lower (earlier) application time
		}
	}
}
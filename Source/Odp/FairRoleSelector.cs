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
	using System.Collections.Generic;
	using System.Linq;

	using Modeling;

	/// <summary>
	/// A fair role selection algorithm
	/// (Konstruktion selbst-organisierender Softwaresysteme, section 6.3).
	/// </summary>
	/// <remarks>
	/// The implementation slightly deviates from the original algorithm:
	/// Counting up the current time (for timestamps) and number of role applications
	/// results in an infinite (or very large) number of states.
	/// Instead, the list of roles is treated as round-robin priority queue:
	/// The first applicable role is chosen, and moved to the end of the list.
	/// Similarly, the (insertion) order of resource requests is used in place of timestamps.
	/// </remarks>
	public class FairRoleSelector : IRoleSelector
	{
		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<Role, uint> _roleOrder = new Dictionary<Role, uint>();

		[NonDiscoverable, Hidden(HideElements = true)]
		private readonly Dictionary<BaseAgent.ResourceRequest, uint> _timeStamps
			= new Dictionary<BaseAgent.ResourceRequest, uint>();

		[Hidden]
		private uint _currentTime;

		protected BaseAgent Agent { get; }

		public FairRoleSelector(BaseAgent agent)
		{
			Agent = agent;
		}

		public Role? ChooseRole(List<Role> roles, IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			var candidateRoles = roles.Where(CanExecute).ToArray();
			if (!candidateRoles.Any())
				return null;

			ComputePriorities(roles, resourceRequests);
			var chosenRole = candidateRoles.Aggregate((r, s) => ChooseRole(r, s, resourceRequests));
			UpdateRoleOrder(roles, chosenRole);

			return chosenRole;
		}

		protected virtual bool CanExecute(Role role) => Agent.CanExecute(role);

		private void ComputePriorities(List<Role> roles, IEnumerable<BaseAgent.ResourceRequest> resourceRequests)
		{
			// cache role order to avoid multiple IndexOf() calls
			_roleOrder.Clear();
			uint i = (uint)roles.Count;
			foreach (var role in roles)
				_roleOrder[role] = i--;

			// compute pseudo-timestamps for resource requests
			_timeStamps.Clear();
			_currentTime = 0;
			foreach (var request in resourceRequests)
				_timeStamps[request] = _currentTime++;
		}

		private void UpdateRoleOrder(List<Role> roles, Role chosenRole)
		{
			int index = roles.Count - (int)_roleOrder[chosenRole];
			roles.RemoveAt(index);
			roles.Add(chosenRole);
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

		protected virtual uint Fitness(Role role)
		{
			return _roleOrder[role];
		}
	}
}

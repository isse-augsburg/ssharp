// The MIT License (MIT)
//
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
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

	/// <summary>
	///   Allows iteration over a task's resource flow, from a given starting point.
	/// </summary>
	public static class ResourceFlowWalker
	{
		/// <summary>
		///   Iterates over <paramref name="startRole"/>'s task's resource flow, starting from <paramref name="startAgent"/> and <paramref name="startRole"/>.
		/// </summary>
		public static IEnumerable<Role.Allocation> WalkForward(BaseAgent startAgent, Role startRole)
		{
			return Walk(startAgent, startRole, role => role.Output, (currentRole, role) => role.PreCondition.StateMatches(currentRole.PostCondition));
		}

		/// <summary>
		///   Iterates over <paramref name="start"/>'s role's task's resource flow, starting at the agent and role given by <paramref name="start"/>.
		/// </summary>
		public static IEnumerable<Role.Allocation> WalkForward(Role.Allocation start)
		{
			return WalkForward(start.Agent, start.Role);
		}

		/// <summary>
		///   Iterates backwards over <paramref name="startRole"/>'s task's resource flow, starting from <paramref name="startAgent"/> and <paramref name="startRole"/>.
		/// </summary>
		public static IEnumerable<Role.Allocation> WalkBackward(BaseAgent startAgent, Role startRole)
		{
			return Walk(startAgent, startRole, role => role.Input, (currentRole, role) => role.PostCondition.StateMatches(currentRole.PreCondition));
		}

		/// <summary>
		///   Iterates backwards over <paramref name="start"/>'s role's task's resource flow, starting at the agent and role given by <paramref name="start"/>.
		/// </summary>
		public static IEnumerable<Role.Allocation> WalkBackward(Role.Allocation start)
		{
			return WalkBackward(start.Agent, start.Role);
		}

		private static IEnumerable<Role.Allocation> Walk(BaseAgent startAgent, Role startRole, Func<Role, BaseAgent> agentSelector,
																Func<Role, Role, bool> roleSelector)
		{
			var currentAgent = startAgent;
			var currentRole = startRole;

			while (currentAgent != null)
			{
				yield return new Role.Allocation(currentAgent, currentRole);

				currentAgent = agentSelector(currentRole);
				if (currentAgent != null)
					currentRole = currentAgent.AllocatedRoles.Single(role => roleSelector(currentRole, role));
			}
		}
	}
}

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

namespace SafetySharp.Odp.Reconfiguration
{
	/// <summary>
	///   An interface implemented by reconfiguration agents for (partially) distributed reconfiguration.
	/// </summary>
	/// <remarks>
	///   One reconfiguration agent is associated to exactly one <see cref="BaseAgent"/>
	///   and is responsible for reconfiguring exactly one <see cref="ITask"/>.
	///   The reconfiguration agent must not communicate with the <see cref="BaseAgent"/>
	///   directly, but via a <see cref="ReconfigurationAgentHandler"/> passed to its constructor.
	/// </remarks>
	public interface IReconfigurationAgent : IAgent
	{
		/// <summary>
		///   Called to notify the reconfiguration agent, that its <see cref="BaseAgent"/>
		///   requested it perform the given <paramref name="reconfiguration"/>.
		/// </summary>
		/// <remarks>
		///   This may be called because the agent detected an invariant violation,
		///   or for an initial configuration for the <paramref name="task"/>,
		///   or because another agent requested its participation in an ongoing reconfiguration.
		///   It may be called several times during the lifetime of the reconfiguration agent
		///   - at most once for either of the first two reasons, but an unlimited number of times
		///   for the last reason.
		/// </remarks>
		/// <param name="reconfiguration">A <see cref="ReconfigurationRequest"/> describing the reconfiguration agent should perform.</param>
		void StartReconfiguration(ReconfigurationRequest reconfiguration);

		/// <summary>
		///   Called by the <see cref="ReconfigurationAgentHandler"/> in response to <see cref="ReconfigurationAgentHandler.UpdateAllocatedRoles"/>.
		///   Notifies the reconfiguration agent that the requested updates have been successfully applied.
		/// </summary>
		void Acknowledge();
	}
}
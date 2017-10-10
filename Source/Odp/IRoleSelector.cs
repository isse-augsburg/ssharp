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
	using JetBrains.Annotations;

	/// <summary>
	///  A strategy interface for choosing a <see cref="Role"/> to be executed next by a <see cref="BaseAgent"/>.
	///  Each instance is associated with one <see cref="BaseAgent"/>.
	/// </summary>
	public interface IRoleSelector
	{
		/// <summary>
		///  Chooses the role to be executed.
		/// </summary>
		/// <param name="resourceRequests">The agent's resource requests.</param>
		/// <returns>
		///  <c>null</c> if the agent should not begin execution of a role at the moment.
		///  Otherwise, one of the given <paramref name="resourceRequests"/>' <see cref="BaseAgent.ResourceRequest.Role"/>.
		/// </returns>
		Role? ChooseRole([NotNull] IEnumerable<BaseAgent.ResourceRequest> resourceRequests);

		/// <summary>
		///  Notifies the selector its <see cref="BaseAgent"/>'s role allocations have changed.
		/// </summary>
		void OnRoleAllocationsChanged();
	}
}

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
	using System;
	using JetBrains.Annotations;

	/// <summary>
	///   Represents a <see cref="BaseAgent"/>'s request for a reconfiguration.
	/// </summary>
	public struct ReconfigurationRequest
	{
		/// <summary>
		///   The task that should be reconfigured.
		/// </summary>
		[NotNull]
		public ITask Task { get; }

		/// <summary>
		///   The reason for the reconfiguration.
		/// </summary>
		[NotNull]
		public ReconfigurationReason Reason { get; }

		/// <summary>
		///   Creates a new request. always use this instead of the default constructor.
		/// </summary>
		public ReconfigurationRequest([NotNull] ITask task, [NotNull] ReconfigurationReason reason)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));
			if (reason == null)
				throw new ArgumentNullException(nameof(reason));

			Task = task;
			Reason = reason;
		}

		public static ReconfigurationRequest Initial(ITask task)
		{
			return new ReconfigurationRequest(task, ReconfigurationReason.InitialReconfiguration.Instance);
		}

		public static ReconfigurationRequest Request(ITask task, IAgent agent)
		{
			return new ReconfigurationRequest(task, new ReconfigurationReason.ParticipationRequested(agent));
		}

		public static ReconfigurationRequest Violation(ITask task, InvariantPredicate[] predicates)
		{
			return new ReconfigurationRequest(task, new ReconfigurationReason.InvariantsViolated(predicates));
		}
	}
}

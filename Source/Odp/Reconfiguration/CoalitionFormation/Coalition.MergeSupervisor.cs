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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

	partial class Coalition
	{
		/// <summary>
		///   Handles coalition merges for a <see cref="Coalition"/>.
		/// </summary>
		private class MergeSupervisor
		{
			private readonly LinkedList<MergeRequest> _mergeRequests = new LinkedList<MergeRequest>();

			private TaskCompletionSource<object> _pendingCoalitionMerge;

			private CoalitionReconfigurationAgent _awaitingRendezvousFrom;

			private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

			private readonly CancellationToken _cancel;

			private readonly Coalition _coalition;

			public MergeSupervisor(Coalition coalition)
			{
				_coalition = coalition;
				_cancel = _cancellation.Token;
			}

			/// <summary>
			///   Handles merge requests created when members are invited by another coalition.
			/// </summary>
			/// <exception cref="OperationCanceledException">Thrown if a merge results in dissolution of the coalition.</exception>
			/// <remarks>Must be executed in this instance's <see cref="_coalition"/>'s execution context.</remarks>
			public void ProcessMergeRequests()
			{
				while (_mergeRequests.Count > 0)
				{
					var request = _mergeRequests.First.Value;
					_mergeRequests.RemoveFirst();
					Debug.WriteLine("Coalition with leader {0} is processing merge request (received when leader was {1}) from coalition with leader {2}",
						_coalition.Leader.BaseAgent.Id,
						request.OriginalLeader.BaseAgent.Id,
						request.OpposingLeader.BaseAgent.Id);

					ExecuteCoalitionMerge(request);
					_cancel.ThrowIfCancellationRequested();
				}
			}

			/// <summary>
			///   Waits for a pending merge (if any) to complete,
			///   while at the same time avoiding deadlocks due to cyclic merge requests.
			/// </summary>
			/// <exception cref="OperationCanceledException">Thrown if a merge results in dissolution of the coalition.</exception>
			/// <remarks>Must be executed in this instance's <see cref="_coalition"/>'s execution context.</remarks>
			public async Task WaitForMergeCompletion()
			{
				// The coalition might already have been merged & disbanded
				_cancel.ThrowIfCancellationRequested();

				Debug.WriteLine("Coalition with leader {0} waiting for merges to complete.", _coalition.Leader.BaseAgent.Id);
				while (_pendingCoalitionMerge != null && !_pendingCoalitionMerge.Task.IsCompleted)
				{
					// wait for some time, then do the deadlock check
					await System.Threading.Tasks.Task.Yield();

					// The coalition might have been disbanded
					_cancel.ThrowIfCancellationRequested();

					// detect deadlock
					if (_mergeRequests.All(request => request.OpposingLeader != _awaitingRendezvousFrom))
						continue;

					Debug.WriteLine("Coalition with leader {0}: merge-deadlock detected");
					// determine which agent has to break the deadlock
					var deadlockBreaker = DetermineLeader(_coalition.Leader, _awaitingRendezvousFrom);
					if (deadlockBreaker != _coalition.Leader)
					{
						Debug.WriteLine("Deadlock must be broken by other coalition");
						continue;
					}

					Debug.WriteLine("Breaking deadlock!");
					// break the deadlock
					_awaitingRendezvousFrom = null;
					_pendingCoalitionMerge = null;

					var deadlockRequest = _mergeRequests.First(request => request.OpposingLeader == _awaitingRendezvousFrom);
					_mergeRequests.Remove(deadlockRequest);
					ExecuteCoalitionMerge(deadlockRequest);
					_cancel.ThrowIfCancellationRequested(); // The coalition might have been disbanded
				}
				Debug.WriteLine("Coalition with leader {0}: merges have completed", _coalition.Leader.BaseAgent.Id);
			}

			/// <summary>
			///   Called by members to notify the coalition they have been invited by another coalition.
			/// </summary>
			/// <param name="source">The agent that invited a member, i.e. the leader of the other coalition.</param>
			/// <remarks>May be called from any execution context.</remarks>
			public void MergeCoalition(CoalitionReconfigurationAgent source)
			{
				Debug.WriteLine("Coalition with leader {0} received merge request from coalition with leader {1}", _coalition.Leader.BaseAgent.Id, source.BaseAgent.Id);
				_mergeRequests.AddLast(new MergeRequest(_coalition.Leader, source));
			}

			/// <summary>
			///   Notifies the coalition that an invited agent already belongs to a different coalition,
			///   and that it will receive a <see cref="RendezvousRequest(Coalition, CoalitionReconfigurationAgent)"/> from
			///   the opposing <paramref name="leader"/>.
			/// </summary>
			/// <remarks>May be called from any execution context.</remarks>
			public void AwaitRendezvous(CoalitionReconfigurationAgent invitedAgent, CoalitionReconfigurationAgent leader)
			{
				Debug.WriteLine("Coalition with leader {0} awaiting rendezvous from coalition with leader {1}",
					_coalition.Leader.BaseAgent.Id,
					leader.BaseAgent.Id);
				_awaitingRendezvousFrom = leader;
				_pendingCoalitionMerge = new TaskCompletionSource<object>();

				_coalition.ReceiveInvitationResponse(invitedAgent);
				_coalition.CancelInvitations();
			}

			/// <summary>
			/// Informs the coalition of a merge with another coalition.
			/// </summary>
			/// <param name="chosenCoalition">The coalition which will take over, i.e., either this instance or the other coalition.</param>
			/// <param name="inNameOf">The agent who lead the other coalition when the <see cref="AwaitRendezvous"/>
			/// message was sent.</param>
			/// <remarks>Called from the opposite coalition's execution context.</remarks>
			private void RendezvousRequest(Coalition chosenCoalition, CoalitionReconfigurationAgent inNameOf)
			{
				Debug.Assert(_awaitingRendezvousFrom == inNameOf,
					$"Awaiting rendezvous from agent #{_awaitingRendezvousFrom.BaseAgent.Id}, but received from agent #{inNameOf.BaseAgent.Id}.");
				Debug.WriteLine("Coalition with leader {0} received rendezvous request in name of {1}",
					_coalition.Leader.BaseAgent.Id,
					inNameOf.BaseAgent.Id
				);
				_awaitingRendezvousFrom = null;

				if (chosenCoalition == _coalition)
				{
					_pendingCoalitionMerge.SetResult(null);
					_pendingCoalitionMerge = null;
					return;
				}

				// actual merge
				Debug.WriteLine("Merging coalition with leader {0} into coalition with leader {1}", _coalition.Leader.BaseAgent.Id, chosenCoalition.Leader.BaseAgent.Id);
				foreach (var member in _coalition.Members)
					chosenCoalition.Join(member);

				// stop controller from continuing
				_pendingCoalitionMerge.SetResult(null);
				_pendingCoalitionMerge = null;
				_cancellation.Cancel();

				Debug.WriteLine("Sending coalition information to the other coalition");
				chosenCoalition.Merger.ReceiveCoalitionInformation(_mergeRequests, _coalition.ViolatedPredicates, _coalition.IsInitialConfiguration);
			}

			/// <summary>
			/// Passes meta-information from a coalition that is merged into this instance.
			/// </summary>
			/// <exception cref="RestartReconfigurationException">Always thrown.</exception>
			/// <remarks>Must be executed in this instance's <see cref="_coalition"/>'s execution context.</remarks>
			[ContractAnnotation("=> halt")]
			private void ReceiveCoalitionInformation(IEnumerable<MergeRequest> mergeRequests, IEnumerable<InvariantPredicate> violatedPredicates, bool initialConf)
			{
				foreach (var request in mergeRequests)
					_mergeRequests.AddLast(request);
				_coalition.ViolatedPredicates = _coalition.ViolatedPredicates.Concat(violatedPredicates).ToArray();
			    _coalition.IsInitialConfiguration = _coalition.IsInitialConfiguration || initialConf;

				throw new RestartReconfigurationException();
			}

			/// <summary>
			/// Executes the merge of two coalitions.
			/// </summary>
			/// <param name="request">The <see cref="MergeRequest"/> being executed.</param>
			/// <remarks>Must be executed in this instance's <see cref="_coalition"/>'s execution context.</remarks>
			private void ExecuteCoalitionMerge(MergeRequest request)
			{
				if (request.OpposingLeader.CurrentCoalition == _coalition)
				{
					Debug.WriteLine("Ignoring merge request: coalitions already merged");
					return;
				}

				var leader = DetermineLeader(_coalition.Leader, request.OpposingLeader);
				Debug.WriteLine("Leader of merged coalition will be {0}", leader.BaseAgent.Id);
				request.OpposingMerger.RendezvousRequest(chosenCoalition: leader.CurrentCoalition, inNameOf: request.OriginalLeader);
			}

			/// <summary>
			/// Given two coalition leaders, determines which one will lead the merged coalition.
			/// </summary>
			/// <remarks>May be called from any execution context.</remarks>
			[Pure]
			private static CoalitionReconfigurationAgent DetermineLeader(CoalitionReconfigurationAgent leader1, CoalitionReconfigurationAgent leader2)
			{
				if (leader1.BaseAgent.Id < leader2.BaseAgent.Id)
					return leader1;
				return leader2;
			}

			/// <summary>
			/// Represents the need to merge two coalitions.
			/// </summary>
			private struct MergeRequest
			{
				public MergeRequest([NotNull] CoalitionReconfigurationAgent originalLeader, [NotNull] CoalitionReconfigurationAgent opposingLeader)
				{
					if (originalLeader == null)
						throw new ArgumentNullException(nameof(originalLeader));
					if (opposingLeader == null)
						throw new ArgumentNullException(nameof(opposingLeader));

					OriginalLeader = originalLeader;
					OpposingLeader = opposingLeader;
				}

				/// <summary>
				/// Leader of the coalition that shall be merged.
				/// </summary>
				[NotNull]
				public CoalitionReconfigurationAgent OpposingLeader { get; }

				[NotNull]
				public MergeSupervisor OpposingMerger => OpposingLeader.CurrentCoalition.Merger;

				/// <summary>
				/// The original leader of the coalition that first created the request.
				/// </summary>
				[NotNull]
				public CoalitionReconfigurationAgent OriginalLeader { get; }
			}
		}
	}
}

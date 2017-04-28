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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	partial class Coalition
	{
		private class MergeSupervisor
		{
			private readonly LinkedList<MergeRequest> _mergeRequests = new LinkedList<MergeRequest>();

			private TaskCompletionSource<object> _pendingCoalitionMerge;

			private CoalitionReconfigurationAgent _awaitingRendezvousFrom;

			private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

			public CancellationToken Cancel => _cancellation.Token;


			private readonly Coalition _coalition;

			public MergeSupervisor(Coalition coalition)
			{
				_coalition = coalition;
			}


			/// <summary>
			/// Handles merge requests created when members are invited by another coalition.
			/// </summary>
			/// <exception cref="OperationCanceledException">Thrown if a merge results in dissolution of the coalition.</exception>
			public void ProcessMergeRequests()
			{
				while (_mergeRequests.Count > 0)
				{
					var request = _mergeRequests.First.Value;
					_mergeRequests.RemoveFirst();

					ExecuteCoalitionMerge(request);
					Cancel.ThrowIfCancellationRequested();
				}
			}

			/// <summary>
			/// Waits for a pending merge (if any) to complete,
			/// while at the same time avoiding deadlocks due to cyclic merge requests.
			/// </summary>
			public async Task WaitForMergeCompletion()
			{
				if (_pendingCoalitionMerge == null || _pendingCoalitionMerge.Task.IsCompleted)
					return;

				while (!_pendingCoalitionMerge.Task.IsCompleted)
				{
					// wait for some time, then do the deadlock check
					await System.Threading.Tasks.Task.Yield();

					// detect deadlock
					if (!_mergeRequests.Any(request => request.OpposingLeader == _awaitingRendezvousFrom))
						continue;

					// determine which agent has to break the deadlock
					var deadlockBreaker = DetermineLeader(_coalition.Leader, _awaitingRendezvousFrom);
					if (deadlockBreaker != _coalition.Leader)
						continue;

					// break the deadlock
					_awaitingRendezvousFrom = null;
					_pendingCoalitionMerge = null;

					var deadlockRequest = _mergeRequests.FirstOrDefault(request => request.OpposingLeader == _awaitingRendezvousFrom);
					_mergeRequests.Remove(deadlockRequest);
					ExecuteCoalitionMerge(deadlockRequest);
					Cancel.ThrowIfCancellationRequested(); // The coalition might have been disbanded

					break;
				}
			}

			/// <summary>
			/// Called by members to notify the coalition they have been invited by another coalition.
			/// </summary>
			/// <param name="source">The agent that invited a member, i.e. the leader of the other coalition.</param>
			public void MergeCoalition(CoalitionReconfigurationAgent source)
			{
				_mergeRequests.AddLast(new MergeRequest { OpposingLeader = source, OriginalLeader = _coalition.Leader });
			}

			/// <summary>
			/// Notifies the coalition an invited agent already belongs to a different coalition,
			/// and that it will receive a <see cref="RendezvousRequest(Coalition, CoalitionReconfigurationAgent)"/> from
			/// the opposing <paramref name="leader"/>.
			/// </summary>
			public void AwaitRendezvous(CoalitionReconfigurationAgent invitedAgent, CoalitionReconfigurationAgent leader)
			{
				_awaitingRendezvousFrom = leader;
				_pendingCoalitionMerge = new TaskCompletionSource<object>();

				_coalition.ReceiveInvitationResponse(invitedAgent);
				_coalition.CancelInvitations();
			}

			/// <summary>
			/// Informs the coalition of a merge with another coalition.
			/// </summary>
			/// <param name="chosenCoalition">The coalition which will take over, i.e., either this instance or the other coalition.</param>
			/// <param name="inNameOf">The agent who lead the other coalition when the <see cref="AwaitRendezvous(CoalitionReconfigurationAgent)"/>
			/// message was sent.</param>
			private void RendezvousRequest(Coalition chosenCoalition, CoalitionReconfigurationAgent inNameOf)
			{
				Debug.Assert(_awaitingRendezvousFrom == inNameOf,
					$"Awaiting rendezvous from agent #{_awaitingRendezvousFrom.BaseAgent.ID}, but received from agent #{inNameOf.BaseAgent.ID}.");
				_awaitingRendezvousFrom = null;

				if (chosenCoalition == _coalition)
				{
					_pendingCoalitionMerge.SetResult(null);
					_pendingCoalitionMerge = null;
					return;
				}

				// actual merge
				foreach (var member in _coalition.Members)
					chosenCoalition.Join(member);
				chosenCoalition.Merger.ReceiveCoalitionInformation(_mergeRequests, _coalition.ViolatedPredicates);

				// stop controller from continuing
				_pendingCoalitionMerge.SetResult(null);
				_pendingCoalitionMerge = null;
				_cancellation.Cancel(throwOnFirstException: true);
			}

			/// <summary>
			/// Passes meta-information from a coalition that is merged into this instance.
			/// </summary>
			private void ReceiveCoalitionInformation(IEnumerable<MergeRequest> mergeRequests, InvariantPredicate[] violatedPredicates)
			{
				foreach (var request in mergeRequests)
					_mergeRequests.AddLast(request);
				_coalition.ViolatedPredicates = _coalition.ViolatedPredicates.Concat(violatedPredicates).ToArray();
			}

			/// <summary>
			/// Executes the merge of two coalitions.
			/// </summary>
			/// <param name="request">The <see cref="MergeRequest"/> being executed.</param>
			private void ExecuteCoalitionMerge(MergeRequest request)
			{
				if (request.OpposingLeader.CurrentCoalition != _coalition)
				{
					var leader = DetermineLeader(_coalition.Leader, request.OpposingLeader);
					request.OpposingMerger.RendezvousRequest(leader.CurrentCoalition, inNameOf: request.OriginalLeader);
				}
			}

			/// <summary>
			/// Given two coalition leaders, determines which one will lead the merged coalition.
			/// </summary>
			private static CoalitionReconfigurationAgent DetermineLeader(CoalitionReconfigurationAgent leader1, CoalitionReconfigurationAgent leader2)
			{
				if (leader1.BaseAgent.ID < leader2.BaseAgent.ID)
					return leader1;
				return leader2;
			}

			/// <summary>
			/// Represents the need to merge two coalitions.
			/// </summary>
			private struct MergeRequest
			{
				/// <summary>
				/// Leader of the coalition that shall be merged.
				/// </summary>
				public CoalitionReconfigurationAgent OpposingLeader { get; set; }

				public Coalition OpposingCoalition => OpposingLeader.CurrentCoalition;

				public MergeSupervisor OpposingMerger => OpposingCoalition.Merger;

				/// <summary>
				/// The original leader of the coalition that first created the request.
				/// </summary>
				public CoalitionReconfigurationAgent OriginalLeader { get; set; }
			}
		}
	}
}

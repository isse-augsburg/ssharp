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
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	partial class CoalitionFormationController
	{
		/// <summary>
		/// A helper class for ordering agents known to a coalition in the order they should be recruited:
		/// alternating inputs and outputs of members, preferring agents known to participate in the task.
		/// </summary>
		private class AgentQueue : IEnumerable<BaseAgent>
		{
			private readonly HashSet<BaseAgent> _knownParticipants;
			private readonly LinkedList<BaseAgent> _inputQueue = new LinkedList<BaseAgent>();
			private readonly LinkedList<BaseAgent> _outputQueue = new LinkedList<BaseAgent>();

			private LinkedListNode<BaseAgent> _firstInputParticipant;
			private LinkedListNode<BaseAgent> _firstOutputParticipant;

			private readonly Coalition _coalition;

			public AgentQueue(Coalition coalition)
			{
				_coalition = coalition;

				var baseAgents = coalition.Members.Select(reconfAgent => reconfAgent.BaseAgent);
				_knownParticipants = new HashSet<BaseAgent>(baseAgents.SelectMany(GetNeighbouringParticipants));
				_inputQueue = new LinkedList<BaseAgent>(baseAgents.SelectMany(agent => agent.Inputs));
				_outputQueue = new LinkedList<BaseAgent>(baseAgents.SelectMany(agent => agent.Inputs));

				_firstInputParticipant = FindSubsequentParticipant(_inputQueue.First);
				_firstOutputParticipant = FindSubsequentParticipant(_outputQueue.First);
			}

			private IEnumerable<BaseAgent> GetNeighbouringParticipants(BaseAgent agent)
			{
				return agent.AllocatedRoles
					.Where(role => role.Task == _coalition.Task)
					.SelectMany(role => new[] { role.PreCondition.Port, role.PostCondition.Port })
					.Distinct()
					.Where(participant => participant != null);
			}

			// returns the first participant following startingPoint, or null if none found (or if startingPoint == null)
			private LinkedListNode<BaseAgent> FindSubsequentParticipant(LinkedListNode<BaseAgent> startingPoint)
			{
				if (startingPoint == null)
					return null;

				var current = startingPoint;
				do
				{
					current = current.Next;
				} while (current != null && !_knownParticipants.Contains(current.Value));
				return current;
			}

			// returns the first participant preceeding endPoint, or endPoint if none found
			private LinkedListNode<BaseAgent> FindPreviousParticipant(LinkedList<BaseAgent> list, LinkedListNode<BaseAgent> endPoint)
			{
				var current = list.First;

				while (current != null && current != endPoint && !_knownParticipants.Contains(current.Value))
					current = current.Next;

				return current;
			}

			public IEnumerator<BaseAgent> GetEnumerator()
			{
				while (_inputQueue.Count > 0 || _outputQueue.Count > 0)
				{
					if (_outputQueue.Count > 0)
					{
						var next = GetNext(_outputQueue, _inputQueue, ref _firstOutputParticipant);
						if (next != null)
							yield return next;
					}

					if (_inputQueue.Count > 0)
					{
						var next = GetNext(_inputQueue, _outputQueue, ref _firstInputParticipant);
						if (next != null)
							yield return next;
					}
				}
			}

			private BaseAgent GetNext(LinkedList<BaseAgent> source, LinkedList<BaseAgent> other, ref LinkedListNode<BaseAgent> firstParticipant)
			{
				LinkedListNode<BaseAgent> next;

				do
				{
					if (source.Count == 0)
						return null;

					if (firstParticipant == null)
						next = source.First;
					else
					{
						next = firstParticipant;
						firstParticipant = FindSubsequentParticipant(next);
					}

					source.Remove(next);
					other.Remove(next.Value);
				} while (_coalition.Contains(next.Value)); // lazily filter members (e.g. returned previously and invited)

				foreach (var input in next.Value.Inputs)
					_inputQueue.AddLast(input);

				foreach (var output in next.Value.Outputs)
					_outputQueue.AddLast(output);

				_knownParticipants.UnionWith(GetNeighbouringParticipants(next.Value));

				// update first participants based on new information about known participants
				_firstInputParticipant = FindPreviousParticipant(_inputQueue, _firstInputParticipant);
				_firstOutputParticipant = FindPreviousParticipant(_outputQueue, _firstOutputParticipant);

				return next.Value;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
	}
}

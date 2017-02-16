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

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutableModel;
	using Modeling;
	using Utilities;


	/// <summary>
	///   Removes all candidate transition that activate one or more faults in an incorrect order.
	/// </summary>
	internal sealed unsafe class FaultOrderModifier<TExecutableModel> : ITransitionModifier<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly Fault _firstFault;
		private readonly bool _forceSimultaneous;
		private readonly Fault _secondFault;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="firstFault">The fault that is expected to be activated first.</param>
		/// <param name="secondFault">The fault that is expected to be activated subsequently.</param>
		/// <param name="forceSimultaneous">Indicates whether both faults must occur simultaneously.</param>
		public FaultOrderModifier(Fault firstFault, Fault secondFault, bool forceSimultaneous)
		{
			_firstFault = firstFault;
			_secondFault = secondFault;
			_forceSimultaneous = forceSimultaneous;
		}

		/// <summary>
		///   Optionally modifies the <paramref name="transitions" />, changing any of their values. However, no new transitions can be
		///   added; transitions can be removed by setting their <see cref="CandidateTransition.IsValid" /> flag to <c>false</c>.
		///   During subsequent traversal steps, only valid transitions and target states reached by at least one valid transition
		///   are considered.
		/// </summary>
		/// <param name="context">The context of the model traversal.</param>
		/// <param name="worker">The worker that found the transition.</param>
		/// <param name="transitions">The transitions that should be checked.</param>
		/// <param name="sourceState">The source state of the transitions.</param>
		/// <param name="sourceStateIndex">The unique index of the transition's source state.</param>
		/// <param name="isInitial">Indicates whether the transitions are initial transitions not starting in any valid source state.</param>
		public void ModifyTransitions(TraversalContext<TExecutableModel> context, Worker<TExecutableModel> worker, TransitionCollection transitions, byte* sourceState,
									  int sourceStateIndex, bool isInitial)
		{
			// The fault order state is encoded into the first four bytes of the state vector (must be four bytes as required by 
			// RuntimeModel's state header field)
			var state = isInitial ? State.NeitherFaultActivated : *(State*)sourceState;

			foreach (CandidateTransition* transition in transitions)
			{
				var activatedFaults = transition->ActivatedFaults;
				var isValid = true;
				var nextState = state;

				switch (state)
				{
					case State.NeitherFaultActivated:
						if (activatedFaults.Contains(_firstFault) && activatedFaults.Contains(_secondFault))
						{
							if (_forceSimultaneous)
								nextState = State.BothFaultsActivated;
							else
								isValid = false;
						}
						else if (activatedFaults.Contains(_firstFault))
						{
							if (_forceSimultaneous)
								isValid = false;
							else
								nextState = State.FirstFaultActivated;
						}
						else if (activatedFaults.Contains(_secondFault))
							isValid = false;
						break;
					case State.FirstFaultActivated:
						if (activatedFaults.Contains(_secondFault))
							nextState = State.BothFaultsActivated;
						break;
					case State.BothFaultsActivated:
						break;
					default:
						Assert.NotReached("Unexpected state value.");
						break;
				}

				transition->IsValid = isValid;
				*(State*)transition->TargetState = nextState;
			}
		}

		/// <summary>
		///   Describes the fault ordering information encoded into the traversed graph.
		/// </summary>
		private enum State
		{
			/// <summary>
			///   Indicates that no fault have been activated so far.
			/// </summary>
			NeitherFaultActivated,

			/// <summary>
			///   Indicates that only the first fault has been activated so far.
			/// </summary>
			FirstFaultActivated,

			/// <summary>
			///   Indicates that both faults have been activated so far.
			/// </summary>
			BothFaultsActivated,
		}
	}
}
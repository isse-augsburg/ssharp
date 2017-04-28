// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

using System;

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Collections.Concurrent;
	using System.Diagnostics;
	using System.Globalization;
	using Modeling;
	using Utilities;
	using AnalysisModel;
	using ExecutableModel;


	internal unsafe partial class LabeledTransitionMarkovDecisionProcess : DisposableObject
	{
		public static readonly int TransitionSize = sizeof(TransitionTargetElement);
		private const int AvgGraphNodesPerSucceedingState = 99;

		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;
		
		private int _indexOfFirstInitialDistribution = -1;

		public ConcurrentBag<int> SourceStates { get; } = new ConcurrentBag<int>();

		private readonly MemoryBuffer _stateStorageStateToRootOfContinuationGraphBuffer = new MemoryBuffer();
		private readonly long* _stateStorageStateToRootOfContinuationGraphMemory;
		
		private readonly MemoryBuffer _continuationGraphBuffer = new MemoryBuffer();
		private readonly ContinuationGraphElement* _continuationGraph;
		private long _continuationGraphElementCount = 0;

		private readonly long _maxNumberOfContinuationGraphElements;

		private readonly MemoryBuffer _transitionTargetBuffer = new MemoryBuffer();
		private readonly TransitionTargetElement* _transitionTarget;
		private int _transitionTargetCount = 0;

		private readonly long _maxNumberOfTransitionTargets;


		public LabeledTransitionMarkovDecisionProcess(long maxNumberOfStates, long maxNumberOfTransitions)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);

			_maxNumberOfTransitionTargets = maxNumberOfTransitions/(AvgGraphNodesPerSucceedingState+1);
			_maxNumberOfContinuationGraphElements = _maxNumberOfTransitionTargets * AvgGraphNodesPerSucceedingState;

			Requires.InRange(_maxNumberOfTransitionTargets, nameof(_maxNumberOfTransitionTargets), 1024, Int32.MaxValue - 1);
			Requires.InRange(_maxNumberOfContinuationGraphElements, nameof(_maxNumberOfContinuationGraphElements), 1024, Int32.MaxValue - 1);

			_stateStorageStateToRootOfContinuationGraphBuffer.Resize((long)maxNumberOfStates * sizeof(long), zeroMemory: false);
			_stateStorageStateToRootOfContinuationGraphMemory = (long*)_stateStorageStateToRootOfContinuationGraphBuffer.Pointer;

			_continuationGraphBuffer.Resize((long)_maxNumberOfContinuationGraphElements * sizeof(ContinuationGraphElement), zeroMemory: false);
			_continuationGraph = (ContinuationGraphElement*)_continuationGraphBuffer.Pointer;

			_transitionTargetBuffer.Resize((long)_maxNumberOfTransitionTargets * sizeof(TransitionTargetElement), zeroMemory: false);
			_transitionTarget = (TransitionTargetElement*)_transitionTargetBuffer.Pointer;
			
			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_stateStorageStateToRootOfContinuationGraphMemory, maxNumberOfStates);
		}
		
		private struct ContinuationGraphElement
		{
			public LtmdpChoiceType ChoiceType;
			public int From;
			public int To;
		}

		private struct TransitionTargetElement
		{
			public int TargetState;
			public StateFormulaSet Formulas;
			public double Probability;
		}

		private long GetPlaceForNewContinuationGraphElements(int number)
		{
			var locationOfFirstNewEntry = InterlockedExtensions.IncrementReturnOld(ref _continuationGraphElementCount);
			if (locationOfFirstNewEntry >= _maxNumberOfContinuationGraphElements)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			return locationOfFirstNewEntry;
		}

		private int GetPlaceForNewTransitionTargetElement()
		{
			var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _transitionTargetCount);
			if (locationOfNewEntry >= _maxNumberOfTransitionTargets)
				throw new OutOfMemoryException("Unable to store distribution.");
			return locationOfNewEntry;
		}

		public void CreateStutteringState(int stutteringStateIndex)
		{
			// The stuttering state might not be reached at all.
			// Make sure, that all used algorithms to not require a connected state graph.
			var currentElementIndex = _stateStorageStateToRootOfContinuationGraphMemory[stutteringStateIndex];
			Assert.That(currentElementIndex == -1, "Stuttering state has already been created");

			var locationOfNewContinuationGraphElement = GetPlaceForNewContinuationGraphElements(1);
			var locationOfNewTransitionTargetElement = GetPlaceForNewTransitionTargetElement();

			_continuationGraph[locationOfNewContinuationGraphElement] =
					new ContinuationGraphElement
					{
						ChoiceType=LtmdpChoiceType.UnsplitOrFinal,
						To = locationOfNewTransitionTargetElement
					};

			_transitionTarget[locationOfNewTransitionTargetElement] =
					new TransitionTargetElement
					{
						Formulas = new StateFormulaSet(),
						Probability = 1.0,
						TargetState = stutteringStateIndex
					};

			SourceStates.Add(stutteringStateIndex);
			_stateStorageStateToRootOfContinuationGraphMemory[stutteringStateIndex] = locationOfNewContinuationGraphElement;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_stateStorageStateToRootOfContinuationGraphBuffer.SafeDispose();
			_continuationGraphBuffer.SafeDispose();
			_transitionTargetBuffer.SafeDispose();
		}
	}
}

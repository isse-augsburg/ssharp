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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Linq.Expressions;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using ExecutableModel;
	using Formula;
	using Modeling;
	using Utilities;
	
	internal sealed unsafe class LtmcRetraverseTransitionSetBuilder : DisposableObject
	{
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _targetStateBuffer = new MemoryBuffer();
		private readonly byte* _targetStateMemory;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;
		private int _transitionCount;
		private int _stateCount;
		private readonly long _capacity;

		public LtmcRetraverseTransitionSetBuilder(int stateVectorSize, long capacity)
		{
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");
			_stateVectorSize = stateVectorSize;
			_capacity = capacity;

			_transitionBuffer.Resize(capacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;

			_targetStateBuffer.Resize(capacity * _stateVectorSize, zeroMemory: false);
			_targetStateMemory = _targetStateBuffer.Pointer;
		}

		private byte* AddState(int originalState, int enrichments)
		{
			// Try to find a matching state. If not found, then add a new one
			byte* targetState;
			int* targetStateAsInt;
			for (var i = 0; i < _stateCount; i++)
			{
				targetState = _targetStateMemory + i * _stateVectorSize;
				targetStateAsInt = (int*)targetState;
				if (targetStateAsInt[0] == originalState && targetStateAsInt[1] == enrichments)
				{
					return targetState;
				}
			}
			Requires.That(_stateCount < _capacity, "more space needed");

			targetState = _targetStateMemory + _stateCount * _stateVectorSize;
			targetStateAsInt = (int*)targetState;
			// create new state
			targetStateAsInt[0] = originalState;
			targetStateAsInt[1] = enrichments;
			++_stateCount;
			return targetState;
		}
		
		public void AddTransition(int targetState, int enrichments, double probability, StateFormulaSet formulas)
		{
			// Try to find a matching transition. If not found, then add a new one
			var state = AddState(targetState, enrichments);

			for (var i = 0; i < _transitionCount; i++)
			{
				var candidateTransition = _transitions[i];
				if (candidateTransition.TargetStatePointer==state &&
					candidateTransition.Formulas == formulas)
				{
					candidateTransition.Probability += probability;
					_transitions[i] = candidateTransition;
					return;
				}
			}

			Requires.That(_transitionCount < _capacity,"more space needed");
			
			_transitions[_transitionCount] = new LtmcTransition
			{
				TargetStatePointer = state,
				Formulas = formulas,
				ActivatedFaults = new FaultSet(),
				Flags = TransitionFlags.IsValidFlag,
				Probability = probability
			};
			++_transitionCount;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			_transitionCount = 0;
			_stateCount = 0;
		}
		
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_transitionBuffer.SafeDispose();
			_targetStateBuffer.SafeDispose();
		}
		
		public TransitionCollection ToCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmcTransition));
		}
	}

	internal unsafe class LtmcRetraverseModel : AnalysisModel
	{
		private Formula[] _formulas;

		private Func<IntPtr, bool>[] formulaInStateTransitionEvaluator;

		private Func<bool>[] formulaInInitialTransitionEvaluator;

		private readonly LtmcRetraverseTransitionSetBuilder _transitions;

		public LabeledTransitionMarkovChain LabeledTransitionMarkovChain { get; }

		public LtmcRetraverseModel(LabeledTransitionMarkovChain ltmc, AnalysisConfiguration configuration)
		{
			LabeledTransitionMarkovChain = ltmc;
			_transitions = new LtmcRetraverseTransitionSetBuilder(InternalStateVectorSize, configuration.SuccessorCapacity);
		}

		public void AddFormula(Formula formula)
		{
			if (formula.IsStateFormula())
			{
				_formulas = _formulas.Concat(new [] { formula } ).ToArray();
			}
		}

		protected override void OnDisposing(bool disposing)
		{
		}

		private const int InternalStateVectorSize = sizeof(int) + sizeof(int); //state is a tuple of (origin-state,enrichments)
		public override int StateVectorSize { get; } = InternalStateVectorSize;
		public override int TransitionSize { get; } = LabeledTransitionMarkovChain.TransitionSize;
		public override Formula[] Formulas => _formulas;

		private TransitionCollection ConvertTransitions(LabeledTransitionMarkovChain.LabeledTransitionEnumerator enumerator)
		{
			_transitions.Clear();
			while (enumerator.MoveNext())
			{
				var enrichment = 0;
				var formulas = new StateFormulaSet();
				_transitions.AddTransition(enumerator.CurrentTargetState, enrichment, enumerator.CurrentProbability, formulas);
			}
			return _transitions.ToCollection();
		}

		public override TransitionCollection GetInitialTransitions()
		{
			var enumerator = LabeledTransitionMarkovChain.GetInitialDistributionEnumerator();
			return ConvertTransitions(enumerator);
		}

		public override TransitionCollection GetSuccessorTransitions(byte* state)
		{
			var stateAsInt = (int*)state;
			var originalState= stateAsInt[0];
			var enumerator = LabeledTransitionMarkovChain.GetTransitionEnumerator(originalState);
			return ConvertTransitions(enumerator);
		}

		public override void Reset()
		{
			_transitions.Clear();
		}

		public override CounterExample CreateCounterExample(byte[][] path, bool endsWithException)
		{
			throw new NotImplementedException();
		}
	}
}

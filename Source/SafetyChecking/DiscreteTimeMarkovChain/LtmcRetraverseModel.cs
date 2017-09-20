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
	using AnalysisModelTraverser;
	using ExecutableModel;
	using Formula;
	using Modeling;
	using Utilities;
	
	internal sealed unsafe class LtmcRetraverseTransitionSetBuilder : DisposableObject
	{
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;
		private int _transitionCount;
		private readonly long _capacity;

		private readonly TemporaryStateStorage _temporalStateStorage;

		public LtmcRetraverseTransitionSetBuilder(TemporaryStateStorage temporalStateStorage, long capacity)
		{
			Requires.That(capacity <= (1 << 30), nameof(capacity), $"Maximum supported capacity is {1 << 30}.");

			_capacity = capacity;

			_transitionBuffer.Resize(capacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;
			
			_temporalStateStorage = temporalStateStorage;
		}

		private byte* AddState(int originalState)
		{
			// Try to find a matching state. If not found, then add a new one
			byte* targetState;

			int* stateToFind = stackalloc int[1];
			stateToFind[0] = originalState;
			
			if (_temporalStateStorage.TryToFindState((byte*) stateToFind, out targetState))
				return targetState;
			
			targetState = _temporalStateStorage.GetFreeTemporalSpaceAddress();
			var targetStateAsInt = (int*)targetState;
			// create new state
			targetStateAsInt[0] = originalState;
			return targetState;
		}
		
		public void AddTransition(int targetState, double probability, StateFormulaSet formulas)
		{
			// Try to find a matching transition. If not found, then add a new one
			var state = AddState(targetState);

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
		}
		
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_transitionBuffer.SafeDispose();
		}
		
		public TransitionCollection ToCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmcTransition));
		}
	}

	internal unsafe class LtmcRetraverseModel : AnalysisModel
	{
		private const int InternalStateVectorSize = sizeof(int);

		public sealed override int ModelStateVectorSize { get; } = InternalStateVectorSize;

		public override int TransitionSize { get; } = LabeledTransitionMarkovChain.TransitionSize;

		public override Formula[] Formulas => _formulas;

		private readonly Formula[] _formulas;
		
		private readonly Func<StateFormulaSet, bool>[] _formulaInStateTransitionEvaluators;

		protected readonly TemporaryStateStorage TemporaryStateStorage;
		
		private readonly LtmcRetraverseTransitionSetBuilder _transitions;

		public LabeledTransitionMarkovChain LabeledTransitionMarkovChain { get; }
		
		public LtmcRetraverseModel(LabeledTransitionMarkovChain ltmc, Formula[] stateFormulasToCheck, AnalysisConfiguration configuration)
		{
			Assert.That(stateFormulasToCheck.Length <= 32, "Too many Formulas");

			LabeledTransitionMarkovChain = ltmc;
			TemporaryStateStorage = new TemporaryStateStorage(ModelStateVectorSize, configuration.SuccessorCapacity);
			_transitions = new LtmcRetraverseTransitionSetBuilder(TemporaryStateStorage, configuration.SuccessorCapacity);
			_formulas = stateFormulasToCheck;

			_formulaInStateTransitionEvaluators = stateFormulasToCheck.Select(CreateFormulaEvaluator).ToArray();
		}
		
		private Func<StateFormulaSet, bool> CreateFormulaEvaluator(Formula formula)
		{
			return StateFormulaSetEvaluatorCompilationVisitor.Compile(LabeledTransitionMarkovChain.StateFormulaLabels, formula);
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (disposing)
			{
				_transitions.SafeDispose();
				TemporaryStateStorage.SafeDispose();
			}
		}


		private StateFormulaSet DeriveNewStateFormulaSet(StateFormulaSet oldStateFormulaSet)
		{
			var evaluatedStateFormulas = new bool[_formulas.Length];
			for (var i = 0; i < _formulas.Length; i++)
			{
				var setNewBit = _formulaInStateTransitionEvaluators[i](oldStateFormulaSet);
				evaluatedStateFormulas[i] = setNewBit;
			}
			return new StateFormulaSet(evaluatedStateFormulas);
		}

		private TransitionCollection ConvertTransitions(LabeledTransitionMarkovChain.LabeledTransitionEnumerator enumerator)
		{
			TemporaryStateStorage.Clear();
			_transitions.Clear();
			while (enumerator.MoveNext())
			{
				var targetStateFormulaSet = DeriveNewStateFormulaSet(enumerator.CurrentFormulas);
				_transitions.AddTransition(enumerator.CurrentTargetState, enumerator.CurrentProbability, targetStateFormulaSet);
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
			var originalState = stateAsInt[0];
			var enumerator = LabeledTransitionMarkovChain.GetTransitionEnumerator(originalState);
			return ConvertTransitions(enumerator);
		}

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		/// <param name="traversalModifierStateVectorSize">Extra bytes in state vector for traversal parameters.</param>
		public sealed override void Reset(int traversalModifierStateVectorSize)
		{
			TemporaryStateStorage.Reset(traversalModifierStateVectorSize);
		}

		public override CounterExample CreateCounterExample(byte[][] path, bool endsWithException)
		{
			throw new NotImplementedException();
		}
	}
}

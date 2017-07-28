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

		private byte* AddState(int originalState, int targetEnrichments)
		{
			// Try to find a matching state. If not found, then add a new one
			byte* targetState;
			int* targetStateAsInt;
			for (var i = 0; i < _stateCount; i++)
			{
				targetState = _targetStateMemory + i * _stateVectorSize;
				targetStateAsInt = (int*)targetState;
				if (targetStateAsInt[0] == originalState && targetStateAsInt[1] == targetEnrichments)
				{
					return targetState;
				}
			}
			Requires.That(_stateCount < _capacity, "more space needed");

			targetState = _targetStateMemory + _stateCount * _stateVectorSize;
			targetStateAsInt = (int*)targetState;
			// create new state
			targetStateAsInt[0] = originalState;
			targetStateAsInt[1] = targetEnrichments;
			++_stateCount;
			return targetState;
		}
		
		public void AddTransition(int targetState, int targetEnrichments, double probability, StateFormulaSet formulas)
		{
			// Try to find a matching transition. If not found, then add a new one
			var state = AddState(targetState, targetEnrichments);

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

		// Formula evaluator needs to know the labeling on the transition and the enrichment the _current_ state.
		// For an initial transition, the enrichment 0 should be provided
		private Func<StateFormulaSet, int, bool>[] _formulaInStateTransitionEvaluators;

		private readonly LtmcRetraverseTransitionSetBuilder _transitions;

		public LabeledTransitionMarkovChain LabeledTransitionMarkovChain { get; }
		
		private Func<StateFormulaSet, bool>[] _enrichmentEvaluators = new Func<StateFormulaSet, bool>[0];

		public LtmcRetraverseModel(LabeledTransitionMarkovChain ltmc, AnalysisConfiguration configuration)
		{
			LabeledTransitionMarkovChain = ltmc;
			_transitions = new LtmcRetraverseTransitionSetBuilder(InternalStateVectorSize, configuration.SuccessorCapacity);
		}

		private readonly List<Formula> _collectedStateFormulas = new List<Formula>();
		private readonly List<UnaryFormula> _collectedOnceFormulas = new List<UnaryFormula>();

		private void AddFormula(Formula formula)
		{
			var alreadyCompilableCollector = new CollectMaximalCompilableFormulasVisitor();
			alreadyCompilableCollector.VisitNewTopLevelFormula(formula);
			var alreadyCompilableFormulas = alreadyCompilableCollector.CollectedStateFormulas;
			foreach (var collectedStateFormula in alreadyCompilableFormulas)
			{
				if (!_collectedStateFormulas.Contains(collectedStateFormula))
					_collectedStateFormulas.Add(collectedStateFormula);
			}

			var onceFormulaCollector = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			onceFormulaCollector.VisitNewTopLevelFormula(formula);
			var onceFormulas = onceFormulaCollector.DeepestOnceFormulasWithCompilableOperand;
			foreach (var onceFormula in onceFormulas)
			{
				Assert.That(onceFormula.Operator == UnaryOperator.Once, "operator of OnceFormula must be Once");
				Assert.That(alreadyCompilableFormulas.Contains(onceFormula.Operand),"operand of OnceFormula should already been included");
				if (!_collectedOnceFormulas.Contains(onceFormula))
					_collectedOnceFormulas.Add(onceFormula);
			}
		}

		public void AddFormulas(IEnumerable<Formula> formulas)
		{
			foreach (var formula in formulas)
			{
				AddFormula(formula);
			}

			if (_collectedStateFormulas.Count + _collectedOnceFormulas.Count > 32)
			{
				throw new Exception("Too many Formulas");
			}

			// EmbedObserversIntoModel

			var newFormulas = new List<Formula>();
			var newFormulaEvaluators = new List<Func<StateFormulaSet, int, bool>>();
			
			foreach (var collectedStateFormula in _collectedStateFormulas)
			{
				newFormulas.Add(collectedStateFormula);
				var oldEvaluator = StateFormulaSetEvaluatorCompilationVisitor.Compile(LabeledTransitionMarkovChain.StateFormulaLabels, collectedStateFormula);
				Func<StateFormulaSet,int,bool> newFormulaEvaluator = (oldTargetStateFormulaSet, targetEnrichment) => oldEvaluator(oldTargetStateFormulaSet);
				newFormulaEvaluators.Add(newFormulaEvaluator);
			}

			foreach (var onceFormula in _collectedOnceFormulas)
			{
				// onceFormulas have a compilable operand. Thus, we can directly rely on them
				var operand = onceFormula.Operand;
				var enrichmentEvaluator =
					StateFormulaSetEvaluatorCompilationVisitor.Compile(LabeledTransitionMarkovChain.StateFormulaLabels, operand);
				var indexOfEnrichmentEvaluator = _enrichmentEvaluators.Length;
				_enrichmentEvaluators = _enrichmentEvaluators.Concat(new[] { enrichmentEvaluator }).ToArray();
				
				newFormulas.Add(onceFormula);
				Func<StateFormulaSet, int, bool> newFormulaEvaluator = (oldTargetStateFormulaSet, targetEnrichment) => (targetEnrichment & (1 << indexOfEnrichmentEvaluator)) != 0;
				newFormulaEvaluators.Add(newFormulaEvaluator);
			}

			_formulas = newFormulas.ToArray();
			_formulaInStateTransitionEvaluators = newFormulaEvaluators.ToArray();
		}

		protected override void OnDisposing(bool disposing)
		{
		}

		private const int InternalStateVectorSize = sizeof(int) + sizeof(int); //state is a tuple of (origin-state,enrichments)
		public override int StateVectorSize { get; } = InternalStateVectorSize;
		public override int TransitionSize { get; } = LabeledTransitionMarkovChain.TransitionSize;
		public override Formula[] Formulas => _formulas;

		private int DeriveNewEnrichment(StateFormulaSet oldStateFormulaSet, int oldEnrichments)
		{
			int newEnrichment = oldEnrichments;
			for (var i = 0; i < _enrichmentEvaluators.Length; i++)
			{
				var isBitAlreadySet = (newEnrichment & (1 << i)) != 0;
				if (!isBitAlreadySet)
				{
					var setNewBit = _enrichmentEvaluators[i](oldStateFormulaSet);
					if (setNewBit)
					{
						newEnrichment |= 1 << i;
					}
				}
			}
			return newEnrichment;
		}

		private StateFormulaSet DeriveNewStateFormulaSet(StateFormulaSet oldStateFormulaSet, int newEnrichment)
		{
			var evaluatedStateFormulas = new bool[_formulas.Length];
			for (var i = 0; i < _formulas.Length; i++)
			{
				var setNewBit = _formulaInStateTransitionEvaluators[i](oldStateFormulaSet,newEnrichment);
				evaluatedStateFormulas[i] = setNewBit;
			}
			return new StateFormulaSet(evaluatedStateFormulas);
		}

		private TransitionCollection ConvertTransitions(int currentEnrichment, LabeledTransitionMarkovChain.LabeledTransitionEnumerator enumerator)
		{
			_transitions.Clear();
			while (enumerator.MoveNext())
			{
				var targetEnrichment= DeriveNewEnrichment(enumerator.CurrentFormulas,currentEnrichment);
				var targetStateFormulaSet = DeriveNewStateFormulaSet(enumerator.CurrentFormulas, targetEnrichment);
				_transitions.AddTransition(enumerator.CurrentTargetState, targetEnrichment, enumerator.CurrentProbability, targetStateFormulaSet);
			}
			return _transitions.ToCollection();
		}

		public override TransitionCollection GetInitialTransitions()
		{
			var enumerator = LabeledTransitionMarkovChain.GetInitialDistributionEnumerator();
			var currentEnrichment = 0;
			return ConvertTransitions(currentEnrichment,enumerator);
		}

		public override TransitionCollection GetSuccessorTransitions(byte* state)
		{
			var stateAsInt = (int*)state;
			var originalState = stateAsInt[0];
			var currentEnrichment = stateAsInt[1];
			var enumerator = LabeledTransitionMarkovChain.GetTransitionEnumerator(originalState);
			return ConvertTransitions(currentEnrichment,enumerator);
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

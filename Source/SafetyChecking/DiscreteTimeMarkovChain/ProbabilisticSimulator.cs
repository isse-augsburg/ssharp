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
	using System.Runtime.InteropServices;
	using AnalysisModel;
	using ExecutableModel;
	using ExecutedModel;
	using Formula;
	using Modeling;
	using Utilities;

	public class SimulationTraceStep
	{
		public byte[] State;
		public int[] Choices;
		public StateFormulaSet EvaluatedNormalizedFormulas;
	}

	
	/// <summary>
	///   Simulates a S# model for debugging or testing purposes.
	/// </summary>
	public unsafe class ProbabilisticSimulator<TExecutableModel> : DisposableObject where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		public readonly TExecutableModel RuntimeModel;
		
		private ProbabilisticSimulatorChoiceResolver _choiceResolver;

		public Formula[] NormalizedFormulas { get; private set; }
		public string[] NormalizedFormulaLabels { get; private set; }
		public int[] NormalizedFormulaSatisfactionCount { get; private set; }

		private readonly List<SimulationTraceStep> _simulationTrace = new List<SimulationTraceStep>();

		public IEnumerable<SimulationTraceStep> SimulationTrace => _simulationTrace;

		private readonly Func<bool>[] _compiledNormalizedFormulas;

		private readonly bool _activateIndependentFaultsAtStepBeginning;
		private readonly bool _allowFaultsOnInitialTransitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public ProbabilisticSimulator(ExecutableModelCreator<TExecutableModel> modelCreator, IEnumerable<Formula> formulas, AnalysisConfiguration configuration)
		{
			// TODO: Set Mode WithCustomSeed (each thread must have its own seed) or RealRandom
			Requires.NotNull(modelCreator, nameof(modelCreator));
			
			RuntimeModel = modelCreator.CreateCoupledModelCreator(formulas.ToArray()).Create(0);

			NormalizedFormulas = RuntimeModel.Formulas.ToArray();
			NormalizedFormulaLabels = NormalizedFormulas.Select(stateFormula => stateFormula.Label).ToArray();
			NormalizedFormulaSatisfactionCount = new int[NormalizedFormulas.Length];
			_compiledNormalizedFormulas = NormalizedFormulas.Select(formula => FormulaCompilationVisitor<TExecutableModel>.Compile(RuntimeModel, formula)).ToArray();
			
			_allowFaultsOnInitialTransitions = configuration.AllowFaultsOnInitialTransitions;

			_activateIndependentFaultsAtStepBeginning =
				configuration.MomentOfIndependentFaultActivation == MomentOfIndependentFaultActivation.AtStepBeginning;
		}
		
		/// <summary>
		///   Runs a number of simulation steps.
		/// </summary>
		public void SimulateSteps(int steps)
		{
			ResetTrace();

			ResetStep();
			SimulateInitialStep();
			UpdateTraceProbability();
			AddCurrentSituationToTrace();

			for (var i = 0; i < steps; i++)
			{
				ResetStep();
				SimulateStep();
				UpdateTraceProbability();
				AddCurrentSituationToTrace();
			}
		}

		private void SimulateInitialStep()
		{
			foreach (var fault in RuntimeModel.NondeterministicFaults)
				fault.Reset();

			var savedActivations = RuntimeModel.NondeterministicFaults.ToDictionary(fault => fault, fault => fault.Activation);
			if (!_allowFaultsOnInitialTransitions)
			{
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					fault.Activation = Activation.Suppressed;
				}
			}

			if (_activateIndependentFaultsAtStepBeginning)
			{
				// Note: Faults get activated and their effects occur, but they are not notified yet of their activation.
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					fault.TryActivate();
				}
			}

			RuntimeModel.ExecuteInitialStep();

			if (!_activateIndependentFaultsAtStepBeginning)
			{
				// force activation of non-transient faults
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					if (!(fault is Modeling.TransientFault))
						fault.TryActivate();
				}
			}
			if (!_allowFaultsOnInitialTransitions)
			{
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					fault.Activation = savedActivations[fault];
				}
			}
		}

		/// <summary>
		///   Runs a step of the simulation.
		/// </summary>
		private void SimulateStep()
		{
			foreach (var fault in RuntimeModel.NondeterministicFaults)
				fault.Reset();

			if (_activateIndependentFaultsAtStepBeginning)
			{
				// Note: Faults get activated and their effects occur, but they are not notified yet of their activation.
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					fault.TryActivate();
				}
			}

			RuntimeModel.ExecuteStep();

			if (!_activateIndependentFaultsAtStepBeginning)
			{
				// force activation of non-transient faults
				foreach (var fault in RuntimeModel.NondeterministicFaults)
				{
					if (!(fault is Modeling.TransientFault))
						fault.TryActivate();
				}
			}
		}

		private void AddCurrentSituationToTrace()
		{
			var step = new SimulationTraceStep();
			AddState(step);
			AddChoices(step);
			var evaluatedCompilableFormulas = EvaluateCompilableFormulas();
			AddEvaluatedCompilableFormulas(step, evaluatedCompilableFormulas);
			UpdateNormalizedFormulaSatisfactionCount(evaluatedCompilableFormulas);
			_simulationTrace.Add(step);
		}

		public void UpdateTraceProbability()
		{
			TraceProbability *= _choiceResolver.CalculateProbabilityOfPath();
		}

		public Probability TraceProbability { get; private set; }
		
		private void ResetStep()
		{
			_choiceResolver.Clear();
		}

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		private void ResetTrace()
		{
			if (_choiceResolver == null)
			{
				_choiceResolver = new ProbabilisticSimulatorChoiceResolver();
				RuntimeModel.SetChoiceResolver(_choiceResolver);
			}

			for (var i = 0; i < NormalizedFormulaSatisfactionCount.Length; i++)
			{
				NormalizedFormulaSatisfactionCount[i] = 0;
			}

			_simulationTrace.Clear();

			RuntimeModel.Reset();

			TraceProbability = Probability.One;
		}

		/// <summary>
		///   Adds the state to the simulator.
		/// </summary>
		private void AddState(SimulationTraceStep step)
		{
			var newState = new byte[RuntimeModel.StateVectorSize];
			fixed (byte* state = newState)
			{
				RuntimeModel.Serialize(state);
			}

			step.State=newState;
		}
		
		private void AddChoices(SimulationTraceStep step)
		{
			step.Choices = _choiceResolver.GetChoices().ToArray();
		}

		private StateFormulaSet EvaluateCompilableFormulas()
		{
			return new StateFormulaSet(_compiledNormalizedFormulas);
		}

		private void AddEvaluatedCompilableFormulas(SimulationTraceStep step, StateFormulaSet evaluatedCompilableFormulas)
		{
			step.EvaluatedNormalizedFormulas = evaluatedCompilableFormulas;
		}

		private void UpdateNormalizedFormulaSatisfactionCount(StateFormulaSet evaluatedCompilableFormulas)
		{
			for (var i = 0; i < NormalizedFormulas.Length; i++)
			{
				if (evaluatedCompilableFormulas[i])
				{
					NormalizedFormulaSatisfactionCount[i]++;
				}
			}
		}

		public int GetCountOfSatisfiedOnTrace(Formula formula)
		{
			Assert.That(NormalizedFormulaLabels.Contains(formula.Label),"formula must be a compilable formula and be known in advance");
			var index = Array.IndexOf(NormalizedFormulaLabels, formula.Label);
			return NormalizedFormulaSatisfactionCount[index];
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;
			
			_choiceResolver.SafeDispose();
		}
	}
}

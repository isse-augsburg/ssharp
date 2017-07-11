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

namespace ISSE.SafetyChecking.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using ExecutableModel;
	using AnalysisModel;

	/// <summary>
	///   Represents a base class for all faults affecting the behavior of <see cref="Component" />s.
	/// </summary>
	[DebuggerDisplay("{Name} (#{Identifier}) [{Activation}]")]
	public abstract class Fault
	{
		public Choice Choice { get; } = new Choice();
		
		private Activation _activation = Activation.Nondeterministic;
		
		private bool _activationIsUnknown;
		
		private bool _canUndoActivation;
		
		private int _choiceIndex;
		
		private bool _isSubsumedFaultSetCached;
		
		private FaultSet _subsumedFaultSet;

		private Probability? _probabilityOfOccurrence;

		private Probability _probabilityOfOccurrenceComplement;

		public Probability? ProbabilityOfOccurrence
		{
			get { return _probabilityOfOccurrence; }
			set
			{
				_probabilityOfOccurrence = value;
				if (ProbabilityOfOccurrence != null)
					_probabilityOfOccurrenceComplement = ProbabilityOfOccurrence.Value.Complement();
			}
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="requiresActivationNotification">Indicates whether the fault must be notified about its activation.</param>
		protected Fault(bool requiresActivationNotification)
		{
			RequiresActivationNotification = requiresActivationNotification;
		}

		/// <summary>
		///   Gets the <see cref="Fault" /> instances subsumed by this fault.
		/// </summary>
		internal ISet<Fault> SubsumedFaults { get; } = new HashSet<Fault>();

		/// <summary>
		///   Gets a value indicating whether the fault must be notified about its activation.
		/// </summary>
		internal bool RequiresActivationNotification { get; private set; }

		/// <summary>
		///   Gets or sets an identifier for the fault.
		/// </summary>
		internal int Identifier { get; set; } = -1;

		/// <summary>
		///   Gets a value indicating whether the fault is used.
		/// </summary>
		internal bool IsUsed => Identifier != -1;

		/// <summary>
		///   Gets a value indicating whether the fault is activated and has some effect on the state of the system, therefore inducing
		///   an error or possibly a failure.
		/// </summary>
		public bool IsActivated { get; private set; }

		/// <summary>
		///   Gets or sets the fault's name.
		/// </summary>
		public string Name { get; set; } = "UnnamedFault";

		/// <summary>
		///   Gets or sets the fault's forced activation kind. This property should not be changed while model checking.
		/// </summary>
		public Activation Activation
		{
			get { return _activation; }
			set
			{
				_activation = value;

				if (value == Activation.Nondeterministic)
					IsActivated = false;
				else
					IsActivated = value == Activation.Forced;

				_activationIsUnknown = value == Activation.Nondeterministic;
			}
		}

		/// <summary>
		///   The set of subsumed faults, including transitively subsumed faults.
		/// </summary>
		internal FaultSet SubsumedFaultSet
		{
			get
			{
				if (!_isSubsumedFaultSetCached)
				{
					_subsumedFaultSet = CollectSubsumedFaultsTransitive();
					_isSubsumedFaultSetCached = true;
				}
				return _subsumedFaultSet;
			}
		}

		private FaultSet CollectSubsumedFaultsTransitive()
		{
			IEnumerable<Fault> currentFaults = new[] { this };
			var subsumed = new FaultSet(this);

			uint oldCount;
			do // fixed-point iteration
			{
				oldCount = subsumed.Cardinality;
				currentFaults = currentFaults.SelectMany(fault => fault.SubsumedFaults);
				subsumed = subsumed.GetUnion(new FaultSet(currentFaults));
			} while (oldCount < subsumed.Cardinality);

			return subsumed;
		}
		
		/// <summary>
		///   Declares the given <paramref name="faults" /> to be subsumed by this instance. Subsumption metadata does
		///   not change the fault's effects and is only used by heuristics.
		/// </summary>
		/// <param name="faults">The subsumed faults.</param>
		public void Subsumes(params Fault[] faults)
		{
			SubsumedFaults.UnionWith(faults);
		}

		/// <summary>
		///   Declares the given <paramref name="faults" /> to be subsumed by this instance. Subsumption metadata does
		///   not change the fault's effects and is only used by heuristics.
		/// </summary>
		/// <param name="faults">The subsumed faults.</param>
		public void Subsumes(IEnumerable<Fault> faults)
		{
			SubsumedFaults.UnionWith(faults);
		}

		/// <summary>
		///   Undoes the activation of the fault when the activation is known to have no observable effect and fault activation was
		///   nondeterministic in the current step.
		/// </summary>
		/// <remarks>
		///   This method is internal to simplify the public API of the class. The method is publically exposed via
		///   <see cref="FaultHelper.UndoActivation" /> for use by the S# compiler.
		/// </remarks>
		internal void UndoActivation()
		{
			if (!Choice.Resolver.UseForwardOptimization)
				return;

			if (!_canUndoActivation)
				return;

			_canUndoActivation = false;
			_activationIsUnknown = true;
			Choice.Resolver.ForwardUntakenChoicesAtIndex(_choiceIndex);
		}

		/// <summary>
		///   Tries to activate the fault.
		/// </summary>
		/// <remarks>
		///   This method is internal to simplify the public API of the class. The method is publically exposed via
		///   <see cref="FaultHelper.Activate" /> for use by the S# compiler.
		/// </remarks>
		internal void TryActivate()
		{
			if (!_activationIsUnknown)
				_canUndoActivation = false;
			else
			{
				switch (CheckActivation())
				{
					case Activation.Forced:
						IsActivated = true;
						_canUndoActivation = false;
						break;
					case Activation.Suppressed:
						IsActivated = false;
						_canUndoActivation = false;
						break;
					case Activation.Nondeterministic:
						if (_probabilityOfOccurrence != null)
						{
							IsActivated = Choice.Choose(new Option<bool>(_probabilityOfOccurrenceComplement, false), new Option<bool>(_probabilityOfOccurrence.Value, true));
						}
						else
						{
							IsActivated = Choice.Choose(false,true);
						}
						_choiceIndex = Choice.Resolver.LastChoiceIndex;
						_canUndoActivation = true;
						break;
					default:
						throw new InvalidOperationException("Unsupported fault activation.");
				}

				_activationIsUnknown = false;
			}
		}

		/// <summary>
		///   Resets the fault's activation state for the current step.
		/// </summary>
		public void Reset()
		{
			if (_activation != Activation.Nondeterministic)
				return;

			_activationIsUnknown = true;
			_canUndoActivation = false;
			IsActivated = false;
		}

		/// <summary>
		///   Invoked when the fault was activated. This method is allowed to have side effects.
		/// </summary>
		public virtual void OnActivated()
		{
		}

		/// <summary>
		///   Checks whether the fault can be activated nondeterministically, or whether it has to be or cannot be activated. This
		///   method is not allowed to have any side effects, as otherwise S#'s fault activation mechanism will be completely broken.
		/// </summary>
		protected abstract Activation CheckActivation();
	}
}
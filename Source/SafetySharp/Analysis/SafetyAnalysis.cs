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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;
	using System.Diagnostics;
	using System.Linq;
	using Heuristics;
	using Modeling;
	using SafetyChecking;
	using Utilities;

	/// <summary>
	///   Performs safety analyses on a model.
	/// </summary>
	public sealed class SafetyAnalysis
	{
		private readonly HashSet<FaultSet> _checkedSets = new HashSet<FaultSet>();
		private readonly Dictionary<FaultSet, CounterExample> _counterExamples = new Dictionary<FaultSet, CounterExample>();
		private readonly Dictionary<FaultSet, Exception> _exceptions = new Dictionary<FaultSet, Exception>();
		private AnalysisBackend _backend;
		private FaultSetCollection _criticalSets;
		private FaultSet _forcedSet;
		private SafetyAnalysisResults _results;
		private FaultSetCollection _safeSets;
		private FaultSet _suppressedSet;

		/// <summary>
		///   Determines the safety analysis backend that is used during the analysis.
		/// </summary>
		public SafetyAnalysisBackend Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		/// <summary>
		///   Determines how faults are activated during the analysis.
		/// </summary>
		public FaultActivationBehavior FaultActivationBehavior = FaultActivationBehavior.Nondeterministic;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public SafetyAnalysis()
		{
			Configuration.ProgressReportsOnly = true;
		}

		/// <summary>
		///   Gets a list of heuristics to use during the analysis.
		/// </summary>
		public List<IFaultSetHeuristic> Heuristics { get; } = new List<IFaultSetHeuristic>();

		/// <summary>
		///   Raised when the model checker has written an output.
		/// </summary>
		public event Action<string> OutputWritten;

		/// <summary>
		///   Computes the minimal critical sets for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="model">The model the safety analysis should be conducted for.</param>
		/// <param name="hazard">The hazard the minimal critical sets should be computed for.</param>
		/// <param name="maxCardinality">
		///   The maximum cardinality of the fault sets that should be checked. By default, all minimal
		///   critical fault sets are determined.
		/// </param>
		/// <param name="backend">Determines the safety analysis backend that is used during the analysis.</param>
		public static SafetyAnalysisResults AnalyzeHazard(ModelBase model, Formula hazard, int maxCardinality = Int32.MaxValue,
														 SafetyAnalysisBackend backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly)
		{
			return new SafetyAnalysis { Backend = backend }.ComputeMinimalCriticalSets(model, hazard, maxCardinality);
		}

		/// <summary>
		///   Computes the minimal critical sets for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="model">The model the safety analysis should be conducted for.</param>
		/// <param name="hazard">The hazard the minimal critical sets should be computed for.</param>
		/// <param name="maxCardinality">
		///   The maximum cardinality of the fault sets that should be checked. By default, all minimal
		///   critical fault sets are determined.
		/// </param>
		public SafetyAnalysisResults ComputeMinimalCriticalSets(ModelBase model, Formula hazard, int maxCardinality = Int32.MaxValue)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(hazard, nameof(hazard));

			ConsoleHelpers.WriteLine("Running Deductive Cause Consequence Analysis.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var allFaults = model.Faults;
			FaultSet.CheckFaultCount(allFaults.Length);

			var forcedFaults = allFaults.Where(fault => fault.Activation == Activation.Forced).ToArray();
			var suppressedFaults = allFaults.Where(fault => fault.Activation == Activation.Suppressed).ToArray();
			var nondeterministicFaults = allFaults.Where(fault => fault.Activation == Activation.Nondeterministic).ToArray();

			_suppressedSet = new FaultSet(suppressedFaults);
			_forcedSet = new FaultSet(forcedFaults);

			var isComplete = true;

			// Remove information from previous analyses
			Reset(model);

			// Initialize the backend, the model, and the analysis results
			switch (Backend)
			{
				case SafetyAnalysisBackend.FaultOptimizedOnTheFly:
					_backend = new FaultOptimizationBackend();
					break;
				case SafetyAnalysisBackend.FaultOptimizedStateGraph:
					_backend = new StateGraphBackend();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			_backend.OutputWritten += output => OutputWritten?.Invoke(output);
			_backend.InitializeModel(Configuration, model, hazard);
			_results = new SafetyAnalysisResults(model, hazard, suppressedFaults, forcedFaults, Heuristics, FaultActivationBehavior);

			// Remember all safe sets of current cardinality - we need them to generate the next power set level
			var currentSafe = new HashSet<FaultSet>();

			// We check fault sets by increasing cardinality; this is, we check the empty set first, then
			// all singleton sets, then all sets with two elements, etc. We don't check sets that we
			// know are going to be critical sets due to monotonicity
			for (var cardinality = 0; cardinality <= allFaults.Length; ++cardinality)
			{
				// Generate the sets for the current level that we'll have to check
				var sets = GeneratePowerSetLevel(cardinality, allFaults, currentSafe);
				currentSafe.Clear();

				// Remove all sets that conflict with the forced or suppressed faults; these sets are considered to be safe.
				// If no sets remain, skip to the next level
				sets = RemoveInvalidSets(sets, currentSafe);
				if (sets.Count == 0)
					continue;

				// Abort if we've exceeded the maximum fault set cardinality; doing the check here allows us
				// to report the analysis as complete if the maximum cardinality is never reached
				if (cardinality > maxCardinality)
				{
					isComplete = false;
					break;
				}

				if (cardinality == 0)
					ConsoleHelpers.WriteLine("Checking the empty fault set...");
				else
					ConsoleHelpers.WriteLine($"Checking {sets.Count} sets of cardinality {cardinality}...");

				// use heuristics
				var setsToCheck = new List<FaultSet>(sets);
				foreach (var heuristic in Heuristics)
					heuristic.Augment(setsToCheck);

				// We have to check each set - heuristics may add further during the loop
				while (setsToCheck.Count > 0)
				{
					var set = setsToCheck[setsToCheck.Count - 1];

					var isCurrentLevel = sets.Remove(set); // returns true if set was actually contained
					setsToCheck.RemoveAt(setsToCheck.Count - 1);

					// for current level, we already know the set is valid
					var isValid = isCurrentLevel || IsValid(set);

					var isSafe = true;
					if (isValid)
						isSafe = CheckSet(set, allFaults, cardinality);

					if (isSafe && isCurrentLevel)
						currentSafe.Add(set);

					// inform heuristics about result and give them the opportunity to add further sets
					foreach (var heuristic in Heuristics)
						heuristic.Update(setsToCheck, set, isSafe);
				}

				// in case heuristics removed a set (they shouldn't)
				foreach (var set in sets)
				{
					var isSafe = CheckSet(set, allFaults, cardinality);
					if (isSafe)
						currentSafe.Add(set);
				}
			}

			// Reset the nondeterministic faults so as to not influence subsequent analyses
			foreach (var fault in nondeterministicFaults)
				fault.Activation = Activation.Nondeterministic;

			// due to heuristics usage, we may have informatiuon on non-minimal critical sets
			var minimalCritical = RemoveNonMinimalCriticalSets();

			_results.IsComplete = isComplete;
			_results.Time = stopwatch.Elapsed;
			_results.SetResult(minimalCritical, _checkedSets, _counterExamples, _exceptions);

			return _results;
		}

		private void Reset(ModelBase model)
		{
			_safeSets = new FaultSetCollection(model.Faults.Length);
			_criticalSets = new FaultSetCollection(model.Faults.Length);
			_checkedSets.Clear();
			_counterExamples.Clear();
			_exceptions.Clear();
		}

		private HashSet<FaultSet> RemoveNonMinimalCriticalSets()
		{
			var minimal = _criticalSets.GetMinimalSets();

			foreach (var set in _criticalSets)
			{
				if (!minimal.Contains(set))
				{
					_exceptions.Remove(set);
					_counterExamples.Remove(set);
				}
			}

			return minimal;
		}

		private bool CheckSet(FaultSet set, Fault[] allFaults, int cardinality)
		{
			var isHeuristic = cardinality != set.Cardinality;
			if (isHeuristic)
				_results.HeuristicSuggestionCount++;

			var isSafe = true;

			// check if set is trivially safe or critical
			// (do not add to safeSets / criticalSets if so, in order to keep them small)
			if (IsTriviallySafe(set))
			{
				_results.TrivialChecksCount++;
				if (isHeuristic)
					_results.HeuristicTrivialCount++;

				// do not add to safeSets: all subsets are subsets of safeSet as well
				return true;
			}

			if (IsTriviallyCritical(set))
			{
				_results.TrivialChecksCount++;
				if (isHeuristic)
					_results.HeuristicTrivialCount++;

				// do not add to criticalSets: non-minimal, and all supersets are supersets of criticalSet as well
				return false;
			}

			// if configured to do so, check with forced fault activation
			if (FaultActivationBehavior == FaultActivationBehavior.ForceOnly || FaultActivationBehavior == FaultActivationBehavior.ForceThenFallback)
				isSafe = CheckSet(set, allFaults, cardinality, Activation.Forced, isHeuristic);

			if (isSafe && FaultActivationBehavior == FaultActivationBehavior.ForceThenFallback)
				ConsoleHelpers.WriteLine("    Checking again with nondeterministic activation...");

			// check with nondeterministic fault activation
			if (isSafe && FaultActivationBehavior != FaultActivationBehavior.ForceOnly)
				isSafe = CheckSet(set, allFaults, cardinality, Activation.Nondeterministic, isHeuristic);

			if (isSafe) // remember non-trivially safe sets to avoid checking their subsets
			{
				_safeSets.Add(set);

				if (isHeuristic)
					_results.HeuristicNonTrivialSafeCount++;
			}

			return isSafe;
		}

		private bool CheckSet(FaultSet set, Fault[] allFaults, int cardinality, Activation activationMode, bool isHeuristic)
		{
			var heuristic = set.Cardinality == cardinality ? String.Empty : "[heuristic]";

			try
			{
				var result = _backend.CheckCriticality(set, activationMode);

				if (!result.FormulaHolds)
				{
					if (!isHeuristic)
						ConsoleHelpers.WriteLine($"    {heuristic} critical:  {{ {set.ToString(allFaults)} }}", ConsoleColor.DarkRed);

					_criticalSets.Add(set);
				}
				else if (isHeuristic)
				{
					ConsoleHelpers.WriteLine($"    {heuristic} safe:  {{ {set.ToString(allFaults)} }}", ConsoleColor.Blue);
				}

				_checkedSets.Add(set);

				if (result.CounterExample != null)
					_counterExamples.Add(set, result.CounterExample);

				return result.FormulaHolds;
			}
			catch (AnalysisException e)
			{
				ConsoleHelpers.WriteLine($"    {heuristic} critical:  {{ {set.ToString(allFaults)} }} [exception thrown]", ConsoleColor.DarkRed);
				Console.WriteLine(e.InnerException);

				_checkedSets.Add(set);
				_criticalSets.Add(set);
				_exceptions.Add(set, e.InnerException);

				if (e.CounterExample != null)
					_counterExamples.Add(set, e.CounterExample);
				return false;
			}
		}

		private bool IsTriviallyCritical(FaultSet faultSet)
		{
			return _criticalSets.ContainsSubsetOf(faultSet);
		}

		private bool IsTriviallySafe(FaultSet faultSet)
		{
			return _safeSets.ContainsSupersetOf(faultSet);
		}

		/// <summary>
		///   Generates a level of the power set.
		/// </summary>
		/// <param name="cardinality">The cardinality of the sets that should be generated.</param>
		/// <param name="faults">The fault set the power set is generated for.</param>
		/// <param name="previousSafe">The set of safe sets generated at the previous level.</param>
		private static HashSet<FaultSet> GeneratePowerSetLevel(int cardinality, Fault[] faults, HashSet<FaultSet> previousSafe)
		{
			var result = new HashSet<FaultSet>();

			switch (cardinality)
			{
				case 0:
					// There is only the empty set with a cardinality of 0
					result.Add(new FaultSet());
					break;
				case 1:
					// We have to kick things off by explicitly generating the singleton sets; at this point,
					// we know that there are no further minimal critical sets if the empty set is already critical.
					if (previousSafe.Count > 0)
					{
						foreach (var fault in faults)
							result.Add(new FaultSet(fault));
					}
					break;
				default:
					// We now generate the sets with the requested cardinality based on the sets from the previous level
					// which had a cardinality that is one less than the sets we're going to generate now. The basic
					// idea is that we create the union between all safe sets and all singleton sets and discard
					// the ones we don't want, while avoiding duplicate generation of sets.

					var setsToRemove = new HashSet<FaultSet>();
					for (var i = 0; i < faults.Length; ++i)
					{
						var fault = faults[i];
						setsToRemove.Clear();

						foreach (var safeSet in previousSafe)
						{
							// avoid duplicate set generation
							if (safeSet.Contains(fault))
							{
								setsToRemove.Add(safeSet);
								continue;
							}

							var set = safeSet.Add(fault);

							// set is trivially critical iff one of the direct subsets is not safe (i.e. critical)
							// * the faults faults[0], ..., faults[i-1] are not definitely not contained in set (see above)
							// * faults[i] is definitely in set, but set.Remove(faults[i]) == safeSet and is thus safe.
							var isTriviallyCritical = false;
							for (var j = i + 1; j < faults.Length; ++j)
							{
								var f = faults[j];
								if (set.Contains(f) && !previousSafe.Contains(set.Remove(f)))
								{
									isTriviallyCritical = true;
									break;
								}
							}

							// Check if the newly generated set is a super set of any critical sets;
							// if so, discard it
							if (!isTriviallyCritical)
								result.Add(set);
						}

						// all supersets of sets in setsToRemove have either
						// been previously generated or are critical
						previousSafe.ExceptWith(setsToRemove);

						// if no more sets in previousSafe, further iterations are pointless
						if (previousSafe.Count == 0)
							break;
					}
					break;
			}

			return result;
		}

		/// <summary>
		///   Removes all invalid sets from <paramref name="sets" /> that conflict with either <see cref="_suppressedSet" /> or
		///   <see cref="_forcedSet" />.
		/// </summary>
		private HashSet<FaultSet> RemoveInvalidSets(HashSet<FaultSet> sets, HashSet<FaultSet> currentSafe)
		{
			if (_suppressedSet.IsEmpty && _forcedSet.IsEmpty)
				return sets;

			var validSets = new HashSet<FaultSet>();
			foreach (var set in sets)
			{
				if (IsValid(set))
					validSets.Add(set);
				else
					currentSafe.Add(set); // necessary so its supersets will be generated
			}

			return validSets;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsValid(FaultSet set)
		{
			// The set must contain all forced faults, hence it must be a superset of those
			// The set is not allowed to contain any suppressed faults, hence the intersection must be empty
			return _forcedSet.IsSubsetOf(set) && _suppressedSet.GetIntersection(set).IsEmpty;
		}
	}
}
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System.IO;
	using System.Linq;
	using Modeling;
	using Runtime.Reflection;
	using Utilities;

	/// <summary>
	///   Performs safety analyses on a model.
	/// </summary>
	public sealed class SafetyAnalysis
	{
		/// <summary>
		///   The model that is analyzed.
		/// </summary>
		private readonly Model _model;

		/// <summary>
		///   The model checker that is used for the analysis.
		/// </summary>
		private readonly ModelChecker _modelChecker;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="modelChecker">The model checker that should be used for the analysis.</param>
		/// <param name="model">The model that should be analyzed.</param>
		public SafetyAnalysis(ModelChecker modelChecker, Model model)
		{
			Requires.NotNull(modelChecker, nameof(modelChecker));
			Requires.NotNull(model, nameof(model));

			_modelChecker = modelChecker;
			_model = model;
		}

		/// <summary>
		///   Computes the minimal cut sets for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="hazard">The hazard the minimal cut sets should be computed for.</param>
		/// <param name="counterExamplePath">
		///   The path the generated counter examples should be written to. If null, counter examples are
		///   not written.
		/// </param>
		public Result ComputeMinimalCutSets(Formula hazard, string counterExamplePath = null)
		{
			Requires.NotNull(hazard, nameof(hazard));

			if (!String.IsNullOrWhiteSpace(counterExamplePath))
				Directory.CreateDirectory(counterExamplePath);

			var faults = _model.GetFaults();
			var safeSets = new HashSet<int>();
			var cutSets = new HashSet<int>();
			var checkedSets = new HashSet<int>();

			Assert.That(faults.Length < 32, "More than 31 faults are currently not supported.");

			// We check fault sets by increasing cardinality; this is, we check the empty set first, then
			// all singleton sets, then all sets with two elements, etc. We don't check sets that we
			// know are going to be cut sets due to monotonicity
			for (var cardinality = 0; cardinality <= faults.Length; ++cardinality)
			{
				// Generate the sets for the current level that we'll have to check
				var sets = GeneratePowerSetLevel(safeSets, cutSets, cardinality, faults.Length);

				// Clear the safe sets, we don't need the previous level to generate the next one
				safeSets.Clear();

				// If there are no sets to check, we're done; this happens when there are so many cut sets
				// that this level does not contain any set that is not a super set of any of those cut sets
				if (sets.Count == 0)
					break;

				// We have to check each set; if one of them is a cut set, it has no effect on the other
				// sets we have to check
				foreach (var set in sets)
				{
					// Enable or disable the faults that the set represents
					for (var i = 1; i <= faults.Length; ++i)
						faults[i - 1].ActivationMode = (set & (1 << (i - 1))) != 0 ? ActivationMode.Nondeterministic : ActivationMode.Never;

					// If there was a counter example, the set is a cut set
					var counterExample = _modelChecker.CheckInvariant(_model, !hazard);
					if (counterExample != null)
						cutSets.Add(set);
					else
						safeSets.Add(set);

					checkedSets.Add(set);

					if (counterExample == null || counterExamplePath == null)
						continue;

					var fileName = String.Join("_", faults.Where(f => f.ActivationMode == ActivationMode.Nondeterministic).Select(f => f.Name));
					if (String.IsNullOrWhiteSpace(fileName))
						fileName = "emptyset";

					counterExample.Save(Path.Combine(counterExamplePath, $"{fileName}{CounterExample.FileExtension}"));
				}
			}

			return new Result(cutSets, checkedSets, faults);
		}

		/// <summary>
		///   Generates a level of the power set.
		/// </summary>
		/// <param name="safeSets">The set of safe sets generated at the previous level.</param>
		/// <param name="cutSets">The sets that are known to be cut sets. All super sets are discarded.</param>
		/// <param name="cardinality">The cardinality of the sets that should be generated.</param>
		/// <param name="count">The number of elements in the set the power set is generated for.</param>
		private static HashSet<int> GeneratePowerSetLevel(HashSet<int> safeSets, HashSet<int> cutSets, int cardinality, int count)
		{
			var result = new HashSet<int>();

			switch (cardinality)
			{
				case 0:
					// There is only the empty set with a cardinality of 0
					result.Add(0);
					break;
				case 1:
					// We have to kick things off by explicitly generating the singleton sets; at this point,
					// we know that there are no further minimal cut sets if we've already found one (= the empty set)
					if (cutSets.Count == 0)
					{
						for (var i = 0; i < count; ++i)
							result.Add(1 << i);
					}
					break;
				default:
					// We now generate the sets with the requested cardinality based on the sets from the previous level 
					// which had a cardinality that is one less than the sets we're going to generate now. The basic
					// idea is that we create the union between all safe sets and all singleton sets and discard
					// the ones we don't want
					foreach (var safeSet in safeSets)
					{
						for (var i = 0; i < count; ++i)
						{
							// If we're trying to add an element to the set that it already contains, we get a set
							// we've already checked before; discard it
							if ((safeSet & (1 << i)) != 0)
								continue;

							var set = safeSet | (1 << i);

							// Check if the newly generated set it a super set of any cut sets; if so, discard it
							if (cutSets.All(s => (set & s) != s))
								result.Add(set);
						}
					}
					break;
			}

			return result;
		}

		/// <summary>
		///   Represents the result of a safety analysis.
		/// </summary>
		public struct Result
		{
			/// <summary>
			///   Gets the minimal cut sets, each cut set containing the faults that potentially result in the occurrence of a hazard.
			/// </summary>
			public ISet<ISet<Fault>> MinimalCutSets { get; }

			/// <summary>
			///   Gets the number of minimal cut sets.
			/// </summary>
			public int MinimalCutSetsCount { get; }

			/// <summary>
			///   Gets all of the fault sets that were checked for criticality. Some sets might not have been checked as they were known to
			///   be cut sets due to the monotonicity of the cut set property.
			/// </summary>
			public ISet<ISet<Fault>> CheckedSets { get; }

			/// <summary>
			///   Gets the number of sets that have been checked for criticality. Some sets might not have been checked as they were known
			///   to be cut sets due to the monotonicity of the cut set property.
			/// </summary>
			public int CheckedSetsCount { get; }

			/// <summary>
			///   Gets the number of faults that have been checked.
			/// </summary>
			public int FaultCount { get; }

			/// <summary>
			///   Gets the faults that have been checked.
			/// </summary>
			public IEnumerable<Fault> Faults { get; }

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="cutSets">The minimal cut sets.</param>
			/// <param name="checkedSets">The sets that have been checked.</param>
			/// <param name="faults">The faults that have been checked.</param>
			internal Result(HashSet<int> cutSets, HashSet<int> checkedSets, Fault[] faults)
			{
				MinimalCutSetsCount = cutSets.Count;
				CheckedSetsCount = checkedSets.Count;
				FaultCount = faults.Length;

				MinimalCutSets = Convert(cutSets, faults);
				CheckedSets = Convert(checkedSets, faults);
				Faults = faults;
			}

			/// <summary>
			///   Converts the integer-based set to a sets of fault sets.
			/// </summary>
			private static ISet<ISet<Fault>> Convert(HashSet<int> sets, Fault[] faults)
			{
				var result = new HashSet<ISet<Fault>>();

				foreach (var set in sets)
				{
					var faultSet = new HashSet<Fault>();
					for (var i = 1; i <= faults.Length; ++i)
					{
						if ((set & (1 << (i - 1))) != 0)
							faultSet.Add(faults[i - 1]);
					}

					result.Add(faultSet);
				}

				return result;
			}
		}
	}
}
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
	using System.IO;
	using System.Linq;
	using System.Text;
	using Modeling;
	using Runtime;
	using Runtime.Reflection;
	using Runtime.Serialization;
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
		private readonly SSharpChecker _modelChecker = new SSharpChecker();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be analyzed.</param>
		public SafetyAnalysis(Model model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
		}

		/// <summary>
		///   Gets or sets the number of states that can be stored during model checking.
		/// </summary>
		public int StateCapacity
		{
			get { return _modelChecker.StateCapacity; }
			set { _modelChecker.StateCapacity = value; }
		}

		/// <summary>
		///   Gets or sets the number of states that can be stored on the stack during model checking.
		/// </summary>
		public int StackCapacity
		{
			get { return _modelChecker.StackCapacity; }
			set { _modelChecker.StackCapacity = value; }
		}

		/// <summary>
		///   Gets or sets the number of CPUs that are used for model checking. The value is clamped to the interval of [1, #CPUs].
		/// </summary>
		public int CpuCount
		{
			get { return _modelChecker.CpuCount; }
			set { _modelChecker.CpuCount = value; }
		}

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten
		{
			add { _modelChecker.OutputWritten += value; }
			remove { _modelChecker.OutputWritten += value; }
		}

		/// <summary>
		///   Computes the minimal critical sets for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="hazard">The hazard the minimal critical sets should be computed for.</param>
		public Result ComputeMinimalCriticalSets(Formula hazard)
		{
			Requires.NotNull(hazard, nameof(hazard));

			var faults = _model.GetFaults();
			Requires.That(faults.Length < 32, "More than 31 faults are currently not supported.");

			for (var i = 0; i < faults.Length; ++i)
				faults[i].Identifier = i;

			var safeSets = new HashSet<int>();
			var criticalSets = new HashSet<int>();
			var checkedSets = new HashSet<int>();
			var counterExamples = new Dictionary<int, CounterExample>();
			var exceptions = new Dictionary<int, Exception>();

			// Store the serialized model to improve performance
			var serializedModel = RuntimeModelSerializer.Save(_model, !hazard);

			// We check fault sets by increasing cardinality; this is, we check the empty set first, then
			// all singleton sets, then all sets with two elements, etc. We don't check sets that we
			// know are going to be critical sets due to monotonicity
			for (var cardinality = 0; cardinality <= faults.Length; ++cardinality)
			{
				// Generate the sets for the current level that we'll have to check
				var sets = GeneratePowerSetLevel(safeSets, criticalSets, cardinality, faults.Length);

				// Clear the safe sets, we don't need the previous level to generate the next one
				safeSets.Clear();

				// If there are no sets to check, we're done; this happens when there are so many critical sets
				// that this level does not contain any set that is not a super set of any of those critical sets
				if (sets.Count == 0)
					break;

				// We have to check each set; if one of them is a critical set, it has no effect on the other
				// sets we have to check
				foreach (var set in sets)
				{
					// Enable or disable the faults that the set represents
					for (var i = 1; i <= faults.Length; ++i)
						faults[i - 1].Activation = (set & (1 << (i - 1))) != 0 ? Activation.Nondeterministic : Activation.Suppressed;

					var faultNames = faults
						.Where(fault => fault.Activation == Activation.Nondeterministic)
						.Select(fault => fault.Name)
						.OrderBy(name => name)
						.ToArray();
					var setRepresentation = faultNames.Length == 0 ? "{}" : String.Join(", ", faultNames);

					// If there was a counter example, the set is a critical set
					try
					{
						var result = _modelChecker.CheckInvariant(CreateRuntimeModel(serializedModel, faults));

						if (!result.FormulaHolds)
						{
							_modelChecker.Output($"*** Found minimal critical fault set: {setRepresentation}.");
							_modelChecker.Output("");

							criticalSets.Add(set);
						}
						else
						{
							_modelChecker.Output($"*** Found safe fault set: {setRepresentation}.");
							_modelChecker.Output("");

							safeSets.Add(set);
						}

						checkedSets.Add(set);

						if (result.CounterExample != null)
							counterExamples.Add(set, result.CounterExample);
					}
					catch (AnalysisException e)
					{
						checkedSets.Add(set);
						criticalSets.Add(set);

						exceptions.Add(set, e.InnerException);
						counterExamples.Add(set, e.CounterExample);
					}
				}
			}

			return new Result(criticalSets, checkedSets, faults, counterExamples, exceptions);
		}

		/// <summary>
		///   Creates a <see cref="RuntimeModel" /> instance.
		/// </summary>
		private static Func<RuntimeModel> CreateRuntimeModel(byte[] serializedModel, Fault[] faultTemplates)
		{
			return () =>
			{
				var serializedData = RuntimeModelSerializer.LoadSerializedData(serializedModel);
				var faults = serializedData.ObjectTable.OfType<Fault>().OrderBy(f => f.Identifier).ToArray();
				Requires.That(faults.Length == faultTemplates.Length, "Unexpected fault count.");

				for (var i = 0; i < faults.Length; ++i)
				{
					Requires.That(faults[i].Identifier == faultTemplates[i].Identifier, "Fault mismatch.");
					faults[i].Activation = faultTemplates[i].Activation;
				}

				return new RuntimeModel(serializedData);
			};
		}

		/// <summary>
		///   Checks if the system has an inherent safety flaw. If this is not the case,
		///   computes the single points of failures for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="hazard">The hazard the minimal cut sets should be computed for.</param>
		/// <param name="counterExamplePath">
		///   The path the generated counter examples should be written to. If null, counter examples are
		///   not written.
		/// </param>
		public Result ComputeSinglePointsOfFailures(Formula hazard, string counterExamplePath = null)
		{
			Requires.NotNull(hazard, nameof(hazard));

			var faults = _model.GetFaults();
			Requires.That(faults.Length < 32, "More than 31 faults are currently not supported.");

			for (var i = 0; i < faults.Length; ++i)
				faults[i].Identifier = i;

			var safeSets = new HashSet<int>();
			var criticalSets = new HashSet<int>();
			var checkedSets = new HashSet<int>();
			var counterExamples = new Dictionary<int, CounterExample>();
			var exceptions = new Dictionary<int, Exception>();

			// Store the serialized model to improve performance
			var serializedModel = RuntimeModelSerializer.Save(_model, !hazard);

			// Max cardinality is either 0 or 1 depending on the number of faults.
			var maxCardinalityToCheck = Math.Min(faults.Length, 1);

			// We check fault sets by increasing cardinality; this is, we check the empty set first, then
			// all singleton sets (SPOFs)
			for (var cardinality = 0; cardinality <= maxCardinalityToCheck; ++cardinality)
			{
				// Generate the sets for the current level that we'll have to check
				var sets = GeneratePowerSetLevel(safeSets, criticalSets, cardinality, faults.Length);

				// Clear the safe sets, we don't need the previous level to generate the next one
				safeSets.Clear();

				// If there are no sets to check, we're done; this happens when there are so many critical sets
				// that this level does not contain any set that is not a super set of any of those critical sets
				if (sets.Count == 0)
					break;

				// We have to check each set; if one of them is a critical set, it has no effect on the other
				// sets we have to check
				foreach (var set in sets)
				{
					// Enable or disable the faults that the set represents
					for (var i = 1; i <= faults.Length; ++i)
						faults[i - 1].Activation = (set & (1 << (i - 1))) != 0 ? Activation.Nondeterministic : Activation.Suppressed;

					var faultNames = faults
						.Where(fault => fault.Activation == Activation.Nondeterministic)
						.Select(fault => fault.Name)
						.OrderBy(name => name)
						.ToArray();
					var setRepresentation = faultNames.Length == 0 ? "{}" : String.Join(", ", faultNames);

					// If there was a counter example, the set is a critical set
					try
					{
						var result = _modelChecker.CheckInvariant(CreateRuntimeModel(serializedModel, faults));

						if (!result.FormulaHolds)
						{
							_modelChecker.Output($"*** Found minimal critical fault set: {setRepresentation}.");
							_modelChecker.Output("");

							criticalSets.Add(set);
						}
						else
						{
							_modelChecker.Output($"*** Found safe fault set: {setRepresentation}.");
							_modelChecker.Output("");

							safeSets.Add(set);
						}

						checkedSets.Add(set);

						if (result.CounterExample != null)
							counterExamples.Add(set, result.CounterExample);
					}
					catch (AnalysisException e)
					{
						checkedSets.Add(set);
						criticalSets.Add(set);

						exceptions.Add(set, e.InnerException);
						counterExamples.Add(set, e.CounterExample);
					}
				}
			}

			return new Result(criticalSets, checkedSets, faults, counterExamples, exceptions);
		}

		/// <summary>
		///   Generates a level of the power set.
		/// </summary>
		/// <param name="safeSets">The set of safe sets generated at the previous level.</param>
		/// <param name="criticalSets">The sets that are known to be critical sets. All super sets are discarded.</param>
		/// <param name="cardinality">The cardinality of the sets that should be generated.</param>
		/// <param name="count">The number of elements in the set the power set is generated for.</param>
		private static HashSet<int> GeneratePowerSetLevel(HashSet<int> safeSets, HashSet<int> criticalSets, int cardinality, int count)
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
					// we know that there are no further minimal critical sets if we've already found one (= the empty set)
					if (criticalSets.Count == 0)
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

							// Check if the newly generated set it a super set of any critical sets; if so, discard it
							if (criticalSets.All(s => (set & s) != s))
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
			///   Gets the minimal critical sets, each critical set containing the faults that potentially result in the occurrence of a
			///   hazard.
			/// </summary>
			public ISet<ISet<Fault>> MinimalCriticalSets { get; }

			/// <summary>
			///   Gets all of the fault sets that were checked for criticality. Some sets might not have been checked as they were known to
			///   be critical sets due to the monotonicity of the critical set property.
			/// </summary>
			public ISet<ISet<Fault>> CheckedSets { get; }

			/// <summary>
			///   Gets the exception that has been thrown during the analysis, if any.
			/// </summary>
			public IDictionary<ISet<Fault>, Exception> Exceptions { get; }

			/// <summary>
			///   Gets the faults that have been checked.
			/// </summary>
			public IEnumerable<Fault> Faults { get; }

			/// <summary>
			///   Gets the counter examples that were generated for the critical fault sets.
			/// </summary>
			public IDictionary<ISet<Fault>, CounterExample> CounterExamples { get; }

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="criticalSets">The minimal critical sets.</param>
			/// <param name="checkedSets">The sets that have been checked.</param>
			/// <param name="faults">The faults that have been checked.</param>
			/// <param name="counterExamples">The counter examples that were generated for the critical fault sets.</param>
			/// <param name="exceptions">The exceptions that have been thrown during the analysis.</param>
			internal Result(HashSet<int> criticalSets, HashSet<int> checkedSets, Fault[] faults, Dictionary<int, CounterExample> counterExamples,
							Dictionary<int, Exception> exceptions)
			{
				var knownFaultSets = new Dictionary<int, ISet<Fault>>();

				MinimalCriticalSets = Convert(knownFaultSets, criticalSets, faults);
				CheckedSets = Convert(knownFaultSets, checkedSets, faults);
				Faults = faults;
				CounterExamples = counterExamples.ToDictionary(pair => Convert(knownFaultSets, pair.Key, faults), pair => pair.Value);
				Exceptions = exceptions.ToDictionary(pair => Convert(knownFaultSets, pair.Key, faults), pair => pair.Value);
			}

			/// <summary>
			///   Converts the integer-based sets to a sets of fault sets.
			/// </summary>
			private static ISet<ISet<Fault>> Convert(Dictionary<int, ISet<Fault>> knownSets, HashSet<int> sets, Fault[] faults)
			{
				var result = new HashSet<ISet<Fault>>();

				foreach (var set in sets)
					result.Add(Convert(knownSets, set, faults));

				return result;
			}

			/// <summary>
			///   Converts the integer-based set to a set faults.
			/// </summary>
			private static ISet<Fault> Convert(Dictionary<int, ISet<Fault>> knownSets, int set, Fault[] faults)
			{
				ISet<Fault> faultSet;
				if (knownSets.TryGetValue(set, out faultSet))
					return faultSet;

				faultSet = new HashSet<Fault>();
				for (var i = 1; i <= faults.Length; ++i)
				{
					if ((set & (1 << (i - 1))) != 0)
						faultSet.Add(faults[i - 1]);
				}

				knownSets.Add(set, faultSet);
				return faultSet;
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="directory">The directory the generated counter examples should be written to.</param>
			public void SaveCounterExamples(string directory)
			{
				Requires.NotNullOrWhitespace(directory, nameof(directory));

				if (!String.IsNullOrWhiteSpace(directory))
					Directory.CreateDirectory(directory);

				foreach (var pair in CounterExamples)
				{
					var fileName = String.Join("_", pair.Key.Select(f => f.Name));
					if (String.IsNullOrWhiteSpace(fileName))
						fileName = "emptyset";

					pair.Value.Save(Path.Combine(directory, $"{fileName}{CounterExample.FileExtension}"));
				}
			}

			/// <summary>
			///   Returns a string representation of the minimal critical fault sets.
			/// </summary>
			public override string ToString()
			{
				var builder = new StringBuilder();
				var percentage = CheckedSets.Count / (float)(1 << Faults.Count()) * 100;

				builder.AppendLine();
				builder.AppendLine("=======================================================================");
				builder.AppendLine("=======      Deductive Cause Consequence Analysis: Results      =======");
				builder.AppendLine("=======================================================================");
				builder.AppendLine();

				if (Exceptions.Any())
				{
					builder.AppendLine("*** Warning: Unhandled exceptions have been thrown during the analysis. ***");
					builder.AppendLine();
				}

				builder.AppendFormat("Fault Count: {0}", Faults.Count());
				builder.AppendLine();
				builder.AppendFormat("Faults: {0}", String.Join(", ", Faults.Select(fault => fault.Name).OrderBy(name => name)));
				builder.AppendLine();
				builder.AppendLine();

				builder.AppendFormat("Checked Fault Sets: {0} ({1:F0}% of all fault sets)", CheckedSets.Count, percentage);
				builder.AppendLine();

				builder.AppendFormat("Minimal Critical Sets: {0}", MinimalCriticalSets.Count);
				builder.AppendLine();
				builder.AppendLine();

				var i = 1;
				foreach (var criticalSet in MinimalCriticalSets)
				{
					builder.AppendFormat("   ({1}) {{ {0} }}", String.Join(", ", criticalSet.Select(fault => fault.Name)), i++);

					Exception e;
					if (Exceptions.TryGetValue(criticalSet, out e))
					{
						builder.AppendLine();
						builder.AppendFormat(
							"    An unhandled exception of type {0} was thrown while checking the fault set: {1}.", 
							e.GetType().FullName, e.Message);
					}

					builder.AppendLine();
				}

				return builder.ToString();
			}
		}
	}
}
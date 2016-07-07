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
	using Heuristics;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Represents the result of a <see cref="SafetyAnalysis" />.
	/// </summary>
	public class SafetyAnalysisResult
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The <see cref="Model" /> instance the safety analysis was conducted for.</param>
		/// <param name="suppressedFaults">The faults whose activations have been completely suppressed during analysis.</param>
		/// <param name="forcedFaults">The faults whose activations have been forced during analysis.</param>
		/// <param name="heuristics">The heuristics that are used during the analysis.</param>
		internal SafetyAnalysisResult(ModelBase model, IEnumerable<Fault> suppressedFaults, IEnumerable<Fault> forcedFaults,
									  IEnumerable<IFaultSetHeuristic> heuristics)
		{
			Model = model;
			SuppressedFaults = suppressedFaults;
			ForcedFaults = forcedFaults;
			Heuristics = heuristics.ToArray(); // make a copy so that later changes to the heuristics don't affect the results
		}

		/// <summary>
		///   Gets the faults whose activations have been completely suppressed during analysis.
		/// </summary>
		public IEnumerable<Fault> SuppressedFaults { get; }

		/// <summary>
		///   Gets the faults whose activations have been forced during analysis.
		/// </summary>
		public IEnumerable<Fault> ForcedFaults { get; }

		/// <summary>
		///   Gets the minimal critical sets, each critical set containing the faults that potentially result in the occurrence of a
		///   hazard.
		/// </summary>
		public ISet<ISet<Fault>> MinimalCriticalSets { get; private set; }

		/// <summary>
		///   Gets all of the fault sets that were checked for criticality. Some sets might not have been checked as they were known to
		///   be critical sets due to the monotonicity of the critical set property.
		/// </summary>
		public ISet<ISet<Fault>> CheckedSets { get; private set; }

		/// <summary>
		///   Gets the exception that has been thrown during the analysis, if any.
		/// </summary>
		public IDictionary<ISet<Fault>, Exception> Exceptions { get; private set; }

		/// <summary>
		///   Gets the faults that have been checked.
		/// </summary>
		public IEnumerable<Fault> Faults => Model.Faults;

		/// <summary>
		///   Gets the counter examples that were generated for the critical fault sets.
		/// </summary>
		public IDictionary<ISet<Fault>, CounterExample> CounterExamples { get; private set; }

		/// <summary>
		///   Gets a value indicating whether the analysis might is complete, i.e., all fault sets have been checked for criticality.
		/// </summary>
		public bool IsComplete { get; internal set; }

		/// <summary>
		///   Gets the <see cref="Model" /> instance the safety analysis was conducted for.
		/// </summary>
		public ModelBase Model { get; }

		/// <summary>
		///   Gets the time it took to complete the analysis.
		/// </summary>
		public TimeSpan Time { get; internal set; }

		/// <summary>
		///   Gets the heuristics that were used during analysis.
		/// </summary>
		public IEnumerable<IFaultSetHeuristic> Heuristics { get; }

		/// <summary>
		///   The total number of fault sets suggested by the heuristics.
		/// </summary>
		public int HeuristicSuggestionCount { get; internal set; }

		/// <summary>
		///   The number of sets suggested by a heuristic that were not trivially safe.
		/// </summary>
		public int HeuristicNonTrivialSafeCount { get; internal set; }

		/// <summary>
		///   The number of sets suggested by a heuristic that were trivially safe or critical.
		/// </summary>
		public int HeuristicTrivialCount { get; internal set; }

		/// <summary>
		///   The number of trivial checks that have been performed.
		/// </summary>
		public int TrivialChecksCount { get; internal set; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="criticalSets">The minimal critical sets.</param>
		/// <param name="checkedSets">The sets that have been checked.</param>
		/// <param name="counterExamples">The counter examples that were generated for the critical fault sets.</param>
		/// <param name="exceptions">The exceptions that have been thrown during the analysis.</param>
		internal void SetResult(HashSet<FaultSet> criticalSets, HashSet<FaultSet> checkedSets,
								Dictionary<FaultSet, CounterExample> counterExamples, Dictionary<FaultSet, Exception> exceptions)
		{
			var knownFaultSets = new Dictionary<FaultSet, ISet<Fault>>();

			MinimalCriticalSets = Convert(knownFaultSets, criticalSets);
			CheckedSets = Convert(knownFaultSets, checkedSets);
			CounterExamples = counterExamples.ToDictionary(pair => Convert(knownFaultSets, pair.Key), pair => pair.Value);
			Exceptions = exceptions.ToDictionary(pair => Convert(knownFaultSets, pair.Key), pair => pair.Value);
		}

		/// <summary>
		///   Converts the integer-based sets to a sets of fault sets.
		/// </summary>
		private ISet<ISet<Fault>> Convert(Dictionary<FaultSet, ISet<Fault>> knownSets, HashSet<FaultSet> sets)
		{
			var result = new HashSet<ISet<Fault>>(ReferenceEqualityComparer<ISet<Fault>>.Default);

			foreach (var set in sets)
				result.Add(Convert(knownSets, set));

			return result;
		}

		/// <summary>
		///   Converts the integer-based set to a set faults.
		/// </summary>
		private ISet<Fault> Convert(Dictionary<FaultSet, ISet<Fault>> knownSets, FaultSet set)
		{
			ISet<Fault> faultSet;
			if (knownSets.TryGetValue(set, out faultSet))
				return faultSet;

			faultSet = new HashSet<Fault>(set.ToFaultSequence(Model.Faults));
			knownSets.Add(set, faultSet);

			return faultSet;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="directory">The directory the generated counter examples should be written to.</param>
		/// <param name="clearFiles">Indicates whether all files in the directory should be cleared before saving the counter examples.</param>
		public void SaveCounterExamples(string directory, bool clearFiles = true)
		{
			Requires.NotNullOrWhitespace(directory, nameof(directory));

			if (clearFiles && Directory.Exists(directory))
			{
				foreach (var file in new DirectoryInfo(directory).GetFiles())
					file.Delete();
			}

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
			var percentage = CheckedSets.Count / (double)(1L << Faults.Count()) * 100;

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

			if (!IsComplete)
			{
				builder.AppendLine("*** Warning: Analysis might be incomplete; not all fault sets have been checked. ***");
				builder.AppendLine();
			}

			Func<IEnumerable<Fault>, string> getFaultString =
				faults => String.Join(", ", faults.Select(fault => fault.Name).OrderBy(name => name));

			builder.AppendLine($"Elapsed Time: {Time}");
			builder.AppendLine($"Fault Count: {Faults.Count()}");
			builder.AppendLine($"Faults: {getFaultString(Faults)}");

			if (ForcedFaults.Any())
				builder.AppendLine($"Forced Faults: {getFaultString(ForcedFaults)}");

			if (SuppressedFaults.Any())
				builder.AppendLine($"Suppressed Faults: {getFaultString(SuppressedFaults)}");

			builder.AppendLine();
			builder.AppendLine($"Checked Fault Sets: {CheckedSets.Count} ({percentage:F0}% of all fault sets)");
			builder.AppendLine($"Minimal Critical Sets: {MinimalCriticalSets.Count}");
			builder.AppendLine();

			var i = 1;
			foreach (var criticalSet in MinimalCriticalSets)
			{
				builder.AppendFormat("   ({1}) {{ {0} }}", String.Join(", ", criticalSet.Select(fault => fault.Name).OrderBy(name => name)), i++);

				Exception e;
				if (Exceptions.TryGetValue(criticalSet, out e))
				{
					builder.AppendLine();
					builder.Append(
						$"    An unhandled exception of type {e.GetType().FullName} was thrown while checking the fault set: {e.Message}");
				}

				builder.AppendLine();
			}

			var heuristicCount = Heuristics.Count();
			if (heuristicCount != 0)
			{
				builder.AppendLine();

				if (HeuristicSuggestionCount == 0)
					builder.AppendLine("No suggestions were made by the heuristics.");
				else
				{
					var nonTriviallyCritical = HeuristicSuggestionCount - HeuristicNonTrivialSafeCount - HeuristicTrivialCount;
					var percentageTrivial = HeuristicTrivialCount / (double)(HeuristicSuggestionCount) * 100;
					var percentageNonTrivialSafe = HeuristicNonTrivialSafeCount / (double)(HeuristicSuggestionCount) * 100;
					var percentageNonTrivialCritical = nonTriviallyCritical / (double)(HeuristicSuggestionCount) * 100;

					builder.AppendLine($"Of {HeuristicSuggestionCount} fault sets suggested by {heuristicCount} heuristics");
					builder.AppendLine($"    {HeuristicTrivialCount} ({percentageTrivial:F0}%) were trivially safe or trivially critical,");
					builder.AppendLine($"    {HeuristicNonTrivialSafeCount} ({percentageNonTrivialSafe:F0}%) were non-trivially safe, and");
					builder.AppendLine($"    {nonTriviallyCritical} ({percentageNonTrivialCritical:F0}%) were non-trivially critical.");
					builder.AppendLine($"In total, {TrivialChecksCount} trivial checks were performed.");
				}
			}

			return builder.ToString();
		}
	}
}
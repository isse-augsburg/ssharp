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

namespace SafetySharp.CaseStudies.PillProduction.Analysis
{
	using System;
	using System.Linq;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using static SafetySharp.Analysis.Operators;

	public class ModelCheckingTests
	{
		public enum HeuristicsUsage
		{
			None,
			Subsumption,
			Redundancy,
			Both
		}

		[Test]
		public void ForcedActivationIncompleteDcca(
			[Values(FaultActivationBehavior.ForceOnly, FaultActivationBehavior.Nondeterministic)] FaultActivationBehavior activation)
		{
			// create stations
			var producer = new ContainerLoader();
			var commonDispenser = new ParticulateDispenser();
			var dispenserTop = new ParticulateDispenser();
			var dispenserBottom = new ParticulateDispenser();
			var consumerTop = new PalletisationStation();
			var consumerBottom = new PalletisationStation();
			var stations = new Station[] { producer, commonDispenser, dispenserTop, dispenserBottom, consumerTop, consumerBottom };

			// set very limited ingredient amounts
			commonDispenser.SetStoredAmount(IngredientType.BlueParticulate, 40);
			dispenserTop.SetStoredAmount(IngredientType.RedParticulate, 100);
			dispenserBottom.SetStoredAmount(IngredientType.RedParticulate, 100);

			// create connections
			producer.Outputs.Add(commonDispenser);
			commonDispenser.Inputs.Add(producer);

			commonDispenser.Outputs.Add(dispenserTop);
			dispenserTop.Inputs.Add(commonDispenser);
			commonDispenser.Outputs.Add(dispenserBottom);
			dispenserBottom.Inputs.Add(commonDispenser);

			dispenserTop.Outputs.Add(consumerTop);
			consumerTop.Inputs.Add(dispenserTop);

			dispenserBottom.Outputs.Add(consumerBottom);
			consumerBottom.Inputs.Add(dispenserBottom);

			var model = new Model(stations, new FastObserverController(stations));
			var recipe = new Recipe(new[] { new Ingredient(IngredientType.BlueParticulate, 30), new Ingredient(IngredientType.RedParticulate, 10) }, 1u);
			model.ScheduleProduction(recipe);

			Dcca(model, activation);
		}

		[Test, Combinatorial]
		public void DccaTest(
			[Values("complete_network.model", "bidirectional_circle.model", "duplicate_dispenser.model", "simple_circle.model", "trivial_setup.model",
				"simple_setup.model", "simple_setup3.model", "simple_setup2.model", "medium_setup.model", "complex_setup.model")]
				string modelFile,
			[Values(HeuristicsUsage.None, HeuristicsUsage.Subsumption, HeuristicsUsage.Redundancy, HeuristicsUsage.Both)]
				HeuristicsUsage heuristicsUsage,
			[Values(FaultActivationBehavior.ForceOnly, FaultActivationBehavior.ForceThenFallback, FaultActivationBehavior.Nondeterministic)]
				FaultActivationBehavior faultActivation)
		{
			var model = new ModelSetupParser().Parse($"Analysis/{modelFile}");
			IFaultSetHeuristic[] heuristics;
			switch (heuristicsUsage)
			{
				case HeuristicsUsage.None:
					heuristics = new IFaultSetHeuristic[0];
					break;
				case HeuristicsUsage.Subsumption:
					heuristics = new[] { new SubsumptionHeuristic(model) };
					break;
				case HeuristicsUsage.Redundancy:
					heuristics = new[] { RedundancyHeuristic(model) };
					break;
				default:
				case HeuristicsUsage.Both:
					heuristics = new[] { new SubsumptionHeuristic(model), RedundancyHeuristic(model) };
					break;
			}
			Dcca(model, faultActivation, heuristics);
		}

		private void Dcca(Model model, FaultActivationBehavior activation, params IFaultSetHeuristic[] heuristics)
		{
			var modelChecker = new SafetyAnalysis
			{
				Configuration =
				{
					StateCapacity = 1 << 16,
					CpuCount = 4,
					GenerateCounterExample = false
				}
			};

			modelChecker.Heuristics.AddRange(heuristics);
			modelChecker.FaultActivationBehavior = activation;

			var result = modelChecker.ComputeMinimalCriticalSets(model, model.ObserverController.Unsatisfiable);
			Console.WriteLine(result);
			Assert.AreEqual(0, result.Exceptions.Count);
		}

		private IFaultSetHeuristic RedundancyHeuristic(Model model)
		{
			return new MinimalRedundancyHeuristic(
				model,
				model.Stations.OfType<ContainerLoader>().Select(c => c.NoContainersLeft),
				model.Stations.OfType<ParticulateDispenser>().Select(d => d.BlueTankDepleted),
				model.Stations.OfType<ParticulateDispenser>().Select(d => d.RedTankDepleted),
				model.Stations.OfType<ParticulateDispenser>().Select(d => d.YellowTankDepleted),
				model.Stations.OfType<PalletisationStation>().Select(p => p.PalletisationDefect));
		}

		[Test]
		public void EnumerateAllStates()
		{
			var model = new ModelSetupParser().Parse("Analysis/medium_setup.model");
			model.Faults.SuppressActivations();

			var checker = new QualitativeChecker { Configuration = { StateCapacity = 1 << 18 } };
			var result = checker.CheckInvariant(model, true);

			foreach (var analysisResultExtension in result.Extensions)
			{
				Console.WriteLine(analysisResultExtension);
			}
		}

		[Test]
		public void ProductionCompletesIfNoFaultsOccur()
		{
			var model = Model.NoRedundancyCircularModel();
			var recipe = new Recipe(new[]
			{
				new Ingredient(IngredientType.BlueParticulate, 1),
				new Ingredient(IngredientType.RedParticulate, 3),
				new Ingredient(IngredientType.BlueParticulate, 1)
			}, 6);
			model.ScheduleProduction(recipe);
			model.Faults.SuppressActivations();

			var result = SafetySharpModelChecker.Check(model, F(recipe.ProcessingComplete));
			Assert.That(result.FormulaHolds, "Recipe production never finishes");
		}
	}
}
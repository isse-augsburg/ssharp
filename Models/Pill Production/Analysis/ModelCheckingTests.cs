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

namespace SafetySharp.CaseStudies.PillProduction.Analysis
{
	using System;
	using System.Collections;
	using System.Linq;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ModelChecking;
	using Modeling;
	using NUnit.Framework;
	using Runtime;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using static SafetySharp.Analysis.Operators;
	
	public class PillProductionModels
	{
		private static bool IsTestCaseFast(string model, ModelCheckingTests.HeuristicsUsage heuristicsUsage, FaultActivationBehavior faultActivation)
		{
			if (model == "complete_network.model" || model == "complex_setup.model" || model == "medium_setup.model")
			{
				return false;
			}
			if (model == "medium_setup.model" && heuristicsUsage!=ModelCheckingTests.HeuristicsUsage.None)
			{
				return true;
			}
			if (model == "simple_setup.model" || model == "duplicate_dispenser.model" || model == "trivial_setup.model" || model == "simple_circle.model")
			{
				return true;
			}
			if (faultActivation != FaultActivationBehavior.ForceOnly || heuristicsUsage == ModelCheckingTests.HeuristicsUsage.None)
				return false;
			return true;
		}

		public static string[] Models =
		{
			"complete_network.model", "bidirectional_circle.model", "duplicate_dispenser.model", "simple_circle.model", "trivial_setup.model",
			"simple_setup.model", "simple_setup3.model", "simple_setup2.model", "medium_setup.model", "complex_setup.model"
		};

		public static ModelCheckingTests.HeuristicsUsage[] Heuristics =
		{
			ModelCheckingTests.HeuristicsUsage.None, ModelCheckingTests.HeuristicsUsage.Subsumption, ModelCheckingTests.HeuristicsUsage.Redundancy, ModelCheckingTests.HeuristicsUsage.Both
		};

		public static FaultActivationBehavior[] FaultBehavior =
		{
			FaultActivationBehavior.ForceOnly, FaultActivationBehavior.ForceThenFallback, FaultActivationBehavior.Nondeterministic
		};


		public static IEnumerable TestCases
		{
			get
			{
				foreach (var model in Models)
				{
					foreach (var heuristicsUsage in Heuristics)
					{
						foreach (var faultActivationBehavior in FaultBehavior)
						{
							var testCaseData = new TestCaseData(model, heuristicsUsage, faultActivationBehavior);
							if (IsTestCaseFast(model, heuristicsUsage, faultActivationBehavior))
								yield return testCaseData;
							else
								yield return testCaseData.Ignore("Model Checking slow");
						}
					}
				}
			}
		}
	}

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
		
		[Test, TestCaseSource(typeof(PillProductionModels), nameof(PillProductionModels.TestCases))]
		public void DccaTest(string modelFile, HeuristicsUsage heuristicsUsage, FaultActivationBehavior faultActivation)
		{
			var model = new ModelSetupParser().Parse($"Analysis/{modelFile}");
			IFaultSetHeuristic[] heuristics;
			switch (heuristicsUsage)
			{
				case HeuristicsUsage.None:
					heuristics = new IFaultSetHeuristic[0];
					break;
				case HeuristicsUsage.Subsumption:
					heuristics = new[] { new SubsumptionHeuristic(model.Faults) };
					break;
				case HeuristicsUsage.Redundancy:
					heuristics = new[] { RedundancyHeuristic(model) };
					break;
				default:
				case HeuristicsUsage.Both:
					heuristics = new[] { new SubsumptionHeuristic(model.Faults), RedundancyHeuristic(model) };
					break;
			}
			Dcca(model, faultActivation, heuristics);
		}
		

		private void Dcca(Model model, FaultActivationBehavior activation, params IFaultSetHeuristic[] heuristics)
		{
			var modelChecker = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					ModelCapacity = new ModelCapacityByModelSize(1 << 16, ModelDensityLimit.Medium),
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
				model.Faults,
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

			var checker = new SafetySharpQualitativeChecker { Configuration = {
					ModelCapacity = new ModelCapacityByModelSize(1 << 18, ModelDensityLimit.Medium)}
			};
			var result = checker.CheckInvariant(model, true);
			
			Console.WriteLine(result);
		}

		[Test,Ignore("Doesn't work")]
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
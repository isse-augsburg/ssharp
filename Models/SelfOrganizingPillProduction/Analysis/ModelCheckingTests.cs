using NUnit.Framework;
using System.Linq;
using SafetySharp.Analysis;
using SafetySharp.Modeling;
using SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling;
using SafetySharp.Analysis.Heuristics;
using static SafetySharp.Analysis.Operators;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Analysis
{
    public class ModelCheckingTests
    {
        [Test]
        public void SimpleDcca()
        {
            var model = new ModelSetupParser().Parse("Analysis/simple_setup.model");
            Dcca(model, new SubsumptionHeuristic(model), RedundancyHeuristic(model));
        }

        [Test]
        public void SimpleDccaWithoutHeuristics()
        {
            Dcca(new ModelSetupParser().Parse("Analysis/simple_setup.model"));
        }

        [Test]
        public void MediumDcca()
        {
            var model = new ModelSetupParser().Parse("Analysis/medium_setup.model");
            Dcca(model, new SubsumptionHeuristic(model), RedundancyHeuristic(model));
        }

        [Test]
        public void MediumDccaWithoutHeuristics()
        {
            Dcca(new ModelSetupParser().Parse("Analysis/medium_setup.model"));
        }

        [Test]
        public void ComplexDcca()
        {
            var model = new ModelSetupParser().Parse("Analysis/complex_setup.model");
            Dcca(model, new SubsumptionHeuristic(model), RedundancyHeuristic(model));
        }

        [Test]
        public void ComplexDccaWithoutHeuristics()
        {
            Dcca(new ModelSetupParser().Parse("Analysis/complex_setup.model"));
        }

        private void Dcca(Model model, params IFaultSetHeuristic[] heuristics)
        {
            var modelChecker = new SafetyAnalysis();

            modelChecker.Configuration.StateCapacity = 40000;
            modelChecker.Configuration.CpuCount = 1;
            modelChecker.Heuristics.AddRange(heuristics);
            modelChecker.FaultActivationBehaviour = FaultActivationBehaviour.ForceOnly;

            var result = modelChecker.ComputeMinimalCriticalSets(model, model.ObserverController.Unsatisfiable);
            System.Console.WriteLine(result);
        }

        private IFaultSetHeuristic RedundancyHeuristic(Model model)
        {
            return new MinimalRedundancyHeuristic(
                model,
                model.Stations.OfType<ContainerLoader>().Select(c => c.NoContainersLeft),
                model.Stations.OfType<ParticulateDispenser>().Select(d => d.BlueTankDepleted),
                model.Stations.OfType<ParticulateDispenser>().Select(d => d.RedTankDepleted),
                model.Stations.OfType<ParticulateDispenser>().Select(d => d.YellowTankDepleted),
                model.Stations.OfType<PalletisationStation>().Select(p => p.PalletisationDefect)
            );
        }

        [Test]
        public void EnumerateAllStates()
        {
            var model = new ModelSetupParser().Parse("Analysis/medium_setup.model");
            model.Faults.SuppressActivations();

            var checker = new SSharpChecker { Configuration = { StateCapacity = 1 << 18 } };
            var result = checker.CheckInvariant(model, true);

            System.Console.WriteLine(result.StateVectorLayout);
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

            var invariant = ((Formula) !recipe.ProcessingComplete).Implies(F(recipe.ProcessingComplete));
            var result = ModelChecker.Check(model, invariant);
            Assert.That(result.FormulaHolds, "Recipe production never finishes");
        }
    }
}
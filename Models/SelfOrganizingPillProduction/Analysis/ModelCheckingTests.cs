using NUnit.Framework;
using SafetySharp.Analysis;
using SafetySharp.Modeling;
using SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling;
using static SafetySharp.Analysis.Operators;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Analysis
{
    public class ModelCheckingTests
    {
        [Test]
        public void Dcca()
        {
            //var model = Model.NoRedundancyCircularModel();
            var model = new ModelSetupParser().Parse("Analysis/medium_setup.model");

            var modelChecker = new SafetyAnalysis();
            modelChecker.Configuration.StateCapacity = 20000;

            var result = modelChecker.ComputeMinimalCriticalSets(model, model.ObserverController.Unsatisfiable);
            System.Console.WriteLine(result);
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

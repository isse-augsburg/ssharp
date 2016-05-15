using NUnit.Framework;
using SafetySharp.Analysis;
using SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Analysis
{
    public class ModelCheckingTests
    {
        [Test]
        public void Dcca()
        {
            var model = Model.NoRedundancyCircularModel();

            var modelChecker = new SafetyAnalysis();
            modelChecker.Configuration.StateCapacity = 20000;
            modelChecker.Configuration.CpuCount = 1;

            var result = modelChecker.ComputeMinimalCriticalSets(model, model.ObserverController.Unsatisfiable);
            System.Console.WriteLine(result);
        }
    }
}

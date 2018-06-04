using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetyLustre;
using System.Collections.Generic;
using System.IO;
using static Lustre_Models.TestUtil;
using static SafetyLustre.LustreSafetyAnalysis;

namespace Lustre_Models
{
    public class ModelCheckingTests
    {
        [Test]
        public void TankRupture()
        {
            Formula invariant = new LustrePressureBelowThreshold();
            LustrePressureBelowThreshold.threshold = 60;
            var faults = new List<Fault>
            {
                new TransientFault() { Name = "fault_switch", Identifier = 0, ProbabilityOfOccurrence = new Probability(3.0E-6) },
                new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) },
                new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) },
                new PermanentFault() { Name = "fault_timer", Identifier = 3, ProbabilityOfOccurrence = new Probability(1.0E-5) },
                new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) }
            };
            var modelChecker = new LustreQualitativeChecker(Path.Combine(AssemblyDirectory, "pressureTank.lus"), "TANK", faults, invariant);

            modelChecker.CheckInvariant(invariant, 100);
        }
    }
}

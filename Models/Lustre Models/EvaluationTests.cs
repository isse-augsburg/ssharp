using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

namespace Lustre_Models
{
    class EvaluationTests
    {
        private ExecutableModelCreator<LustreExecutableModel> _createModel;

        private Formula _invariant = new LustrePressureBelowThreshold();

        private Formula _hazard = new UnaryFormula(new LustrePressureBelowThreshold(), UnaryOperator.Not);

        private Fault[] _faults;

        public EvaluationTests()
        {
            Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";
            Program.modelChecking = true;
            LustrePressureBelowThreshold.threshold = 60;
            var faultK1 = new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) };
            var faultK2 = new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) };
            var faultSensor = new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) };
            _faults = new[] { faultK1, faultK2, faultSensor };

            _createModel = LustreExecutableModel.CreateExecutedModelFromFormulasCreator("pressureTank", _faults);
        }


        [Test]
        public void CreateMarkovChainWithFalseFormula()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(new BinaryFormula(_invariant, BinaryOperator.And,
                new UnaryFormula(_invariant, UnaryOperator.Not)));
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateMarkovChain();
        }

        [Test]
        public void CreateMarkovChainWithHazards()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateMarkovChain();
        }

        [Test]
        public void CreateMarkovChainWithHazardsWithoutStaticPruning()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateMarkovChain();
        }

        [Test]
        public void CreateMarkovChainWithHazardRetraversal1()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
            var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

            var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
            retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
            retraversalMarkovChainGenerator.AddFormulaToCheck(_hazard);
            retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
            retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
            retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
        }

        [Test]
        public void CreateMarkovChainWithHazardsRetraversal2()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
            var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

            var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
            retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
            retraversalMarkovChainGenerator.AddFormulaToCheck(_hazard);
            retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
            retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
            retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
        }


        [Test]
        public void CreateMarkovChainWithHazardFaultsInState()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
                markovChainGenerator.AddFormulaToPlainlyIntegrateIntoStateSpace(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateMarkovChain();
        }


        [Test]
        public void CreateFaultAwareMarkovChainAllFaults()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateMarkovChain();
        }

        [Test]
        public void CalculateHazardSingleCore()
        {
            LustreModelChecker.TraversalConfiguration.CpuCount = 1;
            LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
            LustreModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = false;
            var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
            LustreModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
            LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
            LustreModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = true;
            Console.Write($"Probability of hazard: {result}");
        }

        [Test]
        public void CalculateHazardSingleCoreAllFaults()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            var formulaToCheck = new BoundedUnaryFormula(_hazard, UnaryOperator.Finally, 25);
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

            using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
            {
                var result = modelChecker.CalculateProbability(formulaToCheck);
                Console.Write($"Probability of formulaToCheck: {result}");
            }
        }

        [Test]
        public void CalculateHazardSingleCoreAllFaultsWithOnce()
        {
            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            var onceFormula = new UnaryFormula(_hazard, UnaryOperator.Once);
            var onceFault1 = new UnaryFormula(new FaultFormula(_faults[1]), UnaryOperator.Once);
            markovChainGenerator.AddFormulaToCheck(onceFault1);
            foreach (var fault in _faults.Where(fault => fault != _faults[1]))
            {
                var faultFormula = new UnaryFormula(new FaultFormula(fault), UnaryOperator.Once);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            var formulaToCheck = new BoundedUnaryFormula(new BinaryFormula(onceFault1, BinaryOperator.And, onceFormula), UnaryOperator.Finally, 25);
            markovChainGenerator.AddFormulaToCheck(formulaToCheck);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

            using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
            {
                var result = modelChecker.CalculateProbability(formulaToCheck);
                Console.Write($"Probability of formulaToCheck: {result}");
            }
        }



        [Test]
        public void CalculateHazardWithoutEarlyTermination()
        {
            LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
            var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
            LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
            Console.Write($"Probability of hazard: {result}");
        }


        [Test]
        public void CalculateLtmdpWithoutFaultsWithPruning()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }

        [Test]
        public void CalculateLtmdpWithoutStaticPruning()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }

        [Test]
        public void CalculateLtmdpWithoutStaticPruningSingleCore()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            var formulaToCheck = new BoundedUnaryFormula(_hazard, UnaryOperator.Finally, 25);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            markovChainGenerator.Configuration.CpuCount = 1;
            var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
            _faults[1].ProbabilityOfOccurrence = oldProbability;

            using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
            {
                var result = modelChecker.CalculateProbabilityRange(formulaToCheck);
                Console.Write($"Probability of formulaToCheck: {result}");
            }
        }

        [Test]
        public void CalculateMdpNewStates()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }

        [Test]
        public void CalculateMdpNewStatesConstant()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStatesConstantDistance;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }


        [Test]
        public void CalculateMdpFlattened()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithFlattening;
            markovChainGenerator.AddFormulaToCheck(_hazard);

            foreach (var fault in _faults)
            {
                var faultFormula = new FaultFormula(fault);
                markovChainGenerator.AddFormulaToCheck(faultFormula);
            }
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }

        [Test]
        public void CalculateMdpNewStatesWithoutFaults()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }

        [Test]
        public void CalculateMdpNewStatesConstantWithoutFaults()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStatesConstantDistance;
            markovChainGenerator.AddFormulaToCheck(_hazard);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }


        [Test]
        public void CalculateMdpFlattenedWithoutFaults()
        {
            var oldProbability = _faults[1].ProbabilityOfOccurrence;
            _faults[1].ProbabilityOfOccurrence = null;
            var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(_createModel);
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
            markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithFlattening;
            markovChainGenerator.AddFormulaToCheck(_hazard);

            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            markovChainGenerator.Configuration.EnableEarlyTermination = false;
            var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

            var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
            _faults[1].ProbabilityOfOccurrence = oldProbability;
        }
    }
}

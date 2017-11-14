namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Analysis;
    using ISSE.SafetyChecking.Formula;
    using ISSE.SafetyChecking.Modeling;
    using Modeling;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for bayesian network structure learning by using model selection techniques.
    /// </summary>
    class ScoreBasedStructureLearner
    {

        private readonly ModelBase _model;
        private readonly int _stepBounds;
        private int _numberOfSimulations;

        private ICollection<FaultRandomVariable> _faults;
        private ICollection<McsRandomVariable> _mcs;
        private ICollection<BooleanRandomVariable> _states;
        private BooleanRandomVariable _hazard;

        private const string RPath = "Rscript.exe";
        // TODO: define this path correctly relative to the project
        private const string ScriptPath = @"C:\Users\frits\Documents\Visual Studio 2015\Projects\ssharp\Source\SafetySharp\Bayesian\";
        //TODO: define this path for generation data correctly
        private const string Path = @"D:\Sonstiges\SafetySharpSimulation\";

        private const string RScript = ScriptPath + "StructureLearning.R";
        private const string DataPath = Path + "simulationData.csv";
        private const string WhitelistPath = Path + "whitelist.csv";
        private const string BlacklistPath = Path + "blacklist.csv";

        /// <summary>
        /// Creates a new ScoreBasedStructureLearner instance
        /// </summary>
        /// <param name="model">The model for which a bayesian network will be learned</param>
        /// <param name="stepBounds">The maximal number of steps for a trace in a simulation step</param>
        public ScoreBasedStructureLearner(ModelBase model, int stepBounds)
        {
            _model = model;
            _stepBounds = stepBounds;
        }

        /// <summary>
        /// Learn the best fitting DAG structure and probability distributions by using simulation data
        /// </summary>
        public BayesianNetwork LearnBayesianNetwork(ICollection<FaultRandomVariable> faults, ICollection<McsRandomVariable> mcs,
                                                    ICollection<BooleanRandomVariable> states, BooleanRandomVariable hazard, int numberOfSimulations)
        {
            _faults = faults;
            _mcs = mcs;
            _hazard = hazard;
            _states = states;
            _numberOfSimulations = numberOfSimulations;

            Console.Out.WriteLine("Create simulation data...");
            CreateSimulationData();
            Console.Out.WriteLine("Generate data for reducing model domain...");
            CreateWhiteAndBlacklist();
            Console.Out.WriteLine("Call R script for learning the network...");
            var jsonResult = CallRLearningScript();
            var result = JsonConvert.DeserializeObject<BayesianNetworkResult>(jsonResult);

            return ToBayesianNetwork(result);
        }

        /// <summary>
        /// Simulates the model and writes the results as a CSV file. Checks the occurence of every random variable.
        /// </summary>
        private void CreateSimulationData()
        {
            var allVariables = AllRandomVariables();
            var allFormulas = CreateAllFormulas();
            SafetySharpProbabilisticSimulator.Configuration.UseOptionProbabilitiesInSimulation = BayesianNetworkCreator.Config.UseRealProbabilitiesForSimulation;
            var simulator = new SafetySharpProbabilisticSimulator(_model, allFormulas.Values.ToArray());

            using (var w = new StreamWriter(DataPath))
            {
                w.WriteLine(string.Join(",", allVariables.Select(randomVariable => randomVariable.Name)));
                for (var currentStep = 0; currentStep < _numberOfSimulations; currentStep++)
                {
                    if (_numberOfSimulations > 100 && currentStep % (_numberOfSimulations / 100) == 0)
                    {
                        Console.Out.WriteLine($"{(double)currentStep / _numberOfSimulations:P0} done.");
                    }

                    simulator.SimulateSteps(_stepBounds);
                    var results = new bool[allVariables.Count];
                    for (var varIndex = 0; varIndex < allVariables.Count; varIndex++)
                    {
                        var currentVariable = allVariables[varIndex];
                        // cut sets cannot be checked for a given state, so check the occurence of its faults
                        if (currentVariable is McsRandomVariable)
                        {
                            var cutSet = (McsRandomVariable)currentVariable;
                            results[varIndex] =
                                cutSet.FaultVariables.All(fault => simulator.GetCountOfSatisfiedOnTrace(allFormulas[fault]) > 0);
                        }
                        // check the occurence of the random variable
                        else
                        {
                            results[varIndex] = simulator.GetCountOfSatisfiedOnTrace(allFormulas[currentVariable]) > 0;
                        }
                    }
                    w.WriteLine(string.Join(",", results.Select(res => res ? 'T' : 'F')));
                }
                w.Flush();
            }
        }

        private void CreateWhiteAndBlacklist()
        {
            using (StreamWriter blacklist = new StreamWriter(BlacklistPath), whitelist = new StreamWriter(WhitelistPath))
            {
                whitelist.WriteLine("From,To");
                blacklist.WriteLine("From,To");
                // Arcs that have to be in the model (whitelist):
                //      - faults to the minimal critical sets where they are contained in
                //      - minimal critical sets to hazard
                // Arcs that must not be in the model (blacklist):
                //      - hazard to all faults and minimal critical sets
                //      - faults to minimal critical sets where they are not contained in and the other way
                foreach (var faultVar in _faults)
                {
                    blacklist.WriteLine($"{_hazard.Name},{faultVar.Name}");
                }
                foreach (var criticalSet in _mcs)
                {
                    blacklist.WriteLine($"{_hazard.Name},{criticalSet.Name}");
                }
                if (!BayesianNetworkCreator.Config.UseDccaResultsForLearning)
                {
                    return;
                }
                foreach (var criticalSet in _mcs)
                {
                    foreach (var faultVar in criticalSet.Reference.Faults)
                    {
                        whitelist.WriteLine($"{faultVar.Name},{criticalSet.Name}");
                    }
                    whitelist.WriteLine($"{criticalSet.Name},{_hazard.Name}");
                    foreach (var faultVar in _faults.Where(f => !criticalSet.Reference.Faults.Contains(f.Reference)))
                    {
                        blacklist.WriteLine($"{faultVar.Name},{criticalSet.Name}");
                        blacklist.WriteLine($"{criticalSet.Name},{faultVar.Name}");
                    }
                }
                blacklist.Flush();
                whitelist.Flush();
            }
        }

        private static string CallRLearningScript()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = RPath,
                Arguments = $"\"{RScript}\" \"{DataPath}\" \"{WhitelistPath}\" \"{BlacklistPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            string output;
            string errorOutput;
            using (var process = Process.Start(startInfo))
            {
                output = process.StandardOutput.ReadToEnd();
                errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            Console.Out.WriteLine(errorOutput);
            Console.Out.WriteLine(output);
            return output.Split(new[] { "RESULTING DAG" }, StringSplitOptions.None)[1];
        }

        private BayesianNetwork ToBayesianNetwork(BayesianNetworkResult result)
        {
            var allVariables = MapToAllVariables();
            var nodes = result.Nodes.Select(node => allVariables[node]).ToList();
            var dag = DagPattern<RandomVariable>.InitEmptyDag(nodes);
            foreach (var arc in result.Arcs)
            {
                dag.AddEdge(allVariables[arc.From], allVariables[arc.To]);
            }

            var distributions = new List<ProbabilityDistribution>();
            foreach (var probTable in result.ProbTables)
            {
                var rvar = allVariables[probTable.Rvar.First()];
                var conditions = probTable.Conditions.Select(condition => allVariables[condition]).ToList();
                var probabilities = ToProbabilityArray(probTable.Probs, conditions.Count + 1);
                distributions.Add(new ProbabilityDistribution(rvar, conditions, probabilities));
            }

            return BayesianNetwork.FromDagPattern(dag, distributions);
        }

        private static IList<Probability> ToProbabilityArray(IList<double> probs, int numberOfVariables)
        {
            //R iterates the variables from left to right, and first 'false' then 'true',
            // so flip the binary index representation and revert it to get the 'real' index
            var size = 1 << numberOfVariables;
            var probabilities = new Probability[size];
            for (var index = 0; index < size; index++)
            {
                var indexBits = Convert.ToString(index, 2).PadLeft(numberOfVariables, '0');
                var flipped = indexBits.Select(bit => bit == '1' ? '0' : '1').Reverse().ToArray();
                var realIndex = Convert.ToInt32(new string(flipped), 2);
                probabilities[realIndex] = new Probability(probs[index]);
            }
            return probabilities;
        }

        private Dictionary<string, RandomVariable> MapToAllVariables()
        {
            var dictionary = new Dictionary<string, RandomVariable>();
            foreach (var fault in _faults)
            {
                dictionary[fault.Name] = fault;
            }
            foreach (var cutSet in _mcs)
            {
                dictionary[cutSet.Name] = cutSet;
            }
            foreach (var state in _states)
            {
                dictionary[state.Name] = state;
            }
            dictionary[_hazard.Name] = _hazard;
            return dictionary;
        }

        private IList<RandomVariable> AllRandomVariables()
        {
            var allVariables = new List<RandomVariable>();
            allVariables.AddRange(_faults);
            allVariables.AddRange(_mcs);
            allVariables.AddRange(_states);
            allVariables.Add(_hazard);
            return allVariables;
        }

        private Dictionary<RandomVariable, Formula> CreateAllFormulas()
        {
            var allFormulas = new Dictionary<RandomVariable, Formula>();
            foreach (var faultVar in _faults)
            {
                allFormulas[faultVar] = faultVar.ToFormula();
            }
            foreach (var state in _states)
            {
                allFormulas[state] = state.ToFormula();
            }
            allFormulas[_hazard] = _hazard.ToFormula();
            return allFormulas;
        }
    }

    class BayesianNetworkResult
    {
        public List<string> Nodes { get; set; }
        public List<BayesianNetworkResultArc> Arcs { get; set; }
        public List<BayesianNetworkResultProb> ProbTables { get; set; }
    }

    class BayesianNetworkResultArc
    {
        public string From { get; set; }
        public string To { get; set; }
    }

    class BayesianNetworkResultProb
    {
        public string[] Rvar { get; set; }
        public string[] Conditions { get; set; }
        public double[] Probs { get; set; }
    }
}

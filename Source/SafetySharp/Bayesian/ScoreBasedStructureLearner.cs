// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Stefan Fritsch
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

namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ISSE.SafetyChecking.Modeling;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for bayesian network structure learning by using model selection techniques.
    /// </summary>
    class ScoreBasedStructureLearner
    {

        private ICollection<FaultRandomVariable> _faults;
        private ICollection<McsRandomVariable> _mcs;
        private ICollection<BooleanRandomVariable> _states;
        private BooleanRandomVariable _hazard;
        private readonly BayesianLearningConfiguration _config;

        private readonly IList<string> _dataFiles;
        private readonly string _path;

        private const string RPath = "Rscript.exe";
        // TODO: define this path correctly relative to the project
        private const string ScriptPath = @"ssharp\Source\SafetySharp\Bayesian\";

        private const string RScript = ScriptPath + "StructureLearning.R";
        private string WhitelistPath => _path + "whitelist.csv";
        private string BlacklistPath => _path + "blacklist.csv";

        /// <summary>
        /// Creates a new ScoreBasedStructureLearner instance
        /// </summary>
        /// <param name="dataFiles">Path to the data files</param>
        /// <param name="pathToUse">Path where further files are created</param>
        /// <param name="config">BayesianLearningConfiguration for further optional settings</param>
        public ScoreBasedStructureLearner(IList<string> dataFiles, string pathToUse, BayesianLearningConfiguration? config = null)
        {
            _dataFiles = dataFiles;
            _path = pathToUse;
            _config = config ?? BayesianLearningConfiguration.Default;
        }

        /// <summary>
        /// Learn the best fitting DAG structure and probability distributions by using simulation data
        /// </summary>
        public BayesianNetwork LearnBayesianNetwork(ICollection<FaultRandomVariable> faults, ICollection<McsRandomVariable> mcs,
                                                    ICollection<BooleanRandomVariable> states, BooleanRandomVariable hazard)
        {
            _faults = faults;
            _mcs = mcs;
            _hazard = hazard;
            _states = states;

            Console.Out.WriteLine("Generate data for reducing model domain...");
            CreateWhiteAndBlacklist();
            Console.Out.WriteLine("Call R script for learning the network...");
            var jsonResult = CallRLearningScript();
            var result = JsonConvert.DeserializeObject<BayesianNetworkResult>(jsonResult);

            return ToBayesianNetwork(result);
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
                if (!_config.UseDccaResultsForLearning)
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

        private string CallRLearningScript()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = RPath,
                Arguments = $"\"{RScript}\" \"{WhitelistPath}\" \"{BlacklistPath}\" \"{string.Join("\" \"", _dataFiles)}\"",
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

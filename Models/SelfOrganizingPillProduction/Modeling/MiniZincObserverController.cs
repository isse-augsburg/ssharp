using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    class MiniZincObserverController : ObserverController
    {
        private static readonly int numIngredients = Enum.GetValues(typeof(IngredientType)).Length;

        private const string DataFile = "data.dzn";
        private const string SolutionFile = "s.sol";
        private const string MinizincExe = "minizinc.exe";
        private const string MinizincModel = "ConfigurationConstraints.mzn";

        public MiniZincObserverController(params Station[] stations) : base(stations) { }

        public override void Configure(Recipe recipe)
        {
            RemoveObsoleteConfiguration(recipe);

            // Can't do anything without stations
            // (MiniZinc model expects at least one station, thus handle this case here)
            if (AvailableStations.Length == 0)
            {
                Unsatisfiable = true;
                return;
            }

            CreateDznFile(recipe);
            ExecuteMiniZinc();
            var solution = ReadSolution();

            if (!Unsatisfiable)
            {
                ApplyConfiguration(recipe, solution);
            }
        }

        private void RemoveObsoleteConfiguration(Recipe recipe)
        {
            foreach (var station in AvailableStations)
            {
                var obsoleteRoles = (from role in station.AllocatedRoles where role.Recipe == recipe select role)
                    .ToArray();
                foreach (var role in obsoleteRoles)
                {
                    station.AllocatedRoles.Remove(role);
                }
                RolePool.Return(obsoleteRoles);
            }
        }

        private void CreateDznFile(Recipe recipe)
        {
            var recipe_data = RecipeToCapabilitySequence(recipe);

            StringBuilder capabilities = new StringBuilder();
            StringBuilder connections = new StringBuilder();

            foreach (var station in AvailableStations)
            {
                capabilities.Append(String.Join(",", ExtractCapabilityAmounts(station.AvailableCapabilities)));
                capabilities.Append(",\n|");

                connections.Append("|");
                foreach (var other in AvailableStations)
                {
                    connections.Append(station.Outputs.Contains(other).ToString());
                    connections.Append(",");
                }
            }

            string data =
$@"noCapabilities = {numIngredients + 2};
task = [{ string.Join(",", recipe_data.Item1) }];
task_amount = [{ string.Join(",", recipe_data.Item2) }];

noAgents = { AvailableStations.Length };
capabilities = [|{ capabilities }];
isConnected = [{ connections.ToString().ToLower() }|];
";
            File.WriteAllText(DataFile, data);
        }

        private void ExecuteMiniZinc()
        {
            //Console.WriteLine("Executing Minizinc");

            var startInfo = new ProcessStartInfo
            {
                FileName = MinizincExe,
                Arguments = "-o " + SolutionFile + " " + MinizincModel + " " + DataFile,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            //process.OutputDataReceived += (o, e) => Console.WriteLine(e.Data);
            //process.ErrorDataReceived += (o, e) => Console.WriteLine(e.Data);

            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception("MiniZinc reconfiguration failed");;
        }

        private Tuple<int[], int[]> ReadSolution()
        {
            var lines = File.ReadAllLines(SolutionFile);
            var agentsData = new int[0];
            var workedOnData = new int[0];

            foreach (var line in lines)
            {
                if (line.Contains("UNSATISFIABLE"))
                {
                    Unsatisfiable = true;
                    break;
                }
                if (line.StartsWith("-"))
                {
                    break;
                }

                var splitted = line.Split('=');
                if (splitted[0].Contains("agents"))
                {
                    agentsData = ParseArray(splitted[1]);
                }
                else if (splitted[0].Contains("workedOn"))
                {
                    workedOnData = ParseArray(splitted[1]);
                }
            }
            return new Tuple<int[], int[]>(agentsData, workedOnData);
        }

        private void ApplyConfiguration(Recipe recipe, Tuple<int[], int[]> configuration)
        {
            var agents = configuration.Item1;
            var workedOn = configuration.Item2;

            Station lastAgent = null;
            Role lastRole = null;

            for (int i = 0; i < agents.Length; i++)
            {
                var agent = AvailableStations[agents[i] - 1];

                Role role = lastRole;
                if (agent != lastAgent)
                {
                    role = RolePool.Allocate();
                    agent.AllocatedRoles.Add(role);

                    role.CapabilitiesToApply.Clear();

                    // update precondition
                    role.PreCondition.Recipe = recipe;
                    role.PreCondition.State.Clear();
                    role.PreCondition.Port = lastAgent;

                    if (lastRole != null)
                    {
                        lastRole.PostCondition.Port = agent;
                        role.PreCondition.State.AddRange(lastRole.PostCondition.State);
                    }

                    // update postcondition
                    role.PostCondition.Recipe = recipe;
                    role.PostCondition.State.Clear();
                    role.PostCondition.State.AddRange(role.PreCondition.State);
                }

                if (workedOn[i] > 0)
                {
                    var cap = recipe.RequiredCapabilities[workedOn[i] - 1];
                    role.CapabilitiesToApply.Add(cap);
                    role.PostCondition.State.Add(cap);
                }

                lastAgent = agent;
                lastRole = role;
            }

            if (lastRole != null)
            {
                lastRole.PostCondition.Port = null; // last role must include consume capability -> no output
            }
                
        }

        #region helper methods

        private int[] ParseArray(string input)
        {
            var trimChars = new[] { ' ', '[', ']' };
            return Array.ConvertAll(input.Trim(trimChars).Split(','), int.Parse);
        }

        private uint[] ExtractCapabilityAmounts(Capability[] capabilities)
        {
            var amounts = new uint[numIngredients + 2];
            ProcessCapabilities(capabilities, (cap, amount) => amounts[cap] = amount);
            return amounts;
        }

        private Tuple<int[], uint[]> RecipeToCapabilitySequence(Recipe recipe)
        {
            int[] capabilities = new int[recipe.RequiredCapabilities.Length];
            uint[] amounts = new uint[recipe.RequiredCapabilities.Length];

            int i = 0;
            ProcessCapabilities(recipe.RequiredCapabilities, (cap, amount) => {
                capabilities[i] = cap + 1; // capabilities in mzn are 1-based
                amounts[i] = amount;
                i++;
            });

            return Tuple.Create(capabilities, amounts);
        }

        /// <summary>
        /// Converts capabilities into a (int type, uint amount) pair
        /// and passes it to a caller-defined processing routine.
        /// </summary>
        private void ProcessCapabilities(Capability[] capabilities, Action<int, uint> processing)
        {
            var numIngredients = Enum.GetValues(typeof(IngredientType)).Length;
            int produce_capability = numIngredients, consume_capability = numIngredients + 1;

            for (int i = 0; i < capabilities.Length; ++i)
            {
                int capability_code;
                uint amount = 1u;
                if (capabilities[i] is ProduceCapability)
                {
                    capability_code = produce_capability;
                }
                else if (capabilities[i] is ConsumeCapability)
                {
                    capability_code = consume_capability;
                }
                else // if (capabilities[i] is Ingredient)
                {
                    var ingredient = capabilities[i] as Ingredient;
                    capability_code = (int)ingredient.Type;
                    amount = ingredient.Amount;
                }
                processing.Invoke(capability_code, amount);
            }
        }

        #endregion
    }
}

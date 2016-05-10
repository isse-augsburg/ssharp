using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ProductionCell
{
    class MiniZincObserverController : ObserverController
    {
        const String DataFile = "data.dzn";
        const String SolutionFile = "s.sol";
        const String MinizincExe = "minizinc.exe";
        const String MinizincModel = "../../rolealloc-compact.mzn";

        public override void Reconfigure()
        {
//            CreateDznFile(new[]
//            {
//                Tuple.Create("task", "[D,I,T]"),
//                Tuple.Create("noAgents", "4"),
//                Tuple.Create("capabilities", "[{D,I}, {I,T}, {}, {}]"),
//                Tuple.Create("isCart", "[false, false, true, true]")
//            });

            CreateDznFileFromCurrentConfig();
            ExecuteMinizinc();

            EnroleConfiguration(ReadVar(SolutionFile));


        }

        private void EnroleConfiguration(Tuple<int[], int[]> varVals)
        {
            Console.WriteLine("Applying roles...");

            var agents = varVals.Item1;
            var workedOn = varVals.Item2;
            foreach (var agent in Agents)
            {
                RolePool.AddRange(agent.AllocatedRoles);
                agent.AllocatedRoles.Clear();
            }

            for (int i = 0; i < agents.Length; i++)
            {
                OdpRole roleToAllocate = RolePool.First();
                RolePool.Remove(roleToAllocate);
                roleToAllocate.CapabilitiesToApply.Clear();
                roleToAllocate.CapabilitiesToApply.Add(Agents[agents[i]-1].AvailableCapabilites[workedOn[i]-1]);

                Agents[agents[i]-1].AllocatedRoles.Add(roleToAllocate);

                Console.WriteLine($"Agent {agents[i] - 1}: Add capability {Agents[agents[i] - 1].AvailableCapabilites[workedOn[i] - 1]}");
            }
        }

        private static void ExecuteMinizinc()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = MinizincExe,
                Arguments = "-o " + SolutionFile + " " + MinizincModel + " " + DataFile,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process {StartInfo = startInfo, EnableRaisingEvents = true};
            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.OutputDataReceived += (o, e) => Console.WriteLine(e.Data);
            process.ErrorDataReceived += (o, e) => Console.WriteLine(e.Data);

            process.WaitForExit();
        }

        private void CreateDznFileFromCurrentConfig()
        {
            List<string> capabilities = new List<string>(Agents.Count);
            List<bool> isCart = new List<bool>(Agents.Count);
            foreach (var agent in Agents)
            {
                capabilities.Add("{"+string.Join(",",agent.AvailableCapabilites)+"}");
                isCart.Add(agent.IsCart);
            }
            
            
            IEnumerable<Tuple<String, String>> dnzVars = new List<Tuple<string, string>>() {
                new Tuple<string, string>("task", "[" + string.Join(",", CurrentTasks.GetTaskAsStrings()) + "]"),
                new Tuple<string, string>("noAgents", Agents.Count().ToString()),
                new Tuple<string, string>("capabilities", "[" + string.Join(",", capabilities.ToArray()) + "]"),
                new Tuple<string, string>("isCart", "[" + string.Join(",", isCart.ToArray()).ToLower() + "]")
            };
            CreateDznFile(dnzVars);

        }

        private void CreateDznFile(IEnumerable<Tuple<String, String>> data)
        {
            List<String> lines = new List<string>();
            foreach (var d in data)
            {
                lines.Add($"{d.Item1} = {d.Item2};");
            }
            System.IO.File.WriteAllLines(DataFile, lines);
        }

        private Tuple<int[], int[]> ReadVar(String file)
        {
            var lines = File.ReadAllLines(file);
            var agentsData = new int[] {};
            var workedOnData = new int[] {};
            foreach (var line in lines)
            {
                if (line.StartsWith("-"))
                {
                    break;
                }
                if (line.Split('=')[0].Contains("agents"))
                {
                    var splittedSolutionString = line.Split('=')[1].Trim(new[] {' ', '[', ']'});
                    agentsData = Array.ConvertAll(splittedSolutionString.Split(','), s => int.Parse(s));
                }

                if (!line.Split('=')[0].Contains("workedOn")) continue;
                {
                    var splittedSolutionString = line.Split('=')[1].Trim(new[] {' ', '[', ']'});
                    workedOnData = Array.ConvertAll(splittedSolutionString.Split(','), s => int.Parse(s));
                }
            }
            return new Tuple<int[], int[]>(agentsData, workedOnData);
        }



        [Test]
        public void Test()
        {
            MiniZincObserverController controller = new MiniZincObserverController();
            controller.Reconfigure();
        }

    }
}

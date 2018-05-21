using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.Oc5Compiler
{
    /// <summary>
    /// This requires Windows Subsystem for Linux
    /// To install run
    ///     lxrun.exe /install
    /// For more information visit https://docs.microsoft.com/en-us/windows/wsl/install-win10
    /// This was tested against the Ubuntu distribution
    /// </summary>
    public class LustreCompiler
    {
        public static string Compile(string lustreSource, string mainNode)
        {
            //TODO get $HOME so this will not only work for the root user
            SetupLus2Oc();

            WslUtil.ExecuteCommand($"echo \"{lustreSource}\" > $HOME/{mainNode}.lus");

            WslUtil.ExecuteCommand(
                "export LUSTRE_INSTALL=/root/bin/lustre-v4-III-db-linux64;" +
                "source $LUSTRE_INSTALL/setenv.sh;" +
                $"lus2oc $LUSTRE_INSTALL/examples/parity/parity.lus {mainNode} -o $HOME/{mainNode}.oc"
            );

            return File.ReadAllText($@"{Environment.GetEnvironmentVariable("localappdata")}\lxss\root\{mainNode}.oc");
        }

        private static void SetupLus2Oc()
        {
            if (!WslUtil.CheckDirectory("$HOME/bin/lustre-v4-III-db-linux64/"))
            {
                Console.WriteLine("Downloading lus2oc:");
                WslUtil.ExecuteCommand($"curl -o /tmp/lustrev4.tgz http://www-verimag.imag.fr/DIST-TOOLS/SYNCHRONE/lustre-v4/distrib/linux64/lustre-v4-III-db-linux64.tgz");
                Console.WriteLine("Creating Directory for lus2oc:");
                WslUtil.ExecuteCommand("mkdir $HOME/bin");
                Console.WriteLine("Unpacking lus2oc:");
                WslUtil.ExecuteCommand("tar zxvf /tmp/lustrev4.tgz -C $HOME/bin/");
            }
        }
    }

    internal static class WslUtil
    {
        public static Process GetWslProcess()
        {
            return new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    RedirectStandardOutput = true,
                    CreateNoWindow = false,
                    UseShellExecute = false
                }
            };
        }

        public static void ExecuteCommand(string command)
        {
            using (var wslProcess = GetWslProcess())
            {
                wslProcess.StartInfo.Arguments = command;
                wslProcess.OutputDataReceived += (sendingProcess, dataLine) => Console.WriteLine(dataLine.Data?.ToString());
                wslProcess.Start();
                wslProcess.BeginOutputReadLine();
                wslProcess.WaitForExit();
            }
        }

        public static string ExecuteCommandWithResult(string command)
        {
            using (var wslProcess = GetWslProcess())
            {
                wslProcess.StartInfo.Arguments = command;
                wslProcess.Start();
                var a = wslProcess.StandardOutput.ReadToEnd();
                wslProcess.WaitForExit();
                return a;
            }
        }

        public static bool CheckDirectory(string path)
        {
            using (var wslProcess = GetWslProcess())
            {
                wslProcess.StartInfo.Arguments = $"[ -d {path} ] && echo true || echo false";
                wslProcess.Start();
                wslProcess.WaitForExit();
                return bool.Parse(wslProcess.StandardOutput.ReadToEnd());
            }
        }
    }
}

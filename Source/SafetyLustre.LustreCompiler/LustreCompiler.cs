using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetyLustre.LustreCompiler
{
    /// <summary>
    /// This requires Windows Subsystem for Linux
    /// To install run
    ///     lxrun.exe /install
    /// For more information visit https://docs.microsoft.com/en-us/windows/wsl/install-win10
    /// This was tested against the Ubuntu distribution on Windows 10 Version 1803 (Build 17134.48)
    /// </summary>
    public static class LusCompiler
    {
        private static readonly Object fileLock = new Object();

        public static string Compile(string lustreSource, string mainNode)
        {
            SetupLus2Oc();

            var wslHomeDirectory = WslUtil.ExecuteCommandWithResult("echo $HOME")   // Get home directory of current wsl user
                .Trim();                                                            // Remove trailing '\n'

            var lxssHomeDirectory = Path.Combine(
                Environment.GetEnvironmentVariable("localappdata"),                 // Wsl filesystem ist stored under
                "lxss",                                                             // %localappdata%/lxss
                wslHomeDirectory.Remove(0, 1)                                       // Remove leading '/'
            );

            //HACK to make this thread safe
            lock (fileLock)
            {
                // Store lustre source for lus2oc to read
                File.WriteAllText(Path.Combine(lxssHomeDirectory, $"{mainNode}.lus"), lustreSource);
            }

            WslUtil.ExecuteCommand($"chmod 0777 {wslHomeDirectory}/{mainNode}.lus");

            // Compile .lus to wsl users home directory
            WslUtil.ExecuteCommand(
                $"export LUSTRE_INSTALL={wslHomeDirectory}/bin/lustre-v4-III-db-linux64;" +                                 // Set environment variable
                "source $LUSTRE_INSTALL/setenv.sh;" +                                                                       // Source setenv script
                $"lus2oc {wslHomeDirectory}/{mainNode}.lus {mainNode} -o {wslHomeDirectory}/{mainNode}.oc"                  // Compile .lus file
            );

            // Read and return compiled object code
            return File.ReadAllText(Path.Combine(lxssHomeDirectory, $"{mainNode}.oc"));
        }

        /// <summary>
        /// Downloads and unpacks lustre-v4 to current wsl users home directory if not yet available
        /// For more information on lus2oc visit http://www-verimag.imag.fr/DIST-TOOLS/SYNCHRONE/lustre-v4/distrib/lv4-html/index.html
        /// </summary>
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

    /// <summary>
    /// Utility class for executing commands in wsl environment
    /// </summary>
    internal static class WslUtil
    {
        /// <summary>
        /// Creates a <see cref="System.Diagnostics.Process"/> object referencing wsl.exe
        /// </summary>
        /// <returns>The Process</returns>
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

        /// <summary>
        /// Executes a command in the wsl environment.
        /// Output of the command is written to console.
        /// </summary>
        /// <param name="command">The command to execute</param>
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

        /// <summary>
        /// Executes a command in the wsl environment.
        /// Output of the command is written to console.
        /// The result of the command is returned.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The result of the command</returns>
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

        /// <summary>
        /// Checks if a directory exists in the wsl environment
        /// </summary>
        /// <param name="path">The path of the directory to check</param>
        /// <returns>If the directory exists</returns>
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

using System;
using System.Diagnostics;
using System.IO;

namespace JustinCredible.NetworkScanner
{
    public class ArpScan
    {
        public static string Execute(string interfaceName, bool verbose = false)
        {
            var arpScanArguments = $"--interface={interfaceName} --localnet";

            if (verbose)
            {
                Console.WriteLine("Executing arp-scan command:");
                Console.WriteLine($"\tarp-scan {arpScanArguments}");
            }

            var process = new Process();
            process.StartInfo.FileName = "arp-scan";
            process.StartInfo.Arguments = arpScanArguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            // Synchronously read the standard output of the spawned process. 
            StreamReader stdOutReader = process.StandardOutput;
            string stdOut = stdOutReader.ReadToEnd();

            // Synchronously read the standard output of the spawned process. 
            StreamReader stdErrReader = process.StandardError;
            string stdErr = stdErrReader.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("A non-zero exit code was returned from arp-scan (try sudo)", new Exception("arp-scan stderr: " + stdErr));

            return stdOut;
        }
    }
}

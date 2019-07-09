using System;
using System.Diagnostics;
using System.Text;

namespace JustinCredible.NetworkScanner
{
    public class ArpScan
    {
        public static String Execute(String interfaceName, bool verbose = false)
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
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            var output = new StringBuilder();

            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (sender, data) => output.Append(data.Data);

            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (sender, data) => output.Append(data.Data);

            process.Start();

            return output.ToString();
        }
    }
}

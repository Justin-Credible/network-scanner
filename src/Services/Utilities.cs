using System;
using System.Reflection;

namespace JustinCredible.NetworkScanner
{
    public class Utilities
    {
        public static string MaskString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length <= 4)
                return "****";

            return new String('*', input.Length - 4) + input.Substring(input.Length - 4);
        }

        public static string AppVersion
        {
            get
            {
                return Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            }
        }
    }
}

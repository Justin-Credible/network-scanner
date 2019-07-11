using System;

namespace JustinCredible.NetworkScanner
{
    public class Utilities
    {
        public static string maskString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length <= 4)
                return "****";

            return new String('*', input.Length - 4) + input.Substring(input.Length - 4);
        }
    }
}

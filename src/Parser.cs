using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace JustinCredible.NetworkScanner
{
    public class Parser
    {
        private static Regex _whiteSpaceRegEx = new Regex("\\s+");

        // dhcp-host=98:DE:D0:1F:FA:57,192.168.1.20,hostname
        private static Regex _dhcpReservationRegEx = new Regex("dhcp-host=([0-9A-F:]+),([0-9.]+),(.*)");

        public static List<HostsEntry> ParseHostsFile(String hostsFilePath)
        {
            String hostsFileContent = File.ReadAllText(hostsFilePath);
            return ParseHostsFileContent(hostsFileContent);
        }

        public static List<HostsEntry> ParseHostsFileContent(String hostsFileContent)
        {
            var entries = new List<HostsEntry>();

            if (String.IsNullOrEmpty(hostsFileContent))
                return entries;

            var lines = hostsFileContent.Split(Environment.NewLine);

            foreach (var rawLine in lines)
            {
                if (String.IsNullOrEmpty(rawLine))
                    continue;

                var line = rawLine.Trim();

                // Ignore comments.
                if (line.StartsWith("#"))
                    continue;

                // Ignore IPv6 entries.
                if (line.Contains(":"))
                    continue;

                // Reduce whitespace to a single character for easier parsing.
                line = _whiteSpaceRegEx.Replace(line, " ");

                var parts = line.Split(" ");

                // Only use the IP address and first hostname; ignore aliases.
                if (parts.Length < 2)
                    continue;

                var entry = new HostsEntry()
                {
                    HostName = parts[1],
                    IpAddress = parts[0],
                };

                entries.Add(entry);
            }

            return entries;
        }
        public static List<DhcpReservationEntry> ParseDhcpReservationsFile(String dhcpReservationsFilePath)
        {
            String dhcpReservationsFileContent = File.ReadAllText(dhcpReservationsFilePath);
            return ParseDhcpReservationsFileContent(dhcpReservationsFileContent);
        }

        public static List<DhcpReservationEntry> ParseDhcpReservationsFileContent(String dhcpReservationsFileContent)
        {
            var entries = new List<DhcpReservationEntry>();

            if (String.IsNullOrEmpty(dhcpReservationsFileContent))
                return entries;

            var lines = dhcpReservationsFileContent.Split(Environment.NewLine);

            foreach (var rawLine in lines)
            {
                if (String.IsNullOrEmpty(rawLine))
                    continue;

                var line = rawLine.Trim();

                // Ignore comments.
                if (line.StartsWith("#"))
                    continue;

                var matches = _dhcpReservationRegEx.Match(line);

                if (matches.Groups.Count != 4)
                    continue;

                var entry = new DhcpReservationEntry()
                {
                    HostName = matches.Groups[3].Value,
                    IpAddress = matches.Groups[2].Value,
                    MacAddress = matches.Groups[1].Value,
                };

                entries.Add(entry);
            }

            return entries;
        }
    }
}

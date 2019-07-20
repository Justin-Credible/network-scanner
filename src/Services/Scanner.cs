using System;
using System.Collections.Generic;
using System.IO;

namespace JustinCredible.NetworkScanner
{
    public class Scanner
    {
        public static DetectUnknownHostsResults DetectUnknownHosts(
            string interfaceName,
            string hostsPath = "/etc/hosts",
            string dhcpReservationsPath = null,
            bool verbose = false)
        {
            // Sanity checking: ensure arguments are set as expected.

            if (String.IsNullOrEmpty(interfaceName))
                throw new ArgumentException("Interface name is required.");

            if (String.IsNullOrEmpty(hostsPath))
                throw new ArgumentException("Hosts path is required.");

            if (!File.Exists(hostsPath))
                throw new ArgumentException($"Could not locate hosts file with given path: {hostsPath}");

            if (dhcpReservationsPath != null && !File.Exists(dhcpReservationsPath))
                throw new ArgumentException($"Could not locate dnsmasq DHCP reservations file with given path: {dhcpReservationsPath}");

            // Attempt to parse the hosts file.

            if (verbose)
                Console.WriteLine($"Parsing hosts file: {hostsPath}");

            var hostEntries = Parser.ParseHostsFile(hostsPath);

            if (verbose)
                Console.WriteLine($"Parsed {hostEntries.Count} host entries.");

            // Attempt to parse the dnsmasq DHCP reservations file.

            if (verbose)
                Console.WriteLine($"Parsing dnsmasq DHCP reservations file: {dhcpReservationsPath}");

            List<DhcpReservationEntry> dhcpReservationEntries = null;

            if (!String.IsNullOrEmpty(dhcpReservationsPath))
            {
                dhcpReservationEntries = Parser.ParseDhcpReservationsFile(dhcpReservationsPath);

                if (verbose)
                    Console.WriteLine($"Parsed {dhcpReservationEntries.Count} DHCP reservation entries.");
            }

            // Delegate to arp-scan to scan the network for all hosts.

            if (verbose)
                Console.WriteLine($"Executing arp-scan on interface {interfaceName}");

            var arpScanOutput = ArpScan.Execute(interfaceName, verbose);
            var arpScanEntries = Parser.ParseAprScanOutput(arpScanOutput);

            if (verbose)
                Console.WriteLine($"Received {arpScanEntries.Count} arp-scan entries.");

            // See if each IP address exists in known hosts or MAC address in known DHCP reservations.

            var unknownHosts = new List<ArpScanEntry>();
            List<KeyValuePair<ArpScanEntry, DhcpReservationEntry>> mismatchedIpAddressDhcpHosts = null;

            foreach (var arpEntry in arpScanEntries)
            {
                var matchingHostsEntry = hostEntries.Find(x => x.IpAddress == arpEntry.IpAddress);

                // Found a matching hosts entry.
                if (matchingHostsEntry != null)
                    continue;

                if (dhcpReservationEntries != null)
                {
                    mismatchedIpAddressDhcpHosts = new List<KeyValuePair<ArpScanEntry, DhcpReservationEntry>>();

                    var matchingDhcpEntry = dhcpReservationEntries.Find(x => x.MacAddress == arpEntry.MacAddress);

                    // Found a DHCP reservation match by MAC address, make sure the IP address matches.
                    if (matchingDhcpEntry != null)
                    {
                        // Found a matching DHCP reservation entry.
                        if (matchingDhcpEntry.IpAddress == arpEntry.IpAddress)
                            continue;

                        // If the IP address doesn't match, something might be up.
                        mismatchedIpAddressDhcpHosts.Add(new KeyValuePair<ArpScanEntry, DhcpReservationEntry>(arpEntry, matchingDhcpEntry));
                    }
                }

                // Didn't find a match in either.
                unknownHosts.Add(arpEntry);
            }

            return new DetectUnknownHostsResults()
            {
                UnknownHosts = unknownHosts,
                MismatchedIpAddressDhcpHosts = mismatchedIpAddressDhcpHosts,
            };
        }
    }
}

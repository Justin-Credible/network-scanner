using System;
using System.Collections.Generic;
using System.IO;

namespace JustinCredible.NetworkScanner
{
    public class Scanner
    {
        public static void Scan(
            String interfaceName,
            String hostsPath = "/etc/hosts",
            String dhcpReservationsPath = null,
            bool sendPushNotifications = false,
            bool verbose = false)
        {

            if (String.IsNullOrEmpty(interfaceName))
                throw new ArgumentException("Interface name is required.");

            if (String.IsNullOrEmpty(hostsPath))
                throw new ArgumentException("Hosts path is required.");

            if (!File.Exists(hostsPath))
                throw new ArgumentException($"Could not locate hosts file with given path: {hostsPath}");

            if (dhcpReservationsPath != null && !File.Exists(dhcpReservationsPath))
                throw new ArgumentException($"Could not locate dnsmasq DHCP reservations file with given path: {dhcpReservationsPath}");

            if (verbose)
                Console.WriteLine($"Parsing hosts file: {hostsPath}");

            var hostEntries = Parser.ParseHostsFile(hostsPath);

            if (verbose)
                Console.WriteLine($"Parsed {hostEntries.Count} host entries.");

            if (verbose)
                Console.WriteLine($"Parsing dnsmasq DHCP reservations file: {dhcpReservationsPath}");

            List<DhcpReservationEntry> dhcpReservationEntries = null;

            if (!String.IsNullOrEmpty(dhcpReservationsPath))
            {
                dhcpReservationEntries = Parser.ParseDhcpReservationsFile(dhcpReservationsPath);

                if (verbose)
                    Console.WriteLine($"Parsed {dhcpReservationEntries.Count} DHCP reservation entries.");
            }

            if (verbose)
                Console.WriteLine($"Executing arp-scan on interface {interfaceName}");

            var arpScanOutput = ArpScan.Execute(interfaceName, verbose);
            var arpScanEntries = Parser.ParseAprScanOutput(arpScanOutput);

            if (verbose)
                Console.WriteLine($"Received {arpScanEntries.Count} arp-scan entries.");

            // TODO: See if each IP address exists in known hosts or MAC address in known DHCP reservations
            // TODO: If not, add to list of unknown hosts.
            // TODO: Write stdout line with each unknown host.
            // TODO: Conditionally send push notifications.

            throw new NotImplementedException();
        }
    }
}

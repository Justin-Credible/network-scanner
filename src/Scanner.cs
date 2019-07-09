using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

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

            var hostEntries = Parser.ParseHostsFile(hostsPath);

            List<DhcpReservationEntry> dhcpReservationEntries = null;

            if (!String.IsNullOrEmpty(dhcpReservationsPath))
                dhcpReservationEntries = Parser.ParseDhcpReservationsFile(dhcpReservationsPath);

            // TODO: Execute `sudo arp-scan --interface={interfaceName} --localnet` (https://www.blackmoreops.com/2015/12/31/use-arp-scan-to-find-hidden-devices-in-your-network/)
            // TODO: Parse output of arp-scan
            // TODO: For each host found via arp-scan
            // TODO: See if each IP address exists in known hosts or MAC address in known DHCP reservations
            // TODO: If not, add to list of unknown hosts.
            // TODO: Write stdout line with each unknown host.
            // TODO: Conditionally send push notifications.

            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace JustinCredible.NetworkScanner
{
    public class Reporter
    {
        public static void ReportToConsole(DetectUnknownHostsResults results)
        {
            var unknownHosts = results.UnknownHosts;
            var mismatchedIpAddressDhcpHosts = results.MismatchedIpAddressDhcpHosts;

            if (unknownHosts.Count == 0)
            {
                Console.WriteLine("No unknown hosts found.", unknownHosts.Count);
            }
            else
            {
                Console.WriteLine("Found {0} unknown hosts!", unknownHosts.Count);
                Console.WriteLine();
                Console.WriteLine("IP Address\tMAC Address\tManufacturer");

                foreach (var unknownHost in unknownHosts)

                    Console.WriteLine("{0}\t{1}\t{2}",
                        unknownHost.IpAddress,
                        unknownHost.MacAddress,
                        unknownHost.Manufacturer
                    );
            }

            // Write out the DHCP reservation IP address mismatches to the console.

            if (mismatchedIpAddressDhcpHosts != null)
            {
                if (mismatchedIpAddressDhcpHosts.Count == 0)
                {
                    Console.WriteLine("No DHCP reservation mismatches found.", unknownHosts.Count);
                }
                else
                {
                    Console.WriteLine("Found {0} DHCP reservation mismatches!", unknownHosts.Count);
                    Console.WriteLine();
                    Console.WriteLine("IP Address\tMAC Address\tManufacturer\tExpected IP Address");

                    foreach (var mismatch in mismatchedIpAddressDhcpHosts)
                    {
                        var arpEntry = mismatch.Key;
                        var dhcpReservationEntry = mismatch.Value;

                        Console.WriteLine("{0}\t{1}\t{2}\t{3}",
                            arpEntry.IpAddress,
                            arpEntry.MacAddress,
                            arpEntry.Manufacturer,
                            dhcpReservationEntry.IpAddress
                        );
                    }
                }
            }
        }

        public static void ReportToPushover(DetectUnknownHostsResults results, string token, string user, bool verbose = false)
        {
            var unknownHosts = results.UnknownHosts;
            var mismatchedIpAddressDhcpHosts = results.MismatchedIpAddressDhcpHosts;

            if (unknownHosts.Count == 0 && (mismatchedIpAddressDhcpHosts == null || mismatchedIpAddressDhcpHosts.Count == 0))
                return;

            Console.WriteLine("Reporting unknown hosts and/or mismatches via push notification...");

            // TODO: Read recently notified hosts from a temp JSON file.
            // TODO: Remove expired entries
            // var recentPath = Path.GetTempFileName("network-scanner-unknown-hosts-pn-recents.json");

            // TODO: Remove from the list hosts that were notified in the last 24 hours.
            // TODO: Add to recents list with DateTime.Now
            // TODO: Reserialize recents and write back to temp file.

            // TODO: Build a single message with max of 3 hosts, with "...x more" at the bottom if more than 3
            // var message = "";

            // Pushover.Send(token, user, message, verbose);
        }
    }
}

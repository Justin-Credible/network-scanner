using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace JustinCredible.NetworkScanner
{
    public class Reporter
    {
        private static string RECENTLY_NOTIFIED_PN_UNKNOWN_HOSTS_FILE_NAME = "network-scanner-unknown-hosts-pn-recents.json";
        private static string RECENTLY_NOTIFIED_PN_RESERVATION_MISMATCH_FILE_NAME = "network-scanner-reservation-mismatch-pn-recents.json";
        private static int MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION = 3;
        private static int NOTIFICATION_THRESHOLD_HOURS = 24;

        private static string RecentHostsPath
        {
            get
            {
                return Path.GetTempPath() + RECENTLY_NOTIFIED_PN_UNKNOWN_HOSTS_FILE_NAME;
            }
        }

        private static string RecentMismatchesPath
        {
            get
            {
                return Path.GetTempPath() + RECENTLY_NOTIFIED_PN_RESERVATION_MISMATCH_FILE_NAME;
            }
        }

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

        public static void ReportToPushover(DetectUnknownHostsResults results, string token, string user, string pushoverApiUrl, bool verbose = false)
        {
            // Create shallow clones because we need to modify these lists below.
            var unknownHosts = new List<ArpScanEntry>(results.UnknownHosts);
            var mismatchedIpAddressDhcpHosts = results.MismatchedIpAddressDhcpHosts == null
                ? new List<KeyValuePair<ArpScanEntry, DhcpReservationEntry>>()
                : new List<KeyValuePair<ArpScanEntry, DhcpReservationEntry>>(results.MismatchedIpAddressDhcpHosts);

            if (unknownHosts.Count == 0 && (mismatchedIpAddressDhcpHosts == null || mismatchedIpAddressDhcpHosts.Count == 0))
                return;

            if (verbose)
                Console.WriteLine("Checking to see if push notifications need to be sent...");

            var notificationThreshold = DateTime.Now.AddHours(-NOTIFICATION_THRESHOLD_HOURS);

            // ************************************************************************************************

            if (unknownHosts.Count > 0)
            {
                // Read the recently notified hosts from disk.

                string recentHostsJson = null;

                if (verbose)
                    Console.WriteLine("Using recent hosts JSON from file: " + RecentHostsPath);

                if (File.Exists(RecentHostsPath))
                    recentHostsJson = File.ReadAllText(RecentHostsPath);

                if (String.IsNullOrEmpty(recentHostsJson))
                    recentHostsJson = "[]";

                var recentHostEntries = JsonConvert.DeserializeObject<List<RecentlyNotifiedHostEntry>>(recentHostsJson);

                // Remove expired recently notified entries; these ones we can re-notify for if they're still unknown.
                recentHostEntries.RemoveAll(x => x.NotifiedAt < notificationThreshold);

                // Remove hosts we've already notified for recently.
                unknownHosts.RemoveAll(x => recentHostEntries.FindAll(y => y.UnknownHost.IpAddress == x.IpAddress).Count > 0);

                // Since we're about to notify for these hosts, add them to the list.
                foreach (var unknownHost in unknownHosts)
                {
                    recentHostEntries.Add(new RecentlyNotifiedHostEntry()
                    {
                        NotifiedAt = DateTime.Now,
                        UnknownHost = unknownHost,
                    });
                }

                // Update the recently notified JSON back to disk.
                recentHostsJson = JsonConvert.SerializeObject(recentHostEntries);
                File.WriteAllText(RecentHostsPath, recentHostsJson);

                if (unknownHosts.Count > 0)
                {
                    Console.WriteLine($"Found {unknownHosts.Count} unknown hosts that haven't been recently reported via push notification; sending message now...");

                    // Build the unknown hosts push notification body.
                    var unknownHostsMessage = new StringBuilder();
                    unknownHostsMessage.Append("Found unknown host");

                    if (unknownHosts.Count > 1)
                        unknownHostsMessage.Append("s");

                    unknownHostsMessage.AppendLine(" on network:");

                    for (int i = 0; i < unknownHosts.Count && i < MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION; i++)
                    {
                        var unknownHost = unknownHosts[i];
                        unknownHostsMessage.AppendFormat("• {0} [{1}] ({2})", unknownHost.IpAddress, unknownHost.MacAddress, unknownHost.Manufacturer);
                        unknownHostsMessage.AppendLine();
                    } 

                    if (unknownHosts.Count > MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION)
                    {
                        var more = unknownHosts.Count - MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION;
                        unknownHostsMessage.AppendLine($"{more}...");
                    }

                    Pushover.Send(token, user, pushoverApiUrl, unknownHostsMessage.ToString(), verbose);
                }
            }

            // ************************************************************************************************

            if (mismatchedIpAddressDhcpHosts == null || mismatchedIpAddressDhcpHosts.Count > 0)
            {
                // Read the recently notified DHCP reservation mismatches from disk.

                string recentMismatchJson = null;

                if (verbose)
                    Console.WriteLine("Using recent reservations mismatches JSON from file: " + RecentMismatchesPath);

                if (File.Exists(RecentMismatchesPath))
                    recentMismatchJson = File.ReadAllText(RecentMismatchesPath);

                if (String.IsNullOrEmpty(recentMismatchJson))
                    recentMismatchJson = "[]";

                var recentMismatchEntries = JsonConvert.DeserializeObject<List<RecentlyNotifiedReservationMismatchEntry>>(recentMismatchJson);

                // Remove expired recently notified entries; these ones we can re-notify for if they're still unknown.
                recentMismatchEntries.RemoveAll(x => x.NotifiedAt < notificationThreshold);

                // Remove hosts we've already notified for recently.
                mismatchedIpAddressDhcpHosts.RemoveAll(x => recentMismatchEntries.FindAll(y => y.MismatchEntry.Key.IpAddress == x.Value.IpAddress).Count > 0);

                // Since we're about to notify for these hosts, add them to the list.
                foreach (var mismatchEntry in mismatchedIpAddressDhcpHosts)
                {
                    recentMismatchEntries.Add(new RecentlyNotifiedReservationMismatchEntry()
                    {
                        NotifiedAt = DateTime.Now,
                        MismatchEntry = mismatchEntry,
                    });
                }

                // Update the recently notified JSON back to disk.
                recentMismatchJson = JsonConvert.SerializeObject(recentMismatchEntries);
                File.WriteAllText(RecentMismatchesPath, recentMismatchJson);

                if (mismatchedIpAddressDhcpHosts.Count > 0)
                {
                    Console.WriteLine($"Found {mismatchedIpAddressDhcpHosts.Count} DHCP reservation mismatches that haven't been recently reported via push notification; sending message now...");

                    // Build the unknown hosts push notification body.
                    var reservationMismatchMessage = new StringBuilder();
                    reservationMismatchMessage.Append("Found unknown host");

                    if (mismatchedIpAddressDhcpHosts.Count > 1)
                        reservationMismatchMessage.Append("s");

                    reservationMismatchMessage.AppendLine(" on network:");

                    for (int i = 0; i < mismatchedIpAddressDhcpHosts.Count && i < MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION; i++)
                    {
                        var mismatchEntry = mismatchedIpAddressDhcpHosts[i];
                        reservationMismatchMessage.AppendFormat("• {0} Actual: [{1}] Expected: [{2}]", mismatchEntry.Key.IpAddress, mismatchEntry.Key.MacAddress, mismatchEntry.Value.MacAddress);
                        reservationMismatchMessage.AppendLine();
                    }

                    if (mismatchedIpAddressDhcpHosts.Count > MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION)
                    {
                        var more = mismatchedIpAddressDhcpHosts.Count - MAX_HOSTS_IN_SINGLE_PUSH_NOTIFICATION;
                        reservationMismatchMessage.AppendLine($"{more}...");
                    }

                    Pushover.Send(token, user, pushoverApiUrl, reservationMismatchMessage.ToString(), verbose);
                }
            }

            // ************************************************************************************************

            if (unknownHosts.Count == 0 && (mismatchedIpAddressDhcpHosts == null || mismatchedIpAddressDhcpHosts.Count == 0))
                Console.WriteLine("Did not find any entries that need reporting via push notification.");
        }

        public static void ClearRecentlyNotifiedLog(bool verbose = false)
        {
            if (File.Exists(RecentHostsPath))
            {
                if (verbose)
                    Console.WriteLine($"Deleting {RecentHostsPath}");

                File.Delete(RecentHostsPath);
            }

            if (File.Exists(RecentMismatchesPath))
            {
                if (verbose)
                    Console.WriteLine($"Deleting {RecentMismatchesPath}");

                File.Delete(RecentMismatchesPath);
            }
        }

        public static void PrintRecentlyNotifiedPaths()
        {
            Console.WriteLine("Recently notified unknown hosts:");
            Console.WriteLine("\t" + RecentHostsPath);
            Console.WriteLine("Recently notified mismatched reservations:");
            Console.WriteLine("\t" + RecentMismatchesPath);
        }

        public static void PrintRecentlyNotifiedEntries(bool verbose = false)
        {
            // Read the recently notified hosts from disk.

            string recentHostsJson = null;

            if (verbose)
                Console.WriteLine("Using recent hosts JSON from file: " + RecentHostsPath);

            if (File.Exists(RecentHostsPath))
                recentHostsJson = File.ReadAllText(RecentHostsPath);

            if (String.IsNullOrEmpty(recentHostsJson))
                recentHostsJson = "[]";

            var recentHostEntries = JsonConvert.DeserializeObject<List<RecentlyNotifiedHostEntry>>(recentHostsJson);

            // Read the recently notified DHCP reservation mismatches from disk.

            string recentMismatchJson = null;

            if (verbose)
                Console.WriteLine("Using recent reservations mismatches JSON from file: " + RecentMismatchesPath);

            if (File.Exists(RecentMismatchesPath))
                recentMismatchJson = File.ReadAllText(RecentMismatchesPath);

            if (String.IsNullOrEmpty(recentMismatchJson))
                recentMismatchJson = "[]";

            var recentMismatchEntries = JsonConvert.DeserializeObject<List<RecentlyNotifiedReservationMismatchEntry>>(recentMismatchJson);

            // Write to console.

            Console.WriteLine("Recently notified unknown hosts:");

            Console.WriteLine("Notified At\tIP Address\tMAC Address\tManufacturer");

            foreach (var recentHostEntry in recentHostEntries)
                Console.WriteLine(" {0}\t{1}\t{2}\t{3}", recentHostEntry.NotifiedAt, recentHostEntry.UnknownHost.IpAddress, recentHostEntry.UnknownHost.MacAddress, recentHostEntry.UnknownHost.Manufacturer);

            Console.WriteLine("  ({0} entries)", recentHostEntries.Count);

            Console.WriteLine("Recently notified DHCP reservation mismatches:");

            Console.WriteLine("Notified At\tActual IP\tActual MAC\tExpected MAC");

            foreach (var recentMismatchEntry in recentMismatchEntries)
                Console.WriteLine(" {0}\t{1}\t{2}\t{3}",
                    recentMismatchEntry.NotifiedAt,
                    recentMismatchEntry.MismatchEntry.Key.IpAddress,
                    recentMismatchEntry.MismatchEntry.Key.MacAddress,
                    recentMismatchEntry.MismatchEntry.Value.MacAddress);

            Console.WriteLine("  ({0} entries)", recentMismatchEntries.Count);
        }
    }
}

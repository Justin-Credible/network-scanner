using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace JustinCredible.NetworkScanner
{
    class Program
    {
        private static CommandLineApplication _app;

        private static IConfiguration _config;

        private static IConfiguration Config
        {
            get
            {
                if (_config == null)
                    _config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", true, true)
                        .Build();

                return _config;
            }
        }

        private static int Main(string[] args)
        {
            var version = Utilities.AppVersion;

            _app = new CommandLineApplication();
            _app.Name = "network-scanner";
            _app.HelpOption("-?|-h|--help");

            _app.VersionOption("-v|--version",

                // Used for HelpOption() header
                $"{_app.Name} {version}",

                // Used for output of --version option.
                version
            );

            // When launched without any commands or options.
            _app.OnExecute(() =>
            {
                _app.ShowHelp();
                return 0;
            });

            _app.Command("scan", Scan);
            _app.Command("list-known", ListKnown);
            _app.Command("list-active", ListActive);

            try
            {
                return _app.Execute(args);
            }
            catch (Exception exception)
            {
                var verbose = false;

                foreach (string arg in args)
                {
                    if (arg == "-v" || arg == "--verbose")
                        verbose = true;
                }

                PrintUnhandledException(exception, verbose);
                return 1;
            }
        }

        private static void Scan(CommandLineApplication command)
        {
            command.Description = "Scans the network using an arp-scan broadcast and reports any unidentified hosts.";
            command.HelpOption("-?|-h|--help");

            var interfaceNameArg = command.Argument("[interface]", "The name of the interface to use for scanning (e.g. eth0, ens192)");

            var hostsPathOption = command.Option("-h|--hosts", "Path to the hosts file to check against for known hosts.", CommandOptionType.SingleValue);
            var dnsmasqDhcpPathOption = command.Option("-dd|--dnsmasq-dhcp", "Path to a dnsmasq file containing DHCP reservations to check against for known hosts.", CommandOptionType.SingleValue);
            var pushNotificationOption = command.Option("-pn|--push-notitication", "Send a push notfication via pushover.net for each unidentified host found.", CommandOptionType.NoValue);
            var verboseOption = command.Option("-v|--verbose", "Verbose output.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                var interfaceName = interfaceNameArg.Value;

                if (String.IsNullOrEmpty(interfaceName))
                {
                    PrintMissingArgumentError(interfaceNameArg.Name, _app, command.Name);
                    return 1;
                }

                var hostsPath = hostsPathOption.HasValue() ? hostsPathOption.Value() : "/etc/hosts";
                var dnsmasqDhcpPath = dnsmasqDhcpPathOption.Value();
                var sendPushNotifications = pushNotificationOption.HasValue();
                string pushoverToken = Config["pushover_token"];
                string pushoverUser = Config["pushover_user"];
                string pushoverApiUrl = Config["pushover_api_url"];
                var verbose = verboseOption.HasValue();


                if (verbose)
                {
                    Console.WriteLine("Hosts Path: " + hostsPath);
                    Console.WriteLine("dnsmasq DHCP Reservations Path: " + (String.IsNullOrEmpty(dnsmasqDhcpPath) ? "N/A" : dnsmasqDhcpPath));
                    Console.WriteLine("Send Push Notifications: " + sendPushNotifications);
                    Console.WriteLine("Pushover.net API token: " + Utilities.MaskString(pushoverToken));
                    Console.WriteLine("Pushover.net API user: " + pushoverUser);
                    Console.WriteLine("Pushover.net API URL: " + pushoverApiUrl);

                    Console.WriteLine("Starting network scan...");
                }

                try
                {
                    var results = Scanner.DetectUnknownHosts(interfaceName, hostsPath, dnsmasqDhcpPath, verbose);

                    Reporter.ReportToConsole(results);

                    if (sendPushNotifications)
                        Reporter.ReportToPushover(results, pushoverToken, pushoverUser, pushoverApiUrl, verbose);
                }
                catch (Exception exception)
                {
                    PrintUnhandledException(exception, verbose);
                    return 1;
                }

                if (verbose)
                    Console.WriteLine("Operation completed.");

                return 0;
            });
        }

        private static void ListKnown(CommandLineApplication command)
        {
            command.Description = "Lists the known hosts by parsing the hosts file as well as a dnsmasq DHCP reservations file (optional).";
            command.HelpOption("-?|-h|--help");

            var hostsPathOption = command.Option("-h|--hosts", "Path to the hosts file to check against for known hosts.", CommandOptionType.SingleValue);
            var dnsmasqDhcpPathOption = command.Option("-dd|--dnsmasq-dhcp", "Path to a dnsmasq file containing DHCP reservations to check against for known hosts.", CommandOptionType.SingleValue);
            var verboseOption = command.Option("-v|--verbose", "Verbose output.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                var hostsPath = hostsPathOption.HasValue() ? hostsPathOption.Value() : "/etc/hosts";
                var dnsmasqDhcpPath = dnsmasqDhcpPathOption.Value();
                var verbose = verboseOption.HasValue();

                if (verbose)
                {
                    Console.WriteLine("Hosts Path: " + hostsPath);
                    Console.WriteLine("dnsmasq DHCP Reservations Path: " + (String.IsNullOrEmpty(dnsmasqDhcpPath) ? "N/A" : dnsmasqDhcpPath));

                    Console.WriteLine("Parsing known hosts...");
                }

                try
                {
                    var hostsEntries = Parser.ParseHostsFile(hostsPath);

                    Console.WriteLine("Known hosts via the hosts file:");

                    foreach (var hostsEntry in hostsEntries)
                        Console.WriteLine(" {0}\t{1}", hostsEntry.IpAddress, hostsEntry.HostName);

                    if (!String.IsNullOrEmpty(dnsmasqDhcpPath))
                    {
                        var dhcpReservationEntries = Parser.ParseDhcpReservationsFile(dnsmasqDhcpPath);

                        Console.Write(Environment.NewLine);
                        Console.WriteLine("Known hosts via the dnsmasq reservations file:");

                        foreach (var dhcpReservationEntry in dhcpReservationEntries)
                            Console.WriteLine(" {0}\t{1}\t{2}", dhcpReservationEntry.IpAddress, dhcpReservationEntry.MacAddress, dhcpReservationEntry.HostName);
                    }
                }
                catch (Exception exception)
                {
                    PrintUnhandledException(exception, verbose);
                    return 1;
                }

                if (verbose)
                    Console.WriteLine("Operation completed.");

                return 0;
            });
        }

        private static void ListActive(CommandLineApplication command)
        {
            command.Description = "Lists the active hosts on the network by using arp-scan on the given interface.";
            command.HelpOption("-?|-h|--help");

            var interfaceNameArg = command.Argument("[interface]", "The name of the interface to use for scanning (e.g. eth0, ens192)");
            var verboseOption = command.Option("-v|--verbose", "Verbose output.", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                var interfaceName = interfaceNameArg.Value;

                if (String.IsNullOrEmpty(interfaceName))
                {
                    PrintMissingArgumentError(interfaceNameArg.Name, _app, command.Name);
                    return 1;
                }

                var verbose = verboseOption.HasValue();

                try
                {
                    var output = ArpScan.Execute(interfaceName, verbose);

                    if (verbose)
                        Console.WriteLine("arp-scan output:{0}{1}", Environment.NewLine, output);

                    var aprScanEntries = Parser.ParseAprScanOutput(output);

                    Console.WriteLine("Hosts found via arp-scan:");
                    Console.WriteLine(" IP Address\tMAC Address\tManufacturer");

                    foreach (var aprScanEntry in aprScanEntries)
                        Console.WriteLine(" {0}\t{1}\t{2}", aprScanEntry.IpAddress, aprScanEntry.MacAddress, aprScanEntry.Manufacturer);
                }
                catch (Exception exception)
                {
                    PrintUnhandledException(exception, verbose);
                    return 1;
                }

                if (verbose)
                    Console.WriteLine("Operation completed.");

                return 0;
            });
        }

        private static void PrintMissingArgumentError(string argumentName, CommandLineApplication app = null, string command = null)
        {
            var originalForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {argumentName} must be provided.");

            Console.ForegroundColor = originalForegroundColor;

            if (app != null && !String.IsNullOrEmpty(command))
                app.ShowHelp(command);
        }

        private static void PrintUnhandledException(Exception exception, bool verbose = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            if (verbose)
                Console.Error.WriteLine("Error: {0}{1}{1}{2}", exception.Message, Environment.NewLine, exception.ToString());
            else
                Console.Error.WriteLine("Error: {0}", exception.Message);
        }
    }
}

using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace JustinCredible.NetworkScanner
{
    class Program
    {
        private static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.Name = "netscan";
            app.HelpOption("-?|-h|--help");

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });

            app.Command("scan", (command) =>
            {
                command.Description = "Scans the network using arp-scan broadcast and reports any unidentified hosts.";
                command.HelpOption("-?|-h|--help");

                var interfaceNameArg = command.Argument("[interface]", "The name of the interface to use for scanning (e.g. eth0, ens192)");

                var hostsPathOption = command.Option("-h|--hosts", "Path to the hosts file to check against for known hosts.", CommandOptionType.SingleValue);
                var dnsmasqDhcpPathOption = command.Option("-dd|--dnsmasq-dhcp", "Path to a dnsmasq file containing DHCP reservations to check against for known hosts.", CommandOptionType.SingleValue);
                var pushNotificationOption = command.Option("-pn|--push-notitication", "Send a push notfication for each unidentified host found.", CommandOptionType.NoValue);

                command.OnExecute(() =>
                {
                    var interfaceName = interfaceNameArg.Value;

                    if (String.IsNullOrEmpty(interfaceName))
                    {
                        ShowError("An interface name must be provided.", app, command.Name);
                        return 1;
                    }

                    var hostsPath = hostsPathOption.HasValue() ? hostsPathOption.Value() : "/etc/hosts";
                    var dnsmasqDhcpPath = dnsmasqDhcpPathOption.Value();
                    var sendPushNotifications = pushNotificationOption.HasValue();

                    Console.WriteLine("Scanning network...");
                    Console.WriteLine("Hosts Path: " + hostsPath);
                    Console.WriteLine("dnsmasq DHCP Reservations Path: " + (String.IsNullOrEmpty(dnsmasqDhcpPath) ? "N/A" : dnsmasqDhcpPath));
                    Console.WriteLine("Send Push Notifications: " + pushNotificationOption);

                    try
                    {
                        Scanner.Scan(interfaceName, hostsPath, dnsmasqDhcpPath, sendPushNotifications);
                    }
                    catch (Exception exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("An unhandled exception occurred!");

                        Console.Error.WriteLine("{0}{1}{2}", exception.Message, Environment.NewLine, exception.ToString());

                        return 1;
                    }

                    Console.WriteLine("Operation completed.");
                    return 0;
                });
            });

            return app.Execute(args);
        }

        private static void ShowError(string message, CommandLineApplication app = null, string command = null)
        {
            var originalForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {message}");

            Console.ForegroundColor = originalForegroundColor;

            if (app != null && !String.IsNullOrEmpty(command))
                app.ShowHelp(command);
        }
    }
}

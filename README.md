# network-scanner

A simple tool to scan a LAN looking for unknown devices. If any are found, it can then send a push notification.

Scanning for hosts is done using [`arp-scan`](https://linux.die.net/man/1/arp-scan).

Hosts are "known" if they are located in the hosts file (e.g. `/etc/hosts`). Additionally, you can specify the path to a [`dnsmasq`](https://linux.die.net/man/8/dnsmasq) DHCP reservation file using the `dnsmasq-dhcp` option with the format:

```
$ cat /etc/dnsmasq.d/04-pihole-static-dhcp.conf 
dhcp-host=aa:bb:cc:dd:ee:ff,192.168.1.20,my-device
```

If a host is during the scan that is not in one of these two files, it will be considered "unknown".

A list of unknown devices will be printed to the console. Additionally, if the `--push-notification` option is used a push notification will be sent with up to 3 of the unknown devices included. Push notifications for a given unknown host will only be sent once per 24 hours. The push notification provided is [Pushover.net](https://pushover.net/) and API keys can be specified via `appsettings.json`.

This tool is intended to be ran periodically via a cron job.

Commands are options explained via the `--help` option:

```
 network-scanner 1.0.0

Usage: network-scanner [options] [command]

Options:
  -?|-h|--help  Show help information
  -v|--version  Show version information

Commands:
  list-active  Lists the active hosts on the network by using arp-scan on the given interface.
  list-known   Lists the known hosts by parsing the hosts file as well as a dnsmasq DHCP reservations file (optional).
  notified     Shows the list of hosts that have recently had push notifications sent for.
  scan         Scans the network using an arp-scan broadcast and reports any unidentified hosts.

Use "network-scanner [command] --help" for more information about a command.
```

## Requirements

A Linux-like machine that has the `arp-scan` utility installed. In order to scan the network, `arp-scan` requires root. So you'll need to run this utility with `sudo` or as root.

## Running and Building

This project was coded in C# using [.NET Core](https://dotnet.microsoft.com/download).

To build, install the .NET Core SDK (tested with v2.2), clone the source, and run:

`dotnet publish --runtime debian-x64 --configuration release`

This will create a native binary that can be run on a Debian system without the .NET Core SDK being installed.

If you wish to build for another platform, substitue the `debian-x64` [runtime identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) with the one for your platform.

Alternatively, you can run the tool directly without creating a native binary by using the `dotnet` CLI tool, placing commands and options for `network-scanner` after a `--` break:

`dotnet run -- scan --help`

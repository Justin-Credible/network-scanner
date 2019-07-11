
using System.Collections.Generic;

namespace JustinCredible.NetworkScanner
{
    public class DetectUnknownHostsResults
    {
        public List<ArpScanEntry> UnknownHosts { get; set; }
        public List<KeyValuePair<ArpScanEntry, DhcpReservationEntry>> MismatchedIpAddressDhcpHosts { get; set; }
    }
}

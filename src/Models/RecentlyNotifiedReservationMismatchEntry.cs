
using System.Collections.Generic;

namespace JustinCredible.NetworkScanner
{
    public class RecentlyNotifiedReservationMismatchEntry
    {
        public System.DateTime NotifiedAt { get; set; }
        public KeyValuePair<ArpScanEntry, DhcpReservationEntry> MismatchEntry { get; set; }
    }
}

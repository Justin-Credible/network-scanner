
using System.Collections.Generic;

namespace JustinCredible.NetworkScanner
{
    public class RecentlyNotifiedHostEntry
    {
        public System.DateTime NotifiedAt { get; set; }
        public ArpScanEntry UnknownHost { get; set; }
    }
}

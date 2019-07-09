using System;

namespace JustinCredible.NetworkScanner
{
    public class ArpScanEntry
    {
        public String IpAddress { get; set; }
        public String MacAddress { get; set; }
        public String Manufacturer { get; set; }
    }
}

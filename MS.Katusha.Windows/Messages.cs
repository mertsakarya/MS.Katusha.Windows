using System;
using System.Drawing;

namespace MS.Katusha.Windows
{
    public class WinDialog
    {
        public long ProfileId { get; set; }
        public Image Image { get; set; }
        public string Name { get; set; }
        public DateTime LastSent { get; set; }
        public DateTime LastReceived { get; set; }
        public int Count { get; set; }
        public int UnreadSentCount { get; set; }
        public int UnreadReceivedCount { get; set; }
    }
}

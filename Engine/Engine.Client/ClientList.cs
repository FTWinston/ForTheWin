using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FTW.Engine.Client
{
    public class ClientList
    {
        List<Entry> Entries = new List<Entry>();
        public class Entry
        {
            public string Name { get; set; }
            public ulong ID { get; set; }
            public ushort Ping { get; set; }
        }
    }
}

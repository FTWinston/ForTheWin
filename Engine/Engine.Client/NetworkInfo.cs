#if NET_INFO

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    public class NetworkInfo
    {
        public bool Enabled = false;

        public void Add(InboundMessage m)
        {
            Add(m, false);
        }

        public void Add(OutboundMessage m)
        {
            Add(m, true);
        }

        private void Add(Message m, bool outgoing)
        {
            var packet = new NetworkInfo.PacketInfo() { Outgoing = outgoing, Type = m.Type, Tick = GameClient.Instance.CurrentTick, Size = m.SizeInBits };
            int pos = data.BinarySearch(packet);
            data.Insert(pos < 0 ? ~pos : pos, packet);
        }

        public double DataDuration = 10;

        public void Prune()
        {
            uint cutoff = GameClient.Instance.CurrentTick - (uint)(DataDuration / GameClient.Instance.TickInterval.TotalSeconds);
            while(data[0].Tick < cutoff)
            {
                data.RemoveAt(0);
            }
        }

        List<PacketInfo> data = new List<PacketInfo>();

        public IList<PacketInfo> Data { get { return data.AsReadOnly(); } }

        public struct PacketInfo : IComparable<PacketInfo>
        {
            public uint Tick;
            public int Size; // in BITS
            public byte Type;
            public bool Outgoing;

            public int CompareTo(PacketInfo other)
            {
                return Tick.CompareTo(other.Tick);
            }
        }
    }
}

#endif
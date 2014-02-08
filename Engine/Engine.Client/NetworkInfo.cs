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
            var packet = new NetworkInfo.PacketInfo() { Outgoing = outgoing, Type = m.Type, ActualTimestamp = GameClient.Instance.FrameTime, Size = m.SizeInBits };
            int pos = data.BinarySearch(packet);
            data.Insert(pos < 0 ? ~pos : pos, packet);
        }

        public uint DataDuration = 10000;

        public void Prune()
        {
            uint cutoff = GameClient.Instance.FrameTime - DataDuration;
            for (int i = 0; i < data.Count; i++)
                if (data[i].ActualTimestamp < cutoff)
                {
                    data.RemoveAt(i);
                    i--;
                }
        }

        List<PacketInfo> data = new List<PacketInfo>();

        public IList<PacketInfo> Data { get { return data.AsReadOnly(); } }

        public struct PacketInfo : IComparable<PacketInfo>
        {
            public uint ActualTimestamp;
            public int Size; // in BITS
            public byte Type;
            public bool Outgoing;

            public int CompareTo(PacketInfo other)
            {
                return ActualTimestamp.CompareTo(other.ActualTimestamp);
            }
        }
    }
}

#endif
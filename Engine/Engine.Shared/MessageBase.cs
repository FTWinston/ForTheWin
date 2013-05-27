using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Shared
{
    public abstract class MessageBase
    {
        // creating one to send
        public MessageBase(byte type, PacketPriority priority, PacketReliability reliability)
        {
            Priority = priority;
            Reliability = reliability;
            Stream = new BitStream();
            Type = type;
            Stream.Write(type);
        }

        // reading from a packet
        protected static BitStream GetStream(Packet p)
        {
            BitStream stream = new BitStream(p.data, p.length, false);
            stream.IgnoreBytes(1);
            return stream;
        }

        public PacketPriority Priority { get; private set; }
        public PacketReliability Reliability { get; private set; }

        public byte Type { get; private set; }
        public BitStream Stream { get; private set; }

        public void Write(string val) { Stream.Write(val); }
        public void Write(bool val) { Stream.Write(val); }
        public void Write(byte val) { Stream.Write(val); }
        public void Write(char val) { Stream.Write(val); }
        public void Write(short val) { Stream.Write(val); }
        public void Write(ushort val) { Stream.Write(val); }
        public void Write(int val) { Stream.Write(val); }
        public void Write(long val) { Stream.Write(val); }
        public void Write(float val) { Stream.Write(val); }

        protected static int ReadInt(BitStream bs) { int val; bs.Read(out val); return val; }
    }
}

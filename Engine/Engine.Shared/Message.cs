using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Shared
{
    public class Message
    {
        /// <summary>
        /// Creating a message to send
        /// </summary>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="reliability"></param>
        public Message(byte type, PacketPriority priority, PacketReliability reliability)
        {
            Priority = priority;
            Reliability = reliability;
            Stream = new BitStream();
            Type = type;
            Stream.Write(type);
        }

        /// <summary>
        /// Creating a message from a received packet
        /// </summary>
        /// <param name="p"></param>
        public Message(Packet p)
        {
            Stream = new BitStream(p.data, p.length, false);

            byte b;
            Stream.Read(out b);
            Type = b;
        }

        public PacketPriority Priority { get; private set; }
        public PacketReliability Reliability { get; private set; }

        public byte Type { get; private set; }
        public BitStream Stream { get; private set; }

        public void Write(string val) { Stream.Write(val); }
        public void Write(bool val) { Stream.Write(val); }
        public void Write(byte val) { Stream.Write(val); }
        public void Write(short val) { Stream.Write(val); }
        public void Write(ushort val) { Stream.Write(val); }
        public void Write(int val) { Stream.Write(val); }
        public void Write(long val) { Stream.Write(val); }
        public void Write(float val) { Stream.Write(val); }

        public string ReadString() { string val; Stream.Read(out val); return val; }
        public bool ReadBool() { bool val; Stream.Read(out val); return val; }
        public byte ReadByte() { byte val; Stream.Read(out val); return val; }
        public short ReadShort() { short val; Stream.Read(out val); return val; }
        //public ushort ReadUShort() { ushort val; Stream.Read(out val); return val; }
        public int ReadInt() { int val; Stream.Read(out val); return val; }
        public long ReadLong() { long val; Stream.Read(out val); return val; }
        public float ReadFloat() { float val; Stream.Read(out val); return val; }

        // used for sending data to/from the local client on a listen server
        public static List<Message> ToLocalClient = new List<Message>();
        public static List<Message> ToLocalServer = new List<Message>();
    }
}

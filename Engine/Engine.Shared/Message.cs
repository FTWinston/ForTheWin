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

            byte b = ReadByte();

            if (b == (byte)DefaultMessageIDTypes.ID_TIMESTAMP)
            {
                Timestamp = ReadULong();
                Type = ReadByte();
            }
            else
                Type = b;
        }

        public PacketPriority Priority { get; private set; }
        public PacketReliability Reliability { get; private set; }
        public ulong? Timestamp { get; private set; }

        public byte Type { get; private set; }
        public BitStream Stream { get; private set; }

        public void Write(string val) { Stream.Write(val); }
        public void Write(bool val) { Stream.Write(val); }
        public void Write(byte val) { Stream.Write(val); }
        public void Write(short val) { Stream.Write(val); }
        public void Write(ushort val) { Stream.Write(val); }
        public void Write(int val) { Stream.Write(val); }
        public void Write(uint val) { Stream.Write(BitConverter.GetBytes(val), sizeof(uint)); }
        public void Write(long val) { Stream.Write(val); }
        public void Write(ulong val) { Stream.Write(BitConverter.GetBytes(val), sizeof(ulong)); }
        public void Write(float val) { Stream.Write(val); }

        public string ReadString() { string val; Stream.Read(out val); return val; }
        public bool ReadBool() { bool val; Stream.Read(out val); return val; }
        public byte ReadByte() { byte val; Stream.Read(out val); return val; }
        public short ReadShort() { short val; Stream.Read(out val); return val; }
        public ushort ReadUShort() { byte[] data = new byte[sizeof(ushort)]; Stream.Read(data, sizeof(ushort)); return BitConverter.ToUInt16(data, 0); }
        public int ReadInt() { int val; Stream.Read(out val); return val; }
        public uint ReadUInt() { byte[] data = new byte[sizeof(uint)]; Stream.Read(data, sizeof(uint)); return BitConverter.ToUInt32(data, 0); }
        public long ReadLong() { long val; Stream.Read(out val); return val; }
        public ulong ReadULong() { byte[] data = new byte[sizeof(long)]; Stream.Read(data, sizeof(long)); return BitConverter.ToUInt64(data, 0); }
        public float ReadFloat() { float val; Stream.Read(out val); return val; }

        // used for sending data to/from the local client on a listen server
        public static List<Message> ToLocalClient = new List<Message>();
        public static List<Message> ToLocalServer = new List<Message>();
        public void ResetRead()
        {
            Stream.SetReadOffset(Timestamp.HasValue ? (uint)48 : (uint)8); // 6 bytes, or 1
        }
    }
}

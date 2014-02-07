using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace FTW.Engine.Shared
{
    public abstract class Message
    {
        public byte Type { get; protected set; }

        public abstract int SizeInBits { get; }

        // used for sending data to/from the local client on a listen server
        public static List<OutboundMessage> ToLocalClient = new List<OutboundMessage>();
        public static List<OutboundMessage> ToLocalServer = new List<OutboundMessage>();
    }

    public class InboundMessage : Message
    {
        /// <summary>
        /// Creating a message from a received packet
        /// </summary>
        /// <param name="m"></param>
        internal InboundMessage(NetIncomingMessage m)
        {
            Msg = m;
            Connection = m.SenderConnection;
            //GetTypeAndTimestamp();
        }

        public InboundMessage(OutboundMessage m)
        {
            Msg = m.Msg;
            Msg.Position = 0;
            Connection = null;
            //GetTypeAndTimestamp();
        }

        protected internal NetBuffer Msg { get; protected set; }
        public SequenceChannel? SequenceChannel { get { return null; } }//Msg.SequenceChannel; } }
        public uint? Timestamp { get; private set; }

        public NetConnection Connection { get; private set; }

        public override int SizeInBits { get { return Msg.LengthBits; } }

        public string ReadString() { return Msg.ReadString(); }
        public bool ReadBool() { return Msg.ReadBoolean(); }
        public byte ReadByte() { return Msg.ReadByte(); }
        public short ReadShort() { return Msg.ReadInt16(); }
        public ushort ReadUShort() { return Msg.ReadUInt16(); }
        public int ReadInt() { return Msg.ReadInt32(); }
        public uint ReadUInt() { return Msg.ReadUInt32(); }
        public long ReadLong() { return Msg.ReadInt64(); }
        public ulong ReadULong() { return Msg.ReadUInt64(); }
        public float ReadFloat() { return Msg.ReadSingle(); }
        public double ReadDouble() { return Msg.ReadDouble(); }
        public byte[] ReadBytes(int num) { return Msg.ReadBytes(num); }

        public void ResetRead()
        {
            Msg.Position = 0;
            //GetTypeAndTimestamp();
        }
        /*
        private void GetTypeAndTimestamp()
        {
            Type = ReadByte();
            if (Type == (byte)DefaultMessageIDTypes.ID_TIMESTAMP)
            {
                Timestamp = ReadUInt();
                Type = ReadByte();
            }
        }
        */
        public bool HasMoreData()
        {
            return Msg.Position < Msg.LengthBits;
        }
    }


    public class OutboundMessage : Message
    {
        /// <summary>
        /// Creating a message to send
        /// </summary>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="reliability"></param>
        private OutboundMessage(byte type, MessageReliabililty reliability, SequenceChannel? sequenceChannel = null)
        {
            Msg = NetworkManager.Instance.CreateOutgoing();
            Reliability = reliability;
            SequenceChannel = sequenceChannel;

            Type = type;
            Msg.Write(Type);
        }

        public static OutboundMessage CreateReliable(byte type, bool skipOld, SequenceChannel channel)
        {
            return new OutboundMessage(type, skipOld ? MessageReliabililty.ReliableSkipOld : MessageReliabililty.Reliable, channel);
        }

        public static OutboundMessage CreateUnreliable(byte type)
        {
            return new OutboundMessage(type, MessageReliabililty.Unreliable, null);
        }

        protected internal NetOutgoingMessage Msg { get; private set; }
        public MessageReliabililty Reliability { get; protected set; }
        public SequenceChannel? SequenceChannel { get; set; }
        public uint? Timestamp { get; private set; }

        public override int SizeInBits { get { return (int)Msg.Position; } }

        //public byte Type { get; private set; }

        public void Write(string val) { Msg.Write(val); }
        public void Write(bool val) { Msg.Write(val); }
        public void Write(byte val) { Msg.Write(val); }
        public void Write(short val) { Msg.Write(val); }
        public void Write(ushort val) { Msg.Write(val); }
        public void Write(int val) { Msg.Write(val); }
        public void Write(uint val) { Msg.Write(val); }
        public void Write(long val) { Msg.Write(val); }
        public void Write(ulong val) { Msg.Write(val); }
        public void Write(float val) { Msg.Write(val); }
        public void Write(double val) { Msg.Write(val); }
        public void Write(byte[] val) { Msg.Write(val); }
    }
}

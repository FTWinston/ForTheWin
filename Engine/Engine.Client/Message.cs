using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using RakNet;

namespace FTW.Engine.Client
{
    public class Message : MessageBase
    {
        public Message(byte type, PacketPriority priority, PacketReliability reliability)
            : base(type, priority, reliability)
        {
        }
    }

    public class NewClientInfoMessage : Message
    {
        public NewClientInfoMessage(string clientName)
            : base((byte)EngineMessage.NewClientInfo, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE)
        {
            Stream.Write(clientName);
        }

        public static void Read(Packet p)
        {
            BitStream bs = GetStream(p);
            string desiredName;
            bs.Read(out desiredName);

            localClient.Name = desiredName;
        }
    }
}

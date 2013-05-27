using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using RakNet;

namespace FTW.Engine.Server
{
    public class Message : MessageBase
    {
        public Message(byte type, PacketPriority priority, PacketReliability reliability)
            : base(type, priority, reliability)
        {
        }

        public void SendTo(Client c)
        {
            GameServer.Instance.rakNet.Send(Stream, Priority, Reliability, (char)0, c.UniqueID, false);
        }

        public void SendToAll()
        {
            GameServer.Instance.rakNet.Send(Stream, Priority, Reliability, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
        }

        public void SendToAllExcept(Client c)
        {
            GameServer.Instance.rakNet.Send(Stream, Priority, Reliability, (char)0, c.UniqueID, true);
        }
    }



    public class NewClientInfoMessage : Message
    {
        public NewClientInfoMessage(Client c)
            : base((byte)EngineMessage.NewClientInfo, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE)
        {
            Stream.Write(c.Name);

            List<Client> otherClients = Client.GetAllExcept(c);
            Stream.Write((byte)otherClients.Count);
            foreach (Client other in otherClients)
                Stream.Write(other.Name);
        }

        public static void Read(Client c, Packet p)
        {
            BitStream bs = GetStream(p);
            string desiredName;
            bs.Read(out desiredName);

            c.Name = desiredName;
        }
    }

    public class ClientConnectedMessage : Message
    {
        public ClientConnectedMessage(Client c)
            : base((byte)EngineMessage.ClientConnected, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE)
        {
            Stream.Write(c.Name);
        }
    }
}

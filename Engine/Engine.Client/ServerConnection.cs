using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using RakNet;

namespace FTW.Engine.Client
{
    public abstract class ServerConnection
    {
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void RetrieveUpdates();

        public abstract void Send(Message m);
        public bool MessageReceived(Message m)
        {
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.ClientConnected:
                    {
                        string clientName = m.ReadString();

                        Console.WriteLine(clientName + " joined the game");
                        return true;
                    }
                case EngineMessage.PlayerList:
                    {
                        string clientName = m.ReadString();
                        Console.WriteLine("My name, corrected by server: " + clientName);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine("There are " + numOthers + " other clients connected to this server:");

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        GameRenderer.Instance.FullyConnected = true;
                        return true;
                    }
                default:
                    return false;
            }
        }
    }

    public class ListenServerConnection : ServerConnection
    {
        const string settingsFilename = "settings.yml";
        ServerBase server;

        public override void Connect()
        {
            server = ServerBase.CreateReflection();

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = server.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            server.Start(false, config);
            GameRenderer.Instance.FullyConnected = true;

            Message m = new Message((byte)EngineMessage.ClientConnecting, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE);
            m.Write("ClientName");
            Send(m);
        }

        public override void Disconnect()
        {
            server.Stop();
        }

        public override void RetrieveUpdates()
        {
            Message[] messages;
            lock (Message.ToLocalClient)
            {
                messages = Message.ToLocalClient.ToArray();
                Message.ToLocalClient.Clear();
            }

            foreach (Message m in messages)
                MessageReceived(m);
        }

        public override void Send(Message m)
        {
            lock (Message.ToLocalServer)
                Message.ToLocalServer.Add(m);
        }
    }

    public class RemoteClientConnection : ServerConnection
    {
        public RemoteClientConnection(string hostname, ushort port)
        {
            this.hostname = hostname;
            this.hostPort = port;
        }

        private string hostname;
        private ushort hostPort;
        RakPeerInterface rakNet;

        public override void Connect()
        {
            rakNet = RakPeerInterface.GetInstance();
            rakNet.Startup(1, new SocketDescriptor(), 1);
            rakNet.Connect(hostname, hostPort, string.Empty, 0);
        }

        public override void Disconnect()
        {
            rakNet.Shutdown(300);
            RakPeerInterface.DestroyInstance(rakNet);
            rakNet = null;
        }

        public override void RetrieveUpdates()
        {
            if (rakNet == null)
                return;

            Packet packet;
            for (packet = rakNet.Receive(); packet != null; rakNet.DeallocatePacket(packet), packet = rakNet.Receive())
            {
                byte type = packet.data[0];
                if (type < (byte)DefaultMessageIDTypes.ID_USER_PACKET_ENUM)
                    switch ((DefaultMessageIDTypes)type)
                    {
                        case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED: // now properly connected. send client info? (e.g. name)
                            Console.WriteLine("Connected to server");

                            // server will respond to this with a NewClientInfo message of its own
                            Message m = new Message((byte)EngineMessage.ClientConnecting, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE);
                            m.Write("ClientName");
                            Send(m);
                            break;
                        case DefaultMessageIDTypes.ID_NO_FREE_INCOMING_CONNECTIONS:
                            Console.WriteLine("Unable to connect: the server is full");
                            GameRenderer.Instance.Disconnect();
                            return;
                        case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION: // server disconnected me. kicked, shutdown, or what?
                            Console.WriteLine("You have been kicked from the server");
                            GameRenderer.Instance.Disconnect();
                            return;
                        case DefaultMessageIDTypes.ID_CONNECTION_LOST:
                            Console.WriteLine("Lost connection to the server");
                            GameRenderer.Instance.Disconnect();
                            return;
#if DEBUG
                        default:
                            Console.WriteLine("Received a " + (DefaultMessageIDTypes)type + " packet, " + packet.length + " bytes long");
                            break;
#endif
                    }
                else
                    MessageReceived(new Message(packet));
            }
        }

        public override void Send(Message m)
        {
            rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
        }
    }
}

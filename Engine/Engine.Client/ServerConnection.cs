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
        }

        public override void Disconnect()
        {
            server.Stop();
        }

        public override void RetrieveUpdates()
        {
            throw new NotImplementedException();
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
                DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];

                switch (messageType)
                {
                    case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED: // now properly connected. send client info? (e.g. name)
                        Console.WriteLine("Connected to server");

                        BitStream bs = new BitStream();
                        bs.Write("hello, my name is SOMETHING");

                        rakNet.Send(bs, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);

                        // we actually need to wait for the server's response to packet we just sent, but go ahead for now
                        GameRenderer.Instance.FullyConnected = true;
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
                    default:
                        Console.WriteLine("Received a " + messageType + " packet, " + packet.length + " bytes long");
                        break;
                }
            }
        }
    }
}

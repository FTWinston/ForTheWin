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

            BitStream bs = new BitStream();
            bs.Write("hello, my name is SOMETHING");

            rakNet.Send(bs, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
        }

        public override void Disconnect()
        {
            rakNet.Shutdown(300);
            RakPeerInterface.DestroyInstance(rakNet);
            rakNet = null;
        }

        public override void RetrieveUpdates()
        {
            Packet packet;
            for (packet = rakNet.Receive(); packet != null; rakNet.DeallocatePacket(packet), packet = rakNet.Receive())
            {
                Console.WriteLine("Received a packet, " + packet.length + " bytes long");
            }
        }
    }
}

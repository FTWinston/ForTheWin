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
    }

    public class ListenServerConnection : ServerConnection
    {
        const string settingsFilename = "settings.yml";

        public override void Connect()
        {
            ServerBase server = ServerBase.CreateReflection();

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = server.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            server.Start(false, config);
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

        public override void Connect()
        {
            var peer = RakPeerInterface.GetInstance();
            peer.Startup(1, new SocketDescriptor(), 1);
            peer.Connect(hostname, hostPort, string.Empty, 0);
        }
    }
}

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
        RakPeerInterface connection;

        public override void Connect()
        {
            connection = RakPeerInterface.GetInstance();
            connection.Startup(1, new SocketDescriptor(), 1);
            connection.Connect(hostname, hostPort, string.Empty, 0);
        }

        public override void Disconnect()
        {
            connection.Shutdown(300);
            RakPeerInterface.DestroyInstance(connection);
            connection = null;
        }
    }
}

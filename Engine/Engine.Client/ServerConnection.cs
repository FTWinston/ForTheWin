using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    internal abstract class ServerConnection
    {
        public abstract bool IsLocal { get; }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract void RetrieveUpdates();
        public abstract void Send(OutboundMessage m);

        protected void SendClientInfo()
        {
            var m = OutboundMessage.CreateReliable((byte)EngineMessage.InitialData, false, SequenceChannel.System);
            short numVars = 0;
            var allVars = Variable.GetEnumerable();

            foreach (var v in allVars)
                if (v.HasFlags(VariableFlags.Client))
                    numVars++;
            m.Write(numVars);

            foreach (var v in allVars)
                if (v.HasFlags(VariableFlags.Client))
                {
                    m.Write(v.Name);
                    m.Write(v.Value);
                }
            Send(m);
        }
/*
Client connects, *automatically* sends ID_NEW_INCOMING_CONNECTION
Server *automatically* responds with ID_CONNECTION_REQUEST_ACCEPTED (or ID_NO_FREE_INCOMING_CONNECTIONS)
Client sends InitialData listing all variables
Server responds with InitialData listing all ITS variables
We are then good to go
*/
    }

    internal class ListenServerConnection : ServerConnection
    {
        const string settingsFilename = "server.yml";
        ServerBase server;

        public override bool IsLocal { get { return true; } }

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
            SendClientInfo();
        }

        public override void Disconnect()
        {
            server.Stop();
        }

        public override void RetrieveUpdates()
        {
            OutboundMessage[] messages;
            lock (Message.ToLocalClient)
            {
                messages = Message.ToLocalClient.ToArray();
                Message.ToLocalClient.Clear();
            }

            foreach (OutboundMessage m in messages)
                GameClient.Instance.HandleMessage(new InboundMessage(m));
        }

        public override void Send(OutboundMessage m)
        {
            lock (Message.ToLocalServer)
                Message.ToLocalServer.Add(m);
        }

        public void ConsoleCommand(string cmd)
        {
            server.HandleCommand(cmd);
        }
    }

    internal class RemoteClientConnection : ServerConnection
    {
        public RemoteClientConnection(string hostname, ushort port)
        {
            this.hostname = hostname;
            this.hostPort = port;
        }

        private string hostname;
        private ushort hostPort;
        ClientNetworking Networking;

        public override bool IsLocal { get { return false; } }

        public override void Connect()
        {
            Networking = new ClientNetworking(hostname, hostPort);
            Networking.Connected += (o, e) => SendClientInfo();
            Networking.Disconnected += (o, e) =>
            {
                Console.WriteLine("Lost connection to the server for some reason. Full? Kicked? Timed out?");
                GameClient.Instance.Disconnect();
            };
        }

        public override void Disconnect()
        {
            Networking.Disconnect("Disconnected by user");
            Networking = null;
        }

        public override void RetrieveUpdates()
        {
            if (Networking == null)
                return;

            foreach (var msg in Networking.RetrieveMessages())
                GameClient.Instance.HandleMessage(msg);
        }

        public override void Send(OutboundMessage m)
        {
            Networking.Send(m);
        }
    }
}

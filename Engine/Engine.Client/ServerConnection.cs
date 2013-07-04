using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using RakNet;

namespace FTW.Engine.Client
{
    internal abstract class ServerConnection
    {
        public abstract bool IsLocal { get; }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract void RetrieveUpdates();
        public abstract void Send(Message m);

        protected void SendClientInfo()
        {
            Message m = new Message((byte)EngineMessage.InitialData, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, 0);
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
            Message[] messages;
            lock (Message.ToLocalClient)
            {
                messages = Message.ToLocalClient.ToArray();
                Message.ToLocalClient.Clear();
            }

            foreach (Message m in messages)
                GameClient.Instance.HandleMessage(m);
        }

        public override void Send(Message m)
        {
            m.ResetRead();

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
        RakPeerInterface rakNet;

        public override bool IsLocal { get { return false; } }

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

            GameClient client = GameClient.Instance;
            Packet packet;
            for (packet = rakNet.Receive(); packet != null; rakNet.DeallocatePacket(packet), packet = rakNet.Receive())
            {
                byte type = packet.data[0];
                if (type == (byte)DefaultMessageIDTypes.ID_TIMESTAMP)
                    type = packet.data[5]; // skip the timestamp to get to the REAL "type"

                if (type < (byte)DefaultMessageIDTypes.ID_USER_PACKET_ENUM)
                    switch ((DefaultMessageIDTypes)type)
                    {
                        case DefaultMessageIDTypes.ID_CONNECTION_REQUEST_ACCEPTED: // now properly connected. send client info? (e.g. name)
                            Console.WriteLine("Connected to server");
                            SendClientInfo();
                            break;
                        case DefaultMessageIDTypes.ID_NO_FREE_INCOMING_CONNECTIONS:
                            Console.WriteLine("Unable to connect: the server is full");
                            client.Disconnect();
                            return;
                        case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION: // server disconnected me. kicked, shutdown, or what?
                            Console.WriteLine("You have been kicked from the server");
                            client.Disconnect();
                            return;
                        case DefaultMessageIDTypes.ID_CONNECTION_LOST:
                            Console.WriteLine("Lost connection to the server");
                            client.Disconnect();
                            return;
#if DEBUG
                        default:
                            Console.WriteLine("Received a " + (DefaultMessageIDTypes)type + " packet, " + packet.length + " bytes long");
                            break;
#endif
                    }
                else
                    client.HandleMessage(new Message(packet));
            }
        }

        public override void Send(Message m)
        {
            rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
        }
    }
}

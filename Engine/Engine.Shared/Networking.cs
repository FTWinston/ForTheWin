using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace FTW.Engine.Shared
{
    public abstract class Networking
    {
        public static Networking Instance { get; private set; }
        protected Networking() { Instance = this; }

        protected abstract NetPeer lidgren { get; }

        public void Disconnect(string reason)
        {
            lidgren.Shutdown(reason);
        }

        internal NetOutgoingMessage CreateOutgoing()
        {
            return lidgren.CreateMessage();
        }

        internal virtual ServerNetworking.Connection GetConnection(NetConnection con) { return null; }

        public event EventHandler<StatusEventArgs> Connected, Disconnected;

        public IEnumerable<InboundMessage> RetrieveMessages()
        {
            NetIncomingMessage msg;
            while ((msg = lidgren.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
#if DEBUG
                    case NetIncomingMessageType.DebugMessage:
#endif
                    case NetIncomingMessageType.WarningMessage:
                        Console.WriteLine(msg.ReadString());
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        Console.Error.WriteLine(msg.ReadString());
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        var status = (NetConnectionStatus)msg.ReadByte();

                        switch (status)
                        {
                            case NetConnectionStatus.Connected:
                                if (Connected != null)
                                    Connected(this, new StatusEventArgs { Connection = new ServerNetworking.Connection(msg.SenderConnection) });
                                break;
                            case NetConnectionStatus.Disconnected:
                                if (Disconnected != null)
                                    Disconnected(this, new StatusEventArgs { Connection = GetConnection(msg.SenderConnection) });
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        yield return new InboundMessage(msg);
                        break;
                }

                lidgren.Recycle(msg);
            }
        }

        protected NetDeliveryMethod DeliveryMethod(OutboundMessage msg)
        {
            return (NetDeliveryMethod)msg.Reliability;
        }

        public class StatusEventArgs : EventArgs
        {
            public ServerNetworking.Connection Connection { get; internal set; }
        }
    }

    public class ServerNetworking : Networking
    {
        NetServer server;
        protected override NetPeer lidgren { get { return server; } }

        public ServerNetworking(int port, int maxClients)
        {
            var config = new NetPeerConfiguration("ftw");
            config.Port = port;
            config.MaximumConnections = maxClients;

            server = new NetServer(config);
            server.Start();

            Disconnected += ClientDisconnected;
        }

        private void ClientDisconnected(object sender, StatusEventArgs e)
        {
            Connections.Remove(e.Connection.UniqueID);
        }

        public void Send(OutboundMessage message, Connection recipient)
        {
            server.SendMessage(message.Msg, recipient.Remote, DeliveryMethod(message));
        }

        public void SendToAllExcept(OutboundMessage message, Connection exclude)
        {
            var recipients = server.Connections.Where(con => con != exclude.Remote).ToList();
            if (recipients.Count != 0)
                server.SendMessage(message.Msg, recipients, DeliveryMethod(message), 0);
        }

        public void SendToAll(OutboundMessage message)
        {
            server.SendToAll(message.Msg, DeliveryMethod(message));
        }

        SortedList<long, Connection> Connections = new SortedList<long, Connection>();

        internal override Connection GetConnection(NetConnection con)
        {
            Connection c;
            Connections.TryGetValue(con.RemoteUniqueIdentifier, out c);
            return c;
        }

        public class Connection
        {
            internal Connection(NetConnection remote) { Remote = remote; }
            internal NetConnection Remote { get; set; }
            public long UniqueID { get { return Remote.RemoteUniqueIdentifier; } }
            public string RemoteHailMessage { get { return Remote.RemoteHailMessage.ReadString(); } }
        }
    }

    public class ClientNetworking : Networking
    {
        NetClient client;
        protected override NetPeer lidgren { get { return client; } }

        public ClientNetworking()
        {
            var config = new NetPeerConfiguration("ftw");
            client = new NetClient(config);
        }

        public ClientNetworking(string hostname, int hostPort)
        {
            var config = new NetPeerConfiguration("ftw");
            //config.Port = NetworkPort;
            config.MaximumConnections = 1;
            client = new NetClient(config);
            client.Start();
            client.Connect(hostname, hostPort, client.CreateMessage("this is the client's hail message"));
        }

        public void Send(OutboundMessage message)
        {
            client.SendMessage(message.Msg, DeliveryMethod(message));
        }
    }
}

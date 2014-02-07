using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace FTW.Engine.Shared
{
    public abstract class NetworkManager
    {
        public static NetworkManager Instance { get; private set; }
        protected NetworkManager() { Instance = this; }

        protected abstract NetPeer lidgren { get; }

        public void Disconnect(string reason)
        {
            lidgren.Shutdown(reason);
        }

        internal NetOutgoingMessage CreateOutgoing()
        {
            return lidgren.CreateMessage();
        }

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
                                    Connected(this, new StatusEventArgs { Connection = msg.SenderConnection });
                                break;
                            case NetConnectionStatus.Disconnected:
                                if (Disconnected != null)
                                    Disconnected(this, new StatusEventArgs { Connection = msg.SenderConnection });
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
            public NetConnection Connection { get; internal set; }
        }
    }

    public class NetworkServer : NetworkManager
    {
        NetServer server;
        protected override NetPeer lidgren { get { return server; }}

        public NetworkServer(int port, int maxClients)
        {
            var config = new NetPeerConfiguration("ftw");
            config.Port = port;
            config.MaximumConnections = maxClients;

            server = new NetServer(config);
            server.Start();
        }

        public void Send(OutboundMessage message, NetConnection recipient)
        {
            server.SendMessage(message.Msg, recipient, DeliveryMethod(message));
        }

        public void SendToAllExcept(OutboundMessage message, NetConnection exclude)
        {
            var recipients = server.Connections.Where(con => con != exclude).ToList();
            server.SendMessage(message.Msg, recipients, DeliveryMethod(message), 0);
        }

        public void SendToAll(OutboundMessage message)
        {
            server.SendToAll(message.Msg, DeliveryMethod(message));
        }
    }

    public class NetworkClient : NetworkManager
    {
        NetClient client;
        protected override NetPeer lidgren { get { return client; } }

        public NetworkClient(string hostname, int hostPort)
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

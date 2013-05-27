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
            //throw new NotImplementedException();

            // i was gonna have this message just copy the contents from a List<Message> on the server, which contains all those to be sent to the local client
            // but server and client messages have diverged now.

            // if we do away with reusing the same message for client -> server AND server -> client, and replace the .Read functions with things in the Connection / Server classes, that might work.
            // alternatively, if it's just a list of MessageBase, we can perhaps still get away with reusing the same message IDs. Read functions would still have to be outwith the message classes.
        }

        public override void Send(Message m)
        {
            //throw new NotImplementedException();
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
                            Send(new NewClientInfoMessage("ClientName"));
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
                else if (type <= (byte)EngineMessage.FirstGameMessageID)
                    switch ((EngineMessage)type)
                    {
                        case EngineMessage.NewClientInfo:
                            {
                                //NewClientInfoMessage.Read(packet);

                                BitStream bs = new BitStream(packet.data, packet.length, false);
                                bs.IgnoreBytes(1);

                                string clientName;
                                bs.Read(out clientName);
                                Console.WriteLine("My name, corrected by server: " + clientName);

                                byte numOthers;
                                bs.Read(out numOthers);

                                Console.WriteLine("There are " + numOthers + " other clients connected to this server:");

                                for (int i = 0; i < numOthers; i++)
                                {
                                    string otherName;
                                    bs.Read(out otherName);
                                    Console.WriteLine(" * " + otherName);
                                }

                                GameRenderer.Instance.FullyConnected = true;
                                break;
                            }
                        case EngineMessage.ClientConnected:
                            {
                                BitStream bs = new BitStream(packet.data, packet.length, false);
                                bs.IgnoreBytes(1);

                                string clientName;
                                bs.Read(out clientName);

                                Console.WriteLine(clientName + " joined the game");
                                break;
                            }
                    }
            }
        }

        public override void Send(Message m)
        {
            rakNet.Send(m.Stream, m.Priority, m.Reliability, (char)0, RakNet.RakNet.UNASSIGNED_RAKNET_GUID, true);
        }
    }
}

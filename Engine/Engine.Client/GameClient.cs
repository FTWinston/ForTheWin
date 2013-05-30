using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    public abstract class GameClient
    {
        public static GameClient Instance;
        internal ServerConnection Connection { get; private set; }
        public bool Connected { get { return Connection != null; } }
        public bool FullyConnected { get; internal set; }

        private string name;
        public string Name 
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;

                if (FullyConnected)
                {
                    Message m = new Message((byte)EngineMessage.ClientNameChange, RakNet.PacketPriority.HIGH_PRIORITY, RakNet.PacketReliability.RELIABLE_SEQUENCED);
                    m.Write(value);
                    SendMessage(m);
                }
                else if (Connected)
                {
                    Console.Error.WriteLine("Can't change name while connecting to server!");
                    return;
                }

                name = value;
            }
        }

        private const string defaultClientName = "unnamed";
        protected GameClient(Config settings)
        {
            Instance = this;
            FullyConnected = false;

            Name = settings.FindValueOrDefault("name", defaultClientName);
        }

        public void ConnectLocal()
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new ListenServerConnection();
            Connection.Connect();
        }

        public void ConnectRemote(string hostname, ushort port)
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new RemoteClientConnection(hostname, port);
            Connection.Connect();
        }

        public void Disconnect()
        {
            Connection.Disconnect();

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);

            Connection = null;
            FullyConnected = false;
        }

        public event EventHandler Disconnected;

        public void Update()
        {
            Connection.RetrieveUpdates();

        }

        internal void HandleMessage(Message m)
        {
            if (!MessageReceived(m))
                Console.Error.WriteLine("Received an unrecognised message of Type " + m.Type);
        }

        protected internal virtual bool MessageReceived(Message m)
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
                        Name = m.ReadString();
                        Console.WriteLine("My name, corrected by server: " + Name);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine("There are " + numOthers + " other clients connected to this server:");

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        GameClient.Instance.FullyConnected = true;
                        return true;
                    }
                default:
                    return false;
            }
        }

        public void SendMessage(Message m)
        {
            Connection.Send(m);
        }
    }
}

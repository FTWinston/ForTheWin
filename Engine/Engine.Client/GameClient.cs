using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Threading;

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

            SetupVariableDefaults();
            // read variables from config...
        }

        protected abstract void SetupVariableDefaults();

        public void ConnectLocal()
        {
            if (Connection != null)
            {
                Console.Error.WriteLine("Cannot connect, already connected!");
                return;
            }
            Connection = new ListenServerConnection();
            Connection.Connect();
            Initialize();
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
            Initialize();
        }

        public void Disconnect()
        {
            Connection.Disconnect();

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);

            Entity.AllEntities.Clear();
            Entity.NetworkedEntities.Clear();

            Connection = null;
            FullyConnected = false;
        }

        protected virtual void Initialize()
        {
            NetworkedEntity.InitializeTypes();

            dt = 100;
            FrameTime = lastFrameTime = RakNet.RakNet.GetTime() - dt;
        }

        public event EventHandler Disconnected;

        public uint FrameTime { get; private set; }
        public uint ServerTime { get; private set; }
        private uint lastFrameTime, dt;

        // this should really be a GameFrame type of affair, shouldn't it?
        public void Update()
        {
            lastFrameTime = FrameTime;
            FrameTime = RakNet.RakNet.GetTime();
            ServerTime = FrameTime - Snapshot.lerpDelay;

            /*if (Paused)
            {
                if (!pauseTime.HasValue)
                    pauseTime = DateTime.Now;
                Thread.Sleep(pauseTickMilliseconds);
                return;
            }
            else if (pauseTime.HasValue)
            {
                pauseTime = null;
                lastFrameTime = RakNet.RakNet.GetTime() - dt;
            }
            else*/
            {
                dt = FrameTime - lastFrameTime;
                lastFrameTime = FrameTime;
            }

            Connection.RetrieveUpdates();
            Snapshot.CheckQueue();

            GameFrame(dt / 1000.0); // convert milliseconds to seconds

            //Console.WriteLine("Frame duration: {0}ms", RakNet.RakNet.GetTime() - FrameTime);
        }

        private void GameFrame(double dt)
        {
            int i; Entity e;
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.PreThink(dt);
            }
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.Simulate(dt);
            }
            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (!e.IsDeleted)
                    e.PostThink(dt);
            }
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
                case EngineMessage.ServerInfo:
                    {
                        byte[] hash = new byte[16];
                        m.Stream.Read(hash, 128);
                        if (!NetworkedEntity.CheckNetworkTableHash(hash))
                        {
                            Console.Error.WriteLine("Network table doesn't match server's");
                            Disconnect();
                            return true;
                        }

                        name = m.ReadString(); // change name rather than Name, so as to automatically accept the change without re-sending.
                        Console.WriteLine("My name, corrected by server: " + Name);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine("There {0} {1} other {2} connected to this server{3}",
                            numOthers == 1 ? "is" : "are",
                            numOthers == 0 ? "no" : numOthers.ToString(),
                            numOthers == 1 ? "client" : "clients",
                            numOthers == 0 ? string.Empty : ":"
                            );

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        GameClient.Instance.FullyConnected = true;
                        return true;
                    }
                case EngineMessage.Snapshot:
                    {
                        Snapshot.Read(m);
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

        static readonly char[] cmdSplit = { ' ', '	' };
        public void HandleCommand(string cmd)
        {
            Console.WriteLine(cmd);

            // split the command into words, for ease of processing
            string[] words = cmd.Split(cmdSplit, 2, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return;

            // for the moment, trying the command on the client before the server (for local listen servers only). May want to swap that.
            if (!ConsoleCommand(words[0], words.Length > 1 ? words[1] : null))
            {
                // if this is a listen server, try running the command on the server. That'll report an error if it doesn't recognise the command either.
                if (FullyConnected && Connection.IsLocal)
                    (Connection as ListenServerConnection).ConsoleCommand(cmd);
                else
                    Console.Error.WriteLine("Command not recognised: " + words[0]);
            }
        }

        protected virtual bool ConsoleCommand(string firstWord, string theRest)
        {
            switch (firstWord)
            {

                default:
                    return false;
            }
        }
    }
}

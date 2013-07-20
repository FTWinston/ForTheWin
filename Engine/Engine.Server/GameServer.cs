using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FTW.Engine.Shared;
using RakNet;

namespace FTW.Engine.Server
{
    public abstract class GameServer : ServerBase
    {
        internal static GameServer Instance { get; private set; }

        protected GameServer()
        {
            if (Instance != null)
                Console.Error.WriteLine("GameServer.Create called, but Instance is not null: server is already running!");

            Instance = this;
        }

        const int defaultPort = 24680, defaultMaxClients = 8;
        const string defaultServerName = "Some server";

        public override Config CreateDefaultConfig()
        {
            Config config = new Config(null);
            config.Children = new List<Config>();

            Config value = new Config("name");
            value.Value = defaultServerName;
            config.Children.Add(value);

            value = new Config("port");
            value.Value = defaultPort.ToString();
            config.Children.Add(value);

            value = new Config("max-clients");
            value.Value = defaultMaxClients.ToString();
            config.Children.Add(value);

            return config;
        }

        public virtual void ApplyConfig(Config settings)
        {
            string strPort = settings.FindValueOrDefault("port", defaultPort.ToString());
            string strMaxClients = settings.FindValueOrDefault("max-clients", defaultPort.ToString());
            Name = settings.FindValueOrDefault("name", defaultServerName);

            ushort port, maxClients;
            if (!ushort.TryParse(strPort, out port))
                port = defaultPort;
            if (!ushort.TryParse(strMaxClients, out maxClients))
                maxClients = defaultMaxClients;

            if (maxClients == 0)
                maxClients = 1;
            if (maxClients > byte.MaxValue)
                maxClients = byte.MaxValue;

            NetworkPort = port;
            MaxClients = maxClients;

            // read variables from config... in fact, the things above could be variables read from config, I guess.
        }

        private Thread gameThread;

        public bool IsDedicated { get; private set; }
        public ushort MaxClients { get; set; }
        public ushort NetworkPort { get; private set; }
        public override string Name { get; protected set; }
        public bool IsMultiplayer { get { return IsDedicated || MaxClients > 1; } }

        private bool isRunning, isPaused;
        public override bool IsRunning { get { return isRunning; } }
        public override bool Paused { get { return isPaused; } }

        public override void Start(bool isDedicated, Config config)
        {
            IsDedicated = isDedicated;

            SetupVariableDefaults();
            ApplyConfig(config);

            isRunning = true;
            gameThread = new Thread(ThreadStart);
            gameThread.Start();
        }

        private void ThreadStart()
        {
            if (Initialize())
            {
                if (!IsDedicated)
                    LocalClient.Create("LocalClientName");
                RunMainLoop();
            }
            else
                isRunning = false;
        }

        protected abstract void SetupVariableDefaults();

        internal RakPeerInterface rakNet = null;
        protected virtual bool Initialize()
        {
            Console.WriteLine("Initializing...");
            NetworkedEntity.InitializeTypes();

            if (IsMultiplayer)
            {
                rakNet = RakPeerInterface.GetInstance();
                ushort numRemoteClients = IsDedicated ? MaxClients : (ushort)(MaxClients - 1);

                rakNet.Startup(numRemoteClients, new SocketDescriptor(NetworkPort, null), 1);
                rakNet.SetMaximumIncomingConnections(numRemoteClients);
                rakNet.SetOccasionalPing(true);
                Console.WriteLine("Network server started at time {0}", RakNet.RakNet.GetTime());
            }

            return true;
        }

        public override void Pause()
        {
            if (!Paused)
                Console.WriteLine("Game paused");
            isPaused = true;
        }

        public override void Resume()
        {
            if (Paused)
                Console.WriteLine("Game resumed");
            isPaused = false;
        }

        public override void Stop()
        {
            isRunning = false;
        }

        protected virtual void ShutDown()
        {
            Console.WriteLine("Server is shutting down");

            if (rakNet != null)
            {
                rakNet.Shutdown(300);
                RakPeerInterface.DestroyInstance(rakNet);
                rakNet = null;
            }

            if (Client.LocalClient != null)
                Client.LocalClient = null;

            Client.AllClients.Clear();
            Entity.AllEntities.Clear();
            Entity.NetworkedEntities.Clear();

            Message.ToLocalClient.Clear();
            Message.ToLocalServer.Clear();

            Instance = null;
        }

        protected internal uint TickInterval = 30;
        const int pauseTickMilliseconds = 100;
        
        public uint FrameTime { get; private set; }

        private void RunMainLoop()
        {
            Console.WriteLine("Server has started");
            isPaused = false;

            uint dt = 100;
            uint lastFrameTime = FrameTime = RakNet.RakNet.GetTime() - dt;
            DateTime? pauseTime = null;

            while (IsRunning)
            {
                lastFrameTime = FrameTime;
                FrameTime = RakNet.RakNet.GetTime();

                if (Paused)
                {
                    if (!pauseTime.HasValue)
                        pauseTime = DateTime.Now;
                    Thread.Sleep(pauseTickMilliseconds);
                    continue;
                }
                else if (pauseTime.HasValue)
                {
                    pauseTime = null;
                }
                else
                    dt = FrameTime - lastFrameTime;

                ReceiveMessages();
                GameFrame(dt/1000.0); // convert milliseconds to seconds

                int frameTimeRemaining = (int)(FrameTime + TickInterval) - (int)RakNet.RakNet.GetTime();
                if (frameTimeRemaining > 0)
                    Thread.Sleep(frameTimeRemaining);
            }

            ShutDown();
        }

        private void ReceiveMessages()
        {
            if (rakNet != null)
            {
                Packet packet;
                for (packet = rakNet.Receive(); packet != null; rakNet.DeallocatePacket(packet), packet = rakNet.Receive())
                {
                    Client c = Client.GetByUniqueID(packet.guid);
                    byte type = packet.data[0];
                    if (type == (byte)DefaultMessageIDTypes.ID_TIMESTAMP)
                        type = packet.data[5]; // skip the timestamp to get to the REAL "type"

                    if (type < (byte)DefaultMessageIDTypes.ID_USER_PACKET_ENUM)
                    {
                        switch ((DefaultMessageIDTypes)type)
                        {
                            case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                                if (c == null)
                                    c = RemoteClient.Create(packet.guid);

                                Console.WriteLine("Incoming connection...");
                                // the only response the client needs here is the automatic ID_CONNECTION_REQUEST_ACCEPTED packet
                                break;
                            case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                                Console.WriteLine(c.Name + " disconnected");
                                Client.AllClients.Remove(packet.guid.g);

                                break;
                            case DefaultMessageIDTypes.ID_CONNECTION_LOST:
                                Console.WriteLine(c.Name + "timed out");
                                Client.AllClients.Remove(packet.guid.g);
                                break;
#if DEBUG
                            default:
                                Console.WriteLine("Received a {0} packet from {1}, {2} bytes long", (DefaultMessageIDTypes)type, c == null ? "a new client" : c.Name, packet.length);
                                break;
#endif
                        }
                    }
                    else
                        HandleMessage(c, new Message(packet));
                }
            }

            if (Client.LocalClient != null)
            {
                Message[] messages;
                lock (Message.ToLocalServer)
                {
                    messages = Message.ToLocalServer.ToArray();
                    Message.ToLocalServer.Clear();
                }

                foreach (Message m in messages)
                    HandleMessage(Client.LocalClient, m);
            }
        }

        private void HandleMessage(Client c, Message m)
        {
            if (!MessageReceived(c, m))
                Console.Error.WriteLine("Received an unrecognised message from " + c.Name + " of Type " + m.Type);
        }

        protected virtual bool MessageReceived(Client c, Message m)
        {
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.InitialData:
                    {
                        short num = m.ReadShort();
                        for (int i = 0; i < num; i++)
                            c.SetVariable(m.ReadString(), m.ReadString());

                        string name = c.Name;

                        // tell all other clients about this new client
                        m = new Message((byte)EngineMessage.ClientConnected, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, 0);
                        m.Write(name);
                        Client.SendToAllExcept(m, c);

                        if (IsDedicated)
                            Console.WriteLine(name + " joined the game");

                        // send ServerInfo to the newly-connected client, which as well as the network table hash,
                        // tells them how/if we've modified their name, the names of everyone else on the server, and all the server variables

                        // is it worth having name be a variable here?

                        // how would name change work?
                        // when you try to change the name, it always fails, but sends a "name change" to the server
                        // that sends a "name change" to everyone else, and a special one back to you that actaully updates the variable
                        m = new Message((byte)EngineMessage.InitialData, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE, 0);
                        m.Stream.Write(NetworkedEntity.NetworkTableHash, (uint)128);
                        m.Write(c.Name);

                        List<Client> otherClients = Client.GetAllExcept(c);
                        m.Write((byte)otherClients.Count);
                        foreach (Client other in otherClients)
                            m.Write(other.Name);

                        ushort numVars = 0;
                        var allVars = Variable.GetEnumerable();

                        foreach (var v in allVars)
                            if (v.HasFlags(VariableFlags.Server))
                                numVars++;
                        m.Write(numVars);

                        foreach (var v in allVars)
                            if (v.HasFlags(VariableFlags.Server))
                            {
                                m.Write(v.Name);
                                m.Write(v.Value);
                            }

                        c.Send(m);
                        c.NextSnapshotTime = FrameTime + c.SnapshotInterval;
                        return true;
                    }
                case EngineMessage.ClientNameChange:
                    {
                        string oldName = c.Name;
                        string name = m.ReadString();
                        c.Name = name;
                        name = c.Name; // if it wasn't unique, it'll have been changed

                        // tell everyone else about the change
                        m = new Message((byte)EngineMessage.ClientNameChange, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, (int)OrderingChannel.Chat);
                        m.Write(name);
                        m.Write(false);
                        m.Write(oldName);
                        Client.SendToAllExcept(m, c);

                        // tell this client their "tidied" name
                        m = new Message((byte)EngineMessage.ClientNameChange, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE_ORDERED, (int)OrderingChannel.Chat);
                        m.Write(name);
                        m.Write(true);
                        c.Send(m);

                        if (IsDedicated)
                            Console.WriteLine("{0} changed name to {1}", oldName, name);

                        return true;
                    }
                case EngineMessage.VariableChange:
                    {
                        string name = m.ReadString();
                        string val = m.ReadString();

                        c.SetVariable(name, val);
                        return true;
                    }
                default:
                    return false;
            }
        }

        static readonly char[] cmdSplit = { ' ', '	' };
        public override void HandleCommand(string cmd)
        {
            // split the command into words, for ease of processing
            string[] words = cmd.Split(cmdSplit, 2, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return;

            // for the moment, trying the command on the client before the server (for local listen servers only). May want to swap that.
            if (!ConsoleCommand(words[0], words.Length > 1 ? words[1] : null))
                Console.Error.WriteLine("Command not recognised: {0}", words[0]);
        }

        static readonly char[] space = { ' ' };
        protected virtual bool ConsoleCommand(string firstWord, string theRest)
        {
            switch (firstWord)
            {
                case "get":
                    {
                        string name = theRest.Split(space)[0];
                        var vari = Variable.Get(name);
                        if (vari == null)
                            Console.WriteLine("Variable not recognised: {0}", name);
                        else
                            Console.WriteLine("{0}: {1}", vari.Name, vari.Value);
                        return true;
                    }
                case "set":
                    {
                        string[] parts = theRest.Split(space, 2);
                        var vari = Variable.Get(parts[0]);
                        if (vari == null)
                            Console.WriteLine("Variable not recognised: {0}", parts[0]);
                        else
                            vari.Value = parts.Length > 1 ? parts[1] : string.Empty;
                        return true;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// Runs a single frame of the game
        /// </summary>
        /// <param name="dt">Frame duration to simulate, in seconds</param>
        protected virtual void GameFrame(double dt)
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

            Client.SendSnapshots();

            for (i = 0; i < Entity.AllEntities.Count; i++)
            {
                e = Entity.AllEntities[i];
                if (e.IsDeleted)
                {
                    Entity.AllEntities.RemoveAt(i);
                    i--;
                }
            }
        }

        public virtual string ValidatePlayerName(string name) { return name.Trim(); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FTW.Engine.Shared;

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

        internal ServerNetworking Networking { get; private set; }
        protected virtual bool Initialize()
        {
            Console.WriteLine("Initializing...");
            NetworkedEntity.InitializeTypes();

            if (IsMultiplayer)
            {
                Networking = new ServerNetworking(NetworkPort, IsDedicated ? MaxClients : (MaxClients - 1));
                Networking.Connected += (o, e) =>
                {
                    RemoteClient.Create(e.Connection);
                    Console.WriteLine("Remote hail: " + e.Connection.RemoteHailMessage);
                };

                Networking.Disconnected += (o, e) =>
                {
                    long id = e.Connection.UniqueID;
                    bool deliberate = true; // true means disconnected, false means timed out
                    ClientDisconnected(Client.GetByID(id), deliberate);
                    Client.AllClients.Remove(id);
                };

                //Console.WriteLine("Network server started at time {0}", RakNet.RakNet.GetTime());
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

            if (Networking != null)
            {
                Networking.Disconnect("Server is shutting down");
                Networking = null;
            }

            if (Client.LocalClient != null)
                Client.LocalClient = null;

            Client.AllClients.Clear();
            Entity.AllEntities.Clear();
            Entity.NetworkedEntities.Clear();

            InboundMessage.ToLocalClient.Clear();
            InboundMessage.ToLocalServer.Clear();

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

                PreUpdate();
                GameFrame(dt/1000.0); // convert milliseconds to seconds
                PostUpdate();

                int frameTimeRemaining = (int)(FrameTime + TickInterval) - (int)RakNet.RakNet.GetTime();
                if (frameTimeRemaining > 0)
                    Thread.Sleep(frameTimeRemaining);
            }

            ShutDown();
        }

        protected virtual void PreUpdate()
        {

        }

        protected virtual void PostUpdate()
        {

        }

        private void ReceiveMessages()
        {
            if (Networking != null)
            {
                foreach (var msg in Networking.RetrieveMessages())
                {
                    Client c = Client.GetByID(msg.Connection.UniqueID);
                    HandleMessage(c, msg);
                }
            }

            if (Client.LocalClient != null)
            {
                OutboundMessage[] messages;
                lock (InboundMessage.ToLocalServer)
                {
                    messages = Message.ToLocalServer.ToArray();
                    InboundMessage.ToLocalServer.Clear();
                }

                foreach (OutboundMessage m in messages)
                    HandleMessage(Client.LocalClient, new InboundMessage(m));
            }
        }

        protected virtual void ClientConnected(Client c)
        {
            if (IsDedicated)
                Console.WriteLine(c.Name + " joined the game");
        }

        protected virtual void ClientDisconnected(Client c, bool manualDisconnect)
        {
            Console.WriteLine(c.Name + (manualDisconnect ? " disconnected" : " timed out"));
        }

        private void HandleMessage(Client c, InboundMessage m)
        {
            if (!MessageReceived(c, m))
                Console.Error.WriteLine("Received an unrecognised message from " + c.Name + " of Type " + m.Type);
        }

        protected virtual bool MessageReceived(Client c, InboundMessage m)
        {
            OutboundMessage o;
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.InitialData:
                    {
                        short num = m.ReadShort();
                        for (int i = 0; i < num; i++)
                            c.SetVariable(m.ReadString(), m.ReadString());

                        string name = c.Name;

                        // tell all other clients about this new client
                        o = OutboundMessage.CreateReliable((byte)EngineMessage.ClientConnected, false, SequenceChannel.System);
                        o.Write(name);
                        Client.SendToAllExcept(o, c);
                        
                        ClientConnected(c);

                        // send ServerInfo to the newly-connected client, which as well as the network table hash,
                        // tells them how/if we've modified their name, the names of everyone else on the server, and all the server variables

                        // is it worth having name be a variable here?

                        // how would name change work?
                        // when you try to change the name, it always fails, but sends a "name change" to the server
                        // that sends a "name change" to everyone else, and a special one back to you that actaully updates the variable
                        o = OutboundMessage.CreateReliable((byte)EngineMessage.InitialData, false, SequenceChannel.System);
                        o.Write(NetworkedEntity.NetworkTableHash);
                        o.Write(c.Name);

                        List<Client> otherClients = Client.GetAllExcept(c);
                        o.Write((byte)otherClients.Count);
                        foreach (Client other in otherClients)
                            o.Write(other.Name);

                        ushort numVars = 0;
                        var allVars = Variable.GetEnumerable();

                        foreach (var v in allVars)
                            if (v.HasFlags(VariableFlags.Server))
                                numVars++;
                        o.Write(numVars);

                        foreach (var v in allVars)
                            if (v.HasFlags(VariableFlags.Server))
                            {
                                o.Write(v.Name);
                                o.Write(v.Value);
                            }

                        c.Send(o);
                        c.NextSnapshotTime = FrameTime + c.SnapshotInterval;
                        c.FullyConnected = true;
                        return true;
                    }
                case EngineMessage.ClientNameChange:
                    {
                        string oldName = c.Name;
                        string name = m.ReadString();
                        c.Name = name;
                        name = c.Name; // if it wasn't unique, it'll have been changed

                        // tell everyone else about the change
                        o = OutboundMessage.CreateReliable((byte)EngineMessage.ClientNameChange, false, SequenceChannel.Chat);
                        o.Write(name);
                        o.Write(false);
                        o.Write(oldName);
                        Client.SendToAllExcept(o, c);

                        // tell this client their "tidied" name
                        o = OutboundMessage.CreateReliable((byte)EngineMessage.ClientNameChange, false, SequenceChannel.Chat);
                        o.Write(name);
                        o.Write(true);
                        c.Send(o);

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
                case EngineMessage.ClientUpdate:
                    {
                        if (!c.FullyConnected)
                            return true; // this might come through before the InitialData message

                        // read the "latest snapshot" info from the message

                        UpdateReceived(c, m);
                        return true;
                    }
                default:
                    return false;
            }
        }

        protected abstract void UpdateReceived(Client c, InboundMessage m);

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
                        if (theRest == null)
                            return true;
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
                        if (theRest == null)
                            return true;
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

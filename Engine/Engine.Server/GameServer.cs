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

        internal RakPeerInterface rakNet = null;
        protected virtual bool Initialize()
        {
            Console.WriteLine("Initializing...");

            if (IsMultiplayer)
            {
                rakNet = RakPeerInterface.GetInstance();
                ushort numRemoteClients = IsDedicated ? MaxClients : (ushort)(MaxClients - 1);

                rakNet.Startup(numRemoteClients, new SocketDescriptor(NetworkPort, null), 1);
                rakNet.SetMaximumIncomingConnections(numRemoteClients);
                rakNet.SetOccasionalPing(true);
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

            Instance = null;
        }

        protected double TargetFrameInterval = 1f / 30f;

        const int pauseTickMilliseconds = 100;

        public DateTime FrameTime { get; private set; }
        private DateTime lastFrameTime;
        private double dt;

        private void RunMainLoop()
        {
            Console.WriteLine("Server has started");
            isPaused = false;

            dt = 0.1;
            lastFrameTime = DateTime.Now.AddSeconds(-dt);
            DateTime? pauseTime = null;

            while (IsRunning)
            {
                FrameTime = DateTime.Now;

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
                    lastFrameTime = DateTime.Now.AddSeconds(-dt);
                }
                else
                {
                    TimeSpan duration = (FrameTime - lastFrameTime);
                    dt = duration.TotalSeconds;
                    lastFrameTime = FrameTime - duration;
                }

                ReceiveMessages();

                GameFrame(dt);

                TimeSpan frameTimeRemaining = FrameTime.AddSeconds(TargetFrameInterval) - DateTime.Now;
                if (frameTimeRemaining > TimeSpan.Zero)
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
                                Console.WriteLine(string.Format("Received a {0} packet from {1}, {2} bytes long", (DefaultMessageIDTypes)type, c == null ? "a new client" : c.Name, packet.length));
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
                case EngineMessage.ClientConnecting:
                    {
                        // if it's already in use, this name will be corrected automatically
                        c.Name = m.ReadString();

                        // tell all other clients about this new client
                        m = new Message((byte)EngineMessage.ClientConnected, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE);
                        m.Write(c.Name);
                        Client.SendToAllExcept(m, c);

                        if ( !IsDedicated )
                            Console.WriteLine(c.Name + " joined the game");

                        // send a PlayerList to the newly-connected client, telling them how/if we've modified their name
                        // and the names of everyone else on the server
                        m = new Message((byte)EngineMessage.PlayerList, PacketPriority.HIGH_PRIORITY, PacketReliability.RELIABLE);
                        m.Write(c.Name);

		                List<Client> otherClients = Client.GetAllExcept(c);
		                m.Write((byte)otherClients.Count);
		                foreach (Client other in otherClients)
                            m.Write(other.Name);

                        c.Send(m);
                        return true;
                    }
                case EngineMessage.ClientNameChange:
                    {
                        /*BitStream bs = new BitStream(packet.data, packet.length, false);
                        bs.IgnoreBytes(1);

                        string newName, oldName = c.Name;
                        bs.Read(out newName);
                        c.Name = newName;

                        Console.WriteLine(oldName + " changed name to " + c.Name);*/
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
                Console.Error.WriteLine("Command not recognised: " + words[0]);
        }

        protected virtual bool ConsoleCommand(string firstWord, string theRest)
        {
            switch (firstWord)
            {

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
            
        }

        public virtual string ValidatePlayerName(string name) { return name.Trim(); }
    }
}

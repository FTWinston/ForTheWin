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
                    LocalClient.Create("player name");
                RunMainLoop();
            }
            else
                isRunning = false;
        }

        private RakPeerInterface rakNet = null;
        protected virtual bool Initialize()
        {
            Console.WriteLine("Initializing...");

            if (IsMultiplayer)
            {
                rakNet = RakPeerInterface.GetInstance();
                ushort numRemoteClients = IsDedicated ? MaxClients : (ushort)(MaxClients - 1);

                rakNet.Startup(numRemoteClients, new SocketDescriptor(NetworkPort, null), 1);
                rakNet.SetMaximumIncomingConnections(numRemoteClients);
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
            if (rakNet == null)
                return;

            Packet packet;
            for (packet = rakNet.Receive(); packet != null; rakNet.DeallocatePacket(packet), packet = rakNet.Receive())
            {
                DefaultMessageIDTypes messageType = (DefaultMessageIDTypes)packet.data[0];

                Client c = Client.GetByUniqueID(packet.guid);
                switch (messageType)
                {
                    case DefaultMessageIDTypes.ID_NEW_INCOMING_CONNECTION:
                        if (c == null)
                            c = RemoteClient.Create("unknown", packet.guid);

                        Console.WriteLine("Incoming connection...");

                        // do we need to send them anything at this point?
                        // or do we wait for them to send us stuff first?
                        break;
                    case DefaultMessageIDTypes.ID_DISCONNECTION_NOTIFICATION:
                        Console.WriteLine(c.Name + " disconnected");
                        Client.AllClients.Remove(packet.guid.g);
                        break;
                    case DefaultMessageIDTypes.ID_CONNECTION_LOST:
                        Console.WriteLine(c.Name + "timed out");
                        Client.AllClients.Remove(packet.guid.g);
                        break;
                    default:
                        Console.WriteLine(string.Format("Received a {0} packet from {1}, {2} bytes long", messageType, c == null ? "a new client" : c.Name, packet.length));
                        break;
                }
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

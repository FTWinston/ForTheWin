using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FTW.Engine.Shared;

namespace FTW.Engine.Server
{
    public abstract class GameServer
    {
        internal static GameServer Instance { get; private set; }

        protected GameServer(bool isMultiplayer, bool isDedicated)
        {
            if (Instance != null)
                Console.Error.WriteLine("GameServer.Create called, but Instance is not null: server is already running!");

            Instance = this;
            IsDedicated = isDedicated;
        }

        const int defaultPort = 24680, defaultMaxClients = 8;
        const string defaultServerName = "Some server";

        public virtual Config CreateDefaultConfig()
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

            int port, maxClients;
            if (!int.TryParse(strPort, out port))
                port = defaultPort;
            if (!int.TryParse(strMaxClients, out maxClients))
                maxClients = defaultMaxClients;

            NetworkPort = port;
            MaxClients = maxClients;
        }

        private Thread gameThread;

        public bool IsDedicated { get; private set; }
        public int MaxClients { get; set; }
        public int NetworkPort { get; private set; }
        public string Name { get; private set; }
        public bool IsMultiplayer { get { return MaxClients != 1; } }

        public bool IsRunning { get; private set; }
        public bool Paused { get; private set; }

        public void Start(Config config)
        {
            ApplyConfig(config);

            IsRunning = true;
            gameThread = new Thread(ThreadStart);
            gameThread.Start();
        }

        private void ThreadStart()
        {
            if ( Initialize() )
                RunMainLoop();
            else
                IsRunning = false;
        }

        protected virtual bool Initialize()
        {
            Console.WriteLine("Initializing...");
            return true;
        }

        public void Pause()
        {
            if (!Paused)
                Console.WriteLine("Game paused");
            Paused = true;
        }

        public void Resume()
        {
            if (Paused)
                Console.WriteLine("Game resumed");
            Paused = false;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        protected virtual void ShutDown()
        {
            Console.WriteLine("Server is shutting down");
            Instance = null;
        }

        protected double TargetFrameInterval = 1f / 30f;

        const int pauseTickMilliseconds = 100;

        public DateTime FrameTime { get; private set; }
        private DateTime lastFrameTime;
        private double dt;

        private void RunMainLoop()
        {
            Paused = false;

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

                GameFrame(dt);

                TimeSpan frameTimeRemaining = FrameTime.AddSeconds(TargetFrameInterval) - DateTime.Now;
                if (frameTimeRemaining > TimeSpan.Zero)
                    Thread.Sleep(frameTimeRemaining);
            }

            ShutDown();
        }

        /// <summary>
        /// Runs a single frame of the game
        /// </summary>
        /// <param name="dt">Frame duration to simulate, in seconds</param>
        protected virtual void GameFrame(double dt)
        {
            
        }
    }
}

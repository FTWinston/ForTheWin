using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FTW.Engine.Server
{
    public class GameServer
    {
        internal static GameServer Instance { get; private set; }

        protected GameServer(bool isDedicated, int port, int maxClients, string name)
        {
            if (Instance != null)
                Console.Error.WriteLine("GameServer.Create called, but Instance is not null: server is already running!");

            Instance = this;
            
            IsDedicated = isDedicated;
            NetworkPort = port;
            MaxClients = maxClients;
            Name = name;
        }

        private Thread gameThread;

        public bool IsDedicated { get; private set; }
        public int MaxClients { get; set; }
        public int NetworkPort { get; private set; }
        public string Name { get; private set; }
        public bool IsMultiplayer { get { return MaxClients != 1; } }

        public bool IsRunning { get; private set; }
        public bool Paused { get; private set; }

        public void Start()
        {
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

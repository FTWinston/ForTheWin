using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FTW.Engine.Shared;
using System.IO;
using System.Reflection;
using Game.Server;

namespace DedicatedCLI
{
    class Program
    {
        const string settingsFilename = "settings.yml";
        const int defaultPort = 24680, defaultMaxClients = 8;
        const string defaultServerName = "Some FTW server";

        static void Main(string[] args)
        {
            if (!File.Exists(settingsFilename))
            {
                Stream defaultSettings = Assembly.GetExecutingAssembly().GetManifestResourceStream("DedicatedCLI." + settingsFilename);
                byte[] buf = new byte[defaultSettings.Length];
                defaultSettings.Read(buf, 0, buf.Length);
                File.WriteAllBytes(settingsFilename, buf);
            }

            Config settings = Config.ReadFile(settingsFilename);
            string strPort = settings.FindValueOrDefault("port", defaultPort.ToString());
            string strMaxClients = settings.FindValueOrDefault("max-clients", defaultPort.ToString());
            string serverName = settings.FindValueOrDefault("name", defaultServerName);

            int port, maxClients;
            if (!int.TryParse(strPort, out port))
                port = defaultPort;
            if (!int.TryParse(strMaxClients, out maxClients))
                maxClients = defaultMaxClients;

            GameServer game = new GameServer(true, port, maxClients, serverName);

            game.Start();
            while (game.IsRunning)
            {
                /*game.ServerCommand(*/Console.ReadLine()/*)*/;
            }

            // this was needed with the old networking system. See if we can do without it now.
            Thread.Sleep(100);
        }
    }
}

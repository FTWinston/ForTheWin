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

        static Config GetOrCreateConfig(out int port, out int maxClients, out string serverName)
        {
            if (!File.Exists(settingsFilename))
            {
                Assembly a = Assembly.GetExecutingAssembly();
                Stream defaultSettings = a.GetManifestResourceStream(typeof(Program), settingsFilename);
                byte[] buf = new byte[defaultSettings.Length];
                defaultSettings.Read(buf, 0, buf.Length);
                File.WriteAllBytes(settingsFilename, buf);
            }

            Config settings = Config.ReadFile(settingsFilename);
            string strPort = settings.FindValueOrDefault("port", defaultPort.ToString());
            string strMaxClients = settings.FindValueOrDefault("max-clients", defaultPort.ToString());
            serverName = settings.FindValueOrDefault("name", defaultServerName);

            if (!int.TryParse(strPort, out port))
                port = defaultPort;
            if (!int.TryParse(strMaxClients, out maxClients))
                maxClients = defaultMaxClients;
            
            return settings;
        }

        static void Main(string[] args)
        {
            int port, maxClients;
            string serverName;
            GetOrCreateConfig(out port, out maxClients, out serverName);
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

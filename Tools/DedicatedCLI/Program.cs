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
        
        static void Main(string[] args)
        {
            GameServer game = new GameServer(true, true);

            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = game.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            game.Start(config);
            while (game.IsRunning)
            {
                /*game.ServerCommand(*/Console.ReadLine()/*)*/;
            }

            // this was needed with the old networking system. See if we can do without it now.
            Thread.Sleep(100);
        }
    }
}

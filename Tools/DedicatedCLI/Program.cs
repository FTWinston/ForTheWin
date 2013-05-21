using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FTW.Engine.Shared;
using System.IO;
using System.Reflection;

namespace DedicatedCLI
{
    class Program
    {
        const string settingsFilename = "settings.yml";
        
        static void Main(string[] args)
        {
            ServerBase server = ServerBase.CreateReflection();
            
            Config config = Config.ReadFile(settingsFilename);
            if (config == null)
            {
                config = server.CreateDefaultConfig();
                config.SaveToFile(settingsFilename);
            }

            server.Start(true, config);
            while (server.IsRunning)
            {
                /*game.ServerCommand(*/Console.ReadLine()/*)*/;
            }

            // this was needed with the old networking system. See if we can do without it now.
            Thread.Sleep(100);
        }
    }
}

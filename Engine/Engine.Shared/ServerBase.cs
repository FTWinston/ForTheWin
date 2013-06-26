using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace FTW.Engine.Shared
{
    public abstract class ServerBase
    {
        public abstract Config CreateDefaultConfig();
        
        public abstract string Name { get; protected set; }

        public abstract void Start(bool isDedicated, Config config);
        public abstract void Stop();

        public abstract void Pause();
        public abstract void Resume();

        public abstract bool IsRunning { get; }
        public abstract bool Paused { get; }

        public abstract void HandleCommand(string cmd);

        private const string assemblyFileName = "Game.Server.dll";
        public static ServerBase CreateReflection()
        {
            if ( !File.Exists(assemblyFileName) )
                throw new Exception("Cannot find game server library: " + assemblyFileName);

            Assembly a = Assembly.LoadFrom(assemblyFileName);
            if (a == null)
                throw new Exception("Cannot load game server library: " + assemblyFileName);

            Type serverType = typeof(ServerBase);
            foreach ( Type t in a.GetTypes() )
                if (t.IsSubclassOf(serverType) && t.GetConstructor(Type.EmptyTypes) != null && !t.IsAbstract)
                    return Activator.CreateInstance(t) as ServerBase;

            throw new Exception("Cannot find ServerBase-implementing class in " + assemblyFileName + ". " + Environment.NewLine + "Check it is public, and has a public, parameterless constructor");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using FTW.Engine.Server;

namespace Game.Server
{
    public class GameServer : FTW.Engine.Server.GameServer
    {
        public GameServer()
        {
            ;
        }

        protected override bool MessageReceived(Client c, Message m)
        {
            if (base.MessageReceived(c, m))
                return true;

            // ...
            return false;
        }

        protected override bool ConsoleCommand(string[] words)
        {
            if (base.ConsoleCommand(words))
                return true;

            // ...
            return false;
        }
    }
}

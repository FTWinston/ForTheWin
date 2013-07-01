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

        protected override void SetupVariableDefaults()
        {
            // ...
        }

        protected override bool MessageReceived(Client c, Message m)
        {
            if (base.MessageReceived(c, m))
                return true;

            // ...
            return false;
        }

        protected override bool ConsoleCommand(string firstWord, string theRest)
        {
            if (base.ConsoleCommand(firstWord, theRest))
                return true;

            if (firstWord == "test")
            {
                new TestObject();
                return true;
            }
            // ...
            return false;
        }
    }
}

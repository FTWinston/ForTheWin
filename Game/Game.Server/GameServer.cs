using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Server
{
    public class GameServer : FTW.Engine.Server.GameServer
    {
        public GameServer(bool isDedicated, int port, int maxClients, string name)
            : base(isDedicated, port, maxClients, name)
        {

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace Game.Server
{
    public class GameServer : FTW.Engine.Server.GameServer
    {
        public GameServer(bool isMultiplayer, bool isDedicated)
            : base(isDedicated, isMultiplayer)
        {

        }
    }
}

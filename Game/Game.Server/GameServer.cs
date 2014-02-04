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

        protected override bool Initialize()
        {
            bool retVal = base.Initialize();
            
            var test = new Obstacle();
            test.positionX.Value = 50;
            test.positionY.Value = 50;

            test = new Obstacle();
            test.positionX.Value = 300;
            test.positionY.Value = 300;
            
            return retVal;
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
                new Obstacle();
                return true;
            }
            // ...
            return false;
        }

        List<Player> playerObjects = new List<Player>();

        protected override void ClientConnected(Client c)
        {
            base.ClientConnected(c);
            playerObjects.Add(new Player() { Client = c });
        }

        protected override void ClientDisconnected(Client c, bool manualDisconnect)
        {
            base.ClientDisconnected(c, manualDisconnect);
            for ( int i=0; i<playerObjects.Count; i++ )
                if (playerObjects[i].Client == c)
                {
                    playerObjects.RemoveAt(i);
                    i--;
                }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using FTW.Engine.Server;
using Game.Shared;

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

            if (m.Type == (byte)GameMessage.Movement)
            {
                Player player = playerObjects[c.Name];

                Keys pressed = (Keys)m.ReadByte();

                if ((pressed & Keys.Up) == Keys.Up)
                    player.sy = -Player.speed;
                else if ((pressed & Keys.Down) == Keys.Down)
                    player.sy = Player.speed;
                else
                    player.sy = 0;

                if ((pressed & Keys.Left) == Keys.Left)
                    player.sx = -Player.speed;
                else if ((pressed & Keys.Right) == Keys.Right)
                    player.sx = Player.speed;
                else
                    player.sx = 0;

                return true;
            }

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

        // sorting by name is dangerous. Can't we get a unique ID?
        SortedList<string, Player> playerObjects = new SortedList<string, Player>();

        protected override void ClientConnected(Client c)
        {
            base.ClientConnected(c);
            playerObjects.Add(c.Name, new Player() { Client = c });
        }

        protected override void ClientDisconnected(Client c, bool manualDisconnect)
        {
            base.ClientDisconnected(c, manualDisconnect);
            playerObjects.Remove(c.Name);
        }
    }
}

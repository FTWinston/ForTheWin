using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace Game.Shared
{
    public enum GameMessage : byte
    {
        //Dunno = EngineMessage.FirstGameMessageID
    }

    [Flags]
    public enum Keys
    {
        None = 0,
        Up = 1 << 0,
        Down = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
    }
}

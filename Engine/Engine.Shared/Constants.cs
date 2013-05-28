using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Shared
{
    public enum EngineMessage : byte
    {
        ClientConnecting = DefaultMessageIDTypes.ID_USER_PACKET_ENUM,
        ClientConnected,
        PlayerList,
        ClientNameChange,
        //ClientCommand,
        //Chat,

        FirstGameMessageID
    }
}

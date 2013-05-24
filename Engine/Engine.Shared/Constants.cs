using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Shared
{
    public enum EngineMessage : byte
    {
        NewClientInfo = DefaultMessageIDTypes.ID_USER_PACKET_ENUM + 1,
        ClientConnected,
        ClientNameChange,
        //ClientCommand,
        //Chat,

        FirstGameMessageID
    }
}

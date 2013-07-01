using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RakNet;

namespace FTW.Engine.Shared
{
    public static class Constants
    {
        public static Encoding NetworkTableStringEncoding = Encoding.ASCII;
    }

    public enum EngineMessage : byte
    {
        ClientConnecting = DefaultMessageIDTypes.ID_USER_PACKET_ENUM,
        ClientConnected,
        ServerInfo,
        ClientNameChange,
        Snapshot,
        //ClientCommand,
        //Chat,

        FirstGameMessageID
    }

    public enum EntitySnapshotType : byte
    {
        Full,
        Partial,
        Delete,
        Replace,
    }

    [Flags]
    public enum VariableFlags : byte
    {
        None = 0,
        Server = 1,     // exists on the server, sent to clients
        ServerOnly = 2, // exists on the server, not sent to clients
        Client = 4,     // exist on the client, sent to the server
        ClientOnly = 8, // exists on the client, not sent to the server
        DebugOnly = 16, // fixed value, unless in debug mode
        Cheat = 32,     // fixed value, unless cheats are enabled
    }
}

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
}

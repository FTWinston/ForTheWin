using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace FTW.Engine.Shared
{
    public static class Constants
    {
        public static Encoding NetworkTableStringEncoding = Encoding.ASCII;
    }

    public enum EngineMessage : byte
    {
        Snapshot = 0,
        ClientUpdate,
        InitialData,
        VariableChange,
        ClientConnected,
        ClientNameChange,
        
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

    public enum MessageReliabililty
    {
        Unreliable = NetDeliveryMethod.Unreliable,
        ReliableSkipOld = NetDeliveryMethod.ReliableSequenced,
        Reliable = NetDeliveryMethod.ReliableOrdered,
    }

    public enum SequenceChannel
    {
        System = 0,
        Chat = 1,
        Variables = 2,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    class Snapshot
    {
        public static void Read(InboundMessage m)
        {
            throw new NotImplementedException();

            /*
            // As we're only using timestamps, how should we determine (when trying to apply it) if we're MISSING a snapshot, or not?
            // Rather than have a frame number in the snapshot, we can compare the time difference to the server frame interval variable.
            // That would struggle when the variable changes ... or would it? If the variable change wasn't applied until the relevant snapshot came in, we might get away with it.

            Snapshot s = new Snapshot(m.ReadUInt());

            while (m.HasMoreData())
            {
                ushort entityID = m.ReadUShort();
                EntitySnapshotType snapshotType = (EntitySnapshotType)m.ReadByte();
                NetworkedEntity ent;
                switch (snapshotType)
                {
                    case EntitySnapshotType.Full:
                        string type = m.ReadString();
                        if ( !NetworkedEntity.NetworkedEntities.TryGetValue(entityID, out ent) )
                            s.ScheduleCreation(entityID, type, m, s.Tick);
                        else if (ent.NetworkedType != type)
                        {// delete and create new (this was an error, so also log it)
                            s.Deletions.Add(entityID);
                            s.ScheduleCreation(entityID, type, m, s.Tick);
                            Console.Error.WriteLine("Error reading snapshot: entity {0} has the wrong type (got {1}, update has {2})", entityID, ent.NetworkedType, type);
                            return;
                        }
                        else
                            ent.ReadSnapshot(m, s.Tick, false);
                        break;
                    case EntitySnapshotType.Partial:
                        if (NetworkedEntity.NetworkedEntities.TryGetValue(entityID, out ent))
                            ent.ReadSnapshot(m, s.Tick, true);
                        else
                        {
                            Console.Error.WriteLine("Error reading snapshot: received an update for unknown entity " + entityID);
                            return;
                        }
                        break;
                    case EntitySnapshotType.Delete:
                        s.Deletions.Add(entityID);
                        break;
                    case EntitySnapshotType.Replace:
                        // delete then treat like a full update
                        s.Deletions.Add(entityID);
                        s.ScheduleCreation(entityID, m.ReadString(), m, s.Tick);
                        break;
                    default:
                        Console.Error.WriteLine("Error reading snapshot: invalid EntitySnapshotType: " + snapshotType);
                        return;
                }
            }

            if (s.Tick > GameClient.Instance.LatestSnapshotTick)
                GameClient.Instance.LatestSnapshotTick = s.Tick;

            if (s.Creations.Count != 0 || s.Deletions.Count != 0)
                Enqueue(s);
            */
        }
        /*
        private static void Enqueue(Snapshot s)
        {
            if (s.Tick >= GameClient.Instance.CurrentTick)
                Queue[s.Tick] = s;
        }

        private static SortedList<uint, Snapshot> Queue = new SortedList<uint, Snapshot>();
        */
        internal static void CheckQueue()
        {
            throw new NotImplementedException();
            /*
            while ( Queue.Count > 0 )
            {
                uint tick = Queue.Keys[0];

                if (tick > GameClient.Instance.CurrentTick)
                    break;
                else
                {
                    Queue[tick].Apply();
                    Queue.RemoveAt(0);
                }
            }
            */
        }
        /*
        private void Apply()
        {
            foreach (var id in Deletions)
            {
                NetworkedEntity ent = NetworkedEntity.NetworkedEntities[id];
                if (ent != null)
                    ent.Delete();
            }
            foreach (var ent in Creations)
                ent.Initialize();
        }

        private Snapshot(uint tick)
        {
            Tick = tick;
        }

        public uint Tick { get; private set; }

        private List<NetworkedEntity> Creations = new List<NetworkedEntity>();
        private List<ushort> Deletions = new List<ushort>();

        private void ScheduleCreation(ushort entityID, string type, InboundMessage m, uint tick)
        {
            NetworkedEntity ent = NetworkedEntity.Create(type, entityID);
            if (ent == null)
            {
                Console.Error.WriteLine("Snapshot tried to create unrecognised entity type: " + type);
                return;
            }
            ent.ReadSnapshot(m, tick, false);
            Creations.Add(ent);
        }
        */
    }
}

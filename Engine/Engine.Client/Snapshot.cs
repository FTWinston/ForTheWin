using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;

namespace FTW.Engine.Client
{
    class Snapshot
    {
        public static void Read(Message m)
        {
            // As we're only using timestamps, how should we determine (when trying to apply it) if we're MISSING a snapshot, or not?
            // Rather than have a frame number in the snapshot, we can compare the time difference to the server frame interval variable.
            // That would struggle when the variable changes ... or would it? If the variable change wasn't applied until the relevant snapshot came in, we might get away with it.

            Snapshot s = new Snapshot();

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
                            s.ScheduleCreation(entityID, type, m);
                        else if (ent.NetworkedType != type)
                        {// delete and create new (this was an error, so also log it)
                            s.Deletions.Add(entityID);
                            s.ScheduleCreation(entityID, type, m);
                            Console.Error.WriteLine("Error reading snapshot: entity {0} has the wrong type (got {1}, update has {2})", entityID, ent.NetworkedType, type);
                        }
                        else
                            ent.ReadSnapshot(m, false);
                        break;
                    case EntitySnapshotType.Partial:
                        if (NetworkedEntity.NetworkedEntities.TryGetValue(entityID, out ent))
                            ent.ReadSnapshot(m, true);
                        else
                            Console.Error.WriteLine("Error reading snapshot: received an update for unknown entity " + entityID);
                        break;
                    case EntitySnapshotType.Delete:
                        s.Deletions.Add(entityID);
                        break;
                    case EntitySnapshotType.Replace:
                        // delete then treat like a full update
                        s.Deletions.Add(entityID);
                        s.ScheduleCreation(entityID, m.ReadString(), m);
                        break;
                    default:
                        Console.Error.WriteLine("Error reading snapshot: invalid EntitySnapshotType: " + snapshotType);
                        return;
                }
            }

            if (s.Creations.Count != 0 || s.Deletions.Count != 0)
                Enqueue(s, m.Timestamp.Value);
        }

        private static void Enqueue(Snapshot s, uint timestamp)
        {
            if (timestamp < GameClient.Instance.FrameTime - GameClient.Instance.LerpDelay)
                ;//s.Apply(); // well this was too late. I'm sure we'll do SOMETHING with it, however
            else
                Queue[timestamp] = s;
        }

        private static SortedList<uint, Snapshot> Queue = new SortedList<uint, Snapshot>();

        internal static void CheckQueue()
        {
            for (int i = 0; i < Queue.Count; i++)
            {
                uint time = Queue.Keys[i];

                if (time > GameClient.Instance.FrameTime - GameClient.Instance.LerpDelay)
                    break;
                else
                {
                    Queue[time].Apply();
                    Queue.RemoveAt(0);
                }
            }
        }

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

        private Snapshot()
        {

        }

        private List<NetworkedEntity> Creations = new List<NetworkedEntity>();
        private List<ushort> Deletions = new List<ushort>();

        private void ScheduleCreation(ushort entityID, string type, Message m)
        {
            NetworkedEntity ent = NetworkedEntity.Create(type, entityID);
            if (ent == null)
            {
                Console.Error.WriteLine("Snapshot tried to create unrecognised entity type: " + type);
                return;
            }
            ent.ReadSnapshot(m, false);
            Creations.Add(ent);
        }
    }
}

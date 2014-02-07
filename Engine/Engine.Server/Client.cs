using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using Lidgren.Network;

namespace FTW.Engine.Server
{
    public abstract class Client
    {
        protected Client(long id)
        {
            NeedsFullUpdate = true;
            SnapshotInterval = 50; // this should be a Variable
            AllClients.Add(id, this);
        }

        public string Name
        {
            get { return GetVariable("name"); }
            set { SetVariable("name", GetUniqueName(value)); }
        }
        public long ID { get; private set; }
        internal uint LastSnapshotTime { get; set; }
        internal uint NextSnapshotTime { get; set; }
        internal uint SnapshotInterval { get; set; }
        internal bool NeedsFullUpdate { get; set; }
        internal bool FullyConnected { get; set; }
        private SortedList<ushort, bool> KnownEntities = new SortedList<ushort, bool>();
        internal SortedList<ushort, bool> DeletedEntities = new SortedList<ushort, bool>();

        public abstract bool IsLocal { get; }

        internal static SortedList<long, Client> AllClients = new SortedList<long, Client>();
        public IList<Client> GetAll() { return AllClients.Values; }

        public static List<Client> GetAllExcept(params Client[] exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                bool excluded = false;
                foreach (Client ex in exclude)
                    if (c == ex)
                    {
                        excluded = true;
                        break;
                    }
                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExcept(IEnumerator<Client> exclude)
        {
            List<Client> list = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                exclude.Reset();
                bool excluded = false;
                do
                {
                    if (c == exclude.Current)
                    {
                        excluded = true;
                        break;
                    }
                } while (exclude.MoveNext());

                if (!excluded)
                    list.Add(c);
            }
            return list;
        }

        public static List<Client> GetAllExceptLocal()
        {
            List<Client> cl = new List<Client>();
            foreach (Client c in AllClients.Values)
            {
                if (c.IsLocal)
                    continue;
                cl.Add(c);
            }
            return cl;
        }

        public static Client LocalClient { get; internal set; }

        public static Client GetByName(string name)
        {
            foreach (Client c in AllClients.Values)
                if (c.Name == name)
                    return c;
            return null;
        }

        internal static Client GetByID(long id)
        {
            Client c;
            AllClients.TryGetValue(id, out c);
            return c;
        }

        internal string GetUniqueName(string desiredName)
        {
            desiredName = GameServer.Instance.ValidatePlayerName(desiredName);

            string newName = desiredName;
            int i = 1;

            Client matchingName = Client.GetByName(newName);
            while (matchingName != null && matchingName != this)
            {
                newName = string.Format("{0} ({1})", desiredName, i);
                i++;

                matchingName = Client.GetByName(newName);
            }
            return newName;
        }

        public abstract void Send(OutboundMessage m);

        public static void SendToAll(OutboundMessage m)
        {
            GameServer.Instance.Networking.SendToAll(m);
            if (LocalClient != null)
                LocalClient.Send(m);
        }

        public static void SendToAllExcept(OutboundMessage m, Client c)
        {
            NetConnection exclude = c.IsLocal ? null : (c as RemoteClient).Connection;
            GameServer.Instance.Networking.SendToAllExcept(m, exclude);

            if (c != LocalClient && LocalClient != null)
                LocalClient.Send(m);
        }

        internal static void SendSnapshots()
        {
            foreach (Client c in AllClients.Values)
            {
                if (c.NextSnapshotTime > GameServer.Instance.FrameTime)
                    continue;

                c.SendSnapshot();
                c.LastSnapshotTime = GameServer.Instance.FrameTime;
                c.NextSnapshotTime += c.SnapshotInterval;
            }
        }

        private void SendSnapshot()
        {
            OutboundMessage m = OutboundMessage.CreateUnreliable((byte)EngineMessage.Snapshot);
            m.Write(GameServer.Instance.FrameTime);

            // now step through all entities, decide what to send. Will either be:
            // No change (sends nothing)
            // New entity / full update of entity (Full)
            // Partial update of entity (Partial)
            // Forget about this entity (Delete)
            // ID reassigned, full update of new entity (Replace)
            foreach (Entity e in Entity.AllEntities)
            {
                if (!e.IsNetworked)
                    continue;
                
                NetworkedEntity ne = e as NetworkedEntity;
                if (ne.IsDeleted || !ne.HasChanges(this))
                    continue;

                if (!ne.ShouldSendToClient(this))
                {
                    if (KnownEntities.ContainsKey(ne.EntityID))
                        DeletedEntities.Add(ne.EntityID, false); // the client shouldn't see this entity any more, so delete it on their end
                    continue;
                }

                m.Write(ne.EntityID);

                EntitySnapshotType updateType;
                if (KnownEntities.ContainsKey(ne.EntityID))
                {
                    if (DeletedEntities.ContainsKey(ne.EntityID))
                    {
                        // Reusing an entity ID the client is still using for something else. Ensure it knows the old should be deleted.
                        // Also, don't include it on the Delete list - the Replace accounts for that.
                        updateType = EntitySnapshotType.Replace;
                        DeletedEntities.Remove(ne.EntityID);
                    }
                    else
                        updateType = NeedsFullUpdate ? EntitySnapshotType.Full : EntitySnapshotType.Partial;
                }
                else
                {
                    updateType = EntitySnapshotType.Full;
                    KnownEntities[ne.EntityID] = true;
                }

                m.Write((byte)updateType);
                ne.WriteSnapshot(m, this, updateType == EntitySnapshotType.Partial);
            }

            foreach (ushort entityID in DeletedEntities.Keys)
            {
                m.Write(entityID);
                m.Write((byte)EntitySnapshotType.Delete);
                KnownEntities.Remove(entityID);
            }
            DeletedEntities.Clear();

            Send(m);
            NeedsFullUpdate = false;
        }

        private SortedList<string, string> variables = new SortedList<string, string>();
        private SortedList<string, float> numericVariables = new SortedList<string, float>();
        internal void SetVariable(string name, string val)
        {
            if (name == "name")
                val = GetUniqueName(val);
            else if (name == "cl_snapshotrate")
            {
                float f;
                if (float.TryParse(val, out f) && f != 0f)
                    SnapshotInterval = (uint)(1000f / f);
            }

            variables[name] = val;
            float num;
            if (float.TryParse(val, out num))
                numericVariables[name] = num;
            else
                numericVariables.Remove(name);
        }

        internal void SetVariable(string name, float val)
        {
            variables[name] = val.ToString();
            numericVariables[name] = val;
        }

        public string GetVariable(string name)
        {
            string val;
            if (variables.TryGetValue(name, out val))
                return val;
            return null;
        }

        public float? GetNumericVariable(string name)
        {
            float val;
            if (numericVariables.TryGetValue(name, out val))
                return val;
            return null;
        }
    }

    internal class LocalClient : Client
    {
        private LocalClient() : base(long.MaxValue) { }
        public override bool IsLocal { get { return true; } }

        public static Client Create(string desiredName)
        {
            LocalClient c = new LocalClient();
            c.Name = desiredName;

            LocalClient = c;
            return c;
        }

        public override void Send(OutboundMessage m)
        {
            lock (Message.ToLocalClient)
                Message.ToLocalClient.Add(m);
        }
    }

    internal class RemoteClient : Client
    {
        private RemoteClient(long id) : base(id) { }
        public override bool IsLocal { get { return false; } }
        internal NetConnection Connection { get; private set; }

        public static Client Create(NetConnection connection)
        {
            RemoteClient c = new RemoteClient(connection.RemoteUniqueIdentifier);
            c.Connection = connection;
            c.Name = "unknown";

            return c;
        }

        public override void Send(OutboundMessage m)
        {
            GameServer.Instance.Networking.Send(m, Connection);
        }
    }
}

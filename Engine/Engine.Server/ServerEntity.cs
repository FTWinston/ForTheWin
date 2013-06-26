using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Collections.ObjectModel;

namespace FTW.Engine.Server
{
    public abstract class Entity : BaseEntity
    {
        protected Entity()
        {
            AllEntities.Add(this);
        }

        /// <summary>
        /// Schedules this entity for deletion at the end of the current frame.
        /// Until then, its IsDeleted property will be true.
        /// </summary>
        public virtual void Delete()
        {
            IsDeleted = true;
            AllEntities.Remove(this);
        }

        public virtual void PreThink(double dt) { }
        public virtual void Simulate(double dt) { }
        public virtual void PostThink(double dt) { }

        internal static List<Entity> AllEntities = new List<Entity>();
        internal static SortedList<ushort, NetworkedEntity> NetworkedEntities = new SortedList<ushort, NetworkedEntity>();
        public static ReadOnlyCollection<Entity> GetAll() { return AllEntities.AsReadOnly(); }
    }

    public abstract class NetworkedEntity : Entity
    {
        public NetworkedEntity(string networkedType, bool usesRelatedClient)
            : this(networkedType, usesRelatedClient, null) { }

        public NetworkedEntity(string networkedType, Client relatedClient)
            : this(networkedType, true, relatedClient) { }

        private NetworkedEntity(string networkedType, bool usesRelatedClient, Client relatedClient)
            : base()
        {
            NetworkedType = networkedType;
            EntityID = GetNewEntityID();

            NetworkedEntities.Add(EntityID, this);
            
            Fields = new List<NetworkField>();
            UsesRelatedClient = usesRelatedClient;
            RelatedClient = relatedClient;

            if (UsesRelatedClient)
            {
                RelatedClientFields = new List<NetworkField>();
                OtherClientFields = new List<NetworkField>();
            }
        }

        public override void Delete()
        {
            base.Delete();

            NetworkedEntities.Remove(EntityID);
            
            foreach (Client c in Client.AllClients.Values)
                if ( ShouldSendToClient(c) )
                    c.DeletedEntities.Add(EntityID, true);
        }

        ushort nextEntityID = ushort.MinValue;
        private ushort GetNewEntityID()
        {
            while (NetworkedEntities.ContainsKey(nextEntityID))
            {
                if (nextEntityID == ushort.MaxValue)
                    nextEntityID = ushort.MinValue;
                else
                    nextEntityID++;
            }
            return nextEntityID++;
        }

        internal string NetworkedType { get; private set; }
        public ushort EntityID { get; private set; }
        internal bool UsesRelatedClient { get; private set; }
        public Client RelatedClient { get; private set; }
        private List<NetworkField> Fields, RelatedClientFields, OtherClientFields;
        internal uint LastChanged, LastChangedRelated, LastChangedOther;

        protected internal virtual bool ShouldSendToClient(Client c) { return true; }
        
        internal bool HasChanges(Client c)
        {
            if (LastChanged >= c.LastSnapshotTime)
                return true;

            if (UsesRelatedClient)
            {
                uint field = RelatedClient == c ? LastChangedRelated : LastChangedOther;
                return field >= c.LastSnapshotTime;
            }

            return false;
        }

        internal void WriteSnapshot(Message m, Client c, bool incremental)
        {
            if (incremental)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    var f = Fields[i];
                    if (f.LastChanged > c.LastSnapshotTime)
                    {
                        m.Write((byte)i);
                        f.WriteTo(m);
                    }
                }

                if (UsesRelatedClient)
                {
                    int offset = Fields.Count;
                    var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var f = list[i];
                        if (f.LastChanged > c.LastSnapshotTime)
                        {
                            m.Write((byte)(i + offset));
                            f.WriteTo(m);
                        }
                    }
                }
                m.Write(byte.MaxValue);
            }
            else
            {
                m.Write(NetworkedType);

                for (int i = 0; i < Fields.Count; i++)
                    Fields[i].WriteTo(m);

                if (!UsesRelatedClient)
                    return;

                m.Write(RelatedClient == c);
                var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;

                for (int i = 0; i < list.Count; i++)
                    list[i].WriteTo(m);
            }
        }
    }

    public abstract class ServerOnlyEntity : Entity
    {
        public sealed override bool IsNetworked { get { return false; } }
    }
}

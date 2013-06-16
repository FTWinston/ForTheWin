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
        }


        public virtual void PreThink(double dt) { }
        public virtual void Simulate(double dt) { }
        public virtual void PostThink(double dt) { }

        internal static List<Entity> AllEntities = new List<Entity>();
        public static ReadOnlyCollection<Entity> GetAll() { return AllEntities.AsReadOnly(); }
    }

    public abstract class NetworkedEntity : Entity
    {
        public NetworkedEntity()
            : this(null)
        {
        }

        public NetworkedEntity(Client relatedClient)
        {
            //EntityID = GetNewEntityID();

            Fields = new List<NetworkField>();
            RelatedClient = relatedClient;

            if (RelatedClient != null)
            {
                RelatedClientFields = new List<NetworkField>();
                OtherClientFields = new List<NetworkField>();
            }
        }

        public override void Delete()
        {
            base.Delete();
            
            foreach (Client c in Client.AllClients.Values)
                if ( ShouldSendToClient(c) )
                    c.DeletedEntities.Add(EntityID, true);
        }

        public ushort EntityID { get; private set; }
        public Client RelatedClient { get; private set; }
        private List<NetworkField> Fields, RelatedClientFields, OtherClientFields;
        internal uint LastChanged, LastChangedRelated, LastChangedOther;

        protected internal virtual bool ShouldSendToClient(Client c) { return true; }
        
        internal bool HasChanges(Client c)
        {
            if (LastChanged >= c.LastSnapshotFrame)
                return true;

            if ( RelatedClient != null )
            {
                uint field = RelatedClient == c ? LastChangedRelated : LastChangedOther;
                return field >= c.LastSnapshotFrame;
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
                    if (f.LastChanged > c.LastSnapshotFrame)
                    {
                        m.Write((byte)i);
                        f.WriteTo(m);
                    }
                }

                if (RelatedClient == null)
                    return;

                int offset = Fields.Count;
                var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;
                for (int i = 0; i < list.Count; i++)
                {
                    var f = list[i];
                    if (f.LastChanged > c.LastSnapshotFrame)
                    {
                        m.Write((byte)(i + offset));
                        f.WriteTo(m);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Fields.Count; i++)
                    Fields[i].WriteTo(m);

                if (RelatedClient == null)
                    return;

                var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;
                for (int i = 0; i < list.Count; i++)
                    list[i].WriteTo(m);
            }
        }

        public abstract class ServerOnlyEntity : Entity
        {
            public sealed override bool IsNetworked { get { return false; } }
        }
    }
}

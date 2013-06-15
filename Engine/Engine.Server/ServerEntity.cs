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
        public void Delete()
        {
            IsDeleted = true;
            //if (IsNetworked)
                //Networking.RemoveNetworkedEntity(GetNetworked());
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

        public ushort EntityID { get; private set; }
        public Client RelatedClient { get; private set; }
        private List<NetworkField> Fields, RelatedClientFields, OtherClientFields;
        internal uint LastChanged, LastChangedRelated, LastChangedOther;

        protected internal virtual bool ShouldSendToClient(Client c) { return true; }
        
        internal bool HasChanges(Client c)
        {
            if (LastChanged > c.LastSnapshotFrame)
                return true;

            if ( RelatedClient != null )
            {
                uint field = RelatedClient == c ? LastChangedRelated : LastChangedOther;
                return field > c.LastSnapshotFrame;
            }

            return false;
        }

        internal void Write(Message m, Client c, bool incremental)
        {
            // these ought to be written by the calling method.
            // the "type" should indicate if this is an update, a new entity, a replacement, or a deletion (though this woudln't be called for deletions)
            //m.Write(EntityID);
            //m.Write(incremental);

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

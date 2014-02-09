using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FTW.Engine.Server
{
    public abstract partial class Entity
    {
        protected Entity()
        {
            IsDeleted = false;
            AllEntities.Add(this);
        }
    }

    public abstract partial class NetworkedEntity : Entity
    {
        public NetworkedEntity(string networkedType)
            : this(networkedType, null) { }

        public NetworkedEntity(string networkedType, Client relatedClient)
            : base()
        {
            NetworkedType = networkedType.Replace('¬', '-');
            EntityID = GetNewEntityID();

            NetworkedEntities.Add(EntityID, this);

            RelatedClient = relatedClient;
            DoFieldSetup();
        }

        internal static void InitializeTypes()
        {
            var Instances = new SortedList<string, NetworkedEntity>();

            foreach (Type t in GameServer.Instance.GetType().Assembly.GetTypes())
                if (t.IsSubclassOf(typeof(NetworkedEntity)) && !t.IsAbstract)
                {
                    ConstructorInfo c = t.GetConstructor(Type.EmptyTypes);
                    if (c != null)
                    {
                        NetworkedEntity ent = c.Invoke(Type.EmptyTypes) as NetworkedEntity;
                        if (Instances.ContainsKey(ent.NetworkedType))
                            Console.Error.WriteLine("Duplicate network type detected - {1} and {2} both use the same NetworkedType: {0}", ent.NetworkedType, c.DeclaringType.Name, Instances[ent.NetworkedType].GetType().Name);
                        else
                            Instances.Add(ent.NetworkedType, ent);
                    }
                }

            NetworkTableHash = GetNetworkTableHash(Instances.Values);
        }
        internal static byte[] NetworkTableHash;

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

        public Client RelatedClient { get; private set; }
        internal DateTime LastChanged, LastChangedRelated, LastChangedOther;

        protected internal virtual bool ShouldSendToClient(Client c) { return true; }
        
        internal bool HasChanges(Client c)
        {
            if (LastChanged >= c.LastSnapshotTime)
                return true;

            if (UsesRelatedClient)
            {
                DateTime field = RelatedClient == c ? LastChangedRelated : LastChangedOther;
                return field >= c.LastSnapshotTime;
            }

            return false;
        }

        internal void WriteSnapshot(OutboundMessage m, Client c, bool incremental)
        {
            if (incremental)
            {
                for (int i = 0; i < Fields.Length; i++)
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
                    int offset = Fields.Length;
                    var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;
                    for (int i = 0; i < list.Length; i++)
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

                for (int i = 0; i < Fields.Length; i++)
                    Fields[i].WriteTo(m);

                if (!UsesRelatedClient)
                    return;

                m.Write(RelatedClient == c);
                var list = RelatedClient == c ? RelatedClientFields : OtherClientFields;

                for (int i = 0; i < list.Length; i++)
                    list[i].WriteTo(m);
            }
        }
    }

    public abstract class ServerOnlyEntity : Entity
    {
        public sealed override bool IsNetworked { get { return false; } }
    }
}

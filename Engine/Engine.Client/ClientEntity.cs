using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FTW.Engine.Client
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
        internal static SortedList<string, ConstructorInfo> Types;

        internal static void InitializeTypes()
        {
            Types = new SortedList<string, ConstructorInfo>();

            foreach (Type t in Assembly.GetEntryAssembly().GetTypes())
                if (t.IsSubclassOf(typeof(NetworkedEntity)) && !t.IsAbstract)
                {
                    ConstructorInfo c = t.GetConstructor(Type.EmptyTypes);
                    if (c != null)
                    {
                        NetworkedEntity ent = c.Invoke(Type.EmptyTypes) as NetworkedEntity;
                        if (Types.ContainsKey(ent.NetworkedType))
                            Console.Error.WriteLine(string.Format("Duplicate network type detected - {1} and {2} both use the same NetworkedType: {0}", ent.NetworkedType, c.DeclaringType, Types[ent.NetworkedType].DeclaringType));
                        else
                            Types.Add(ent.NetworkedType, c);
                    }
                }
        }

        internal static NetworkedEntity Create(string type, ushort entityID)
        {
            ConstructorInfo c;
            if (!Types.TryGetValue(type, out c))
                return null;

            NetworkedEntity ent = c.Invoke(Type.EmptyTypes) as NetworkedEntity;
            ent.EntityID = entityID;
            return ent;
        }

        public NetworkedEntity(ushort entityID, string networkedType, bool usesRelatedClient, bool isRelatedClient)
            : base()
        {
            NetworkedType = networkedType;
            EntityID = entityID;
            UsesRelatedClient = usesRelatedClient;
            IsRelatedClient = isRelatedClient;

            NetworkedEntities.Add(EntityID, this);

            Fields = new List<NetworkField>();

            if (UsesRelatedClient)
                if (IsRelatedClient)
                    RelatedClientFields = new List<NetworkField>();
                else
                    OtherClientFields = new List<NetworkField>();
        }

        public override void Delete()
        {
            base.Delete();
            NetworkedEntities.Remove(EntityID);
        }

        internal void ReadSnapshot(Message m, bool incremental)
        {
            if (incremental)
            {
                var list = UsesRelatedClient ? IsRelatedClient ? RelatedClientFields : OtherClientFields : null;
                int b = m.ReadByte();
                int offset = Fields.Count, max = list == null ? offset : list.Count + offset;
                while (b != byte.MaxValue)
                {
                    if (b < offset)
                        Fields[b].PerformRead(m);
                    else if (b < max)
                        list[b-offset].PerformRead(m);
                    else
                        Console.Error.WriteLine("Error reading incremental update: invalid field ID specified: " + b);

                    b = m.ReadByte();
                }
            }
            else
            {
                for (int i = 0; i < Fields.Count; i++)
                    Fields[i].PerformRead(m);

                if (!UsesRelatedClient)
                    return;

                // related client stuff
                var list = IsRelatedClient ? RelatedClientFields : OtherClientFields;
                for (int i = 0; i < list.Count; i++)
                    list[i].PerformRead(m);
            }
        }

        internal string NetworkedType { get; private set; }
        public ushort EntityID { get; private set; }
        internal bool UsesRelatedClient { get; private set; }
        public bool IsRelatedClient { get; private set; }
        private List<NetworkField> Fields, RelatedClientFields, OtherClientFields;
    }
}

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

        public NetworkedEntity(ushort entityID, string networkedType, bool isRelatedClient)
            : base()
        {
            NetworkedType = networkedType;
            EntityID = entityID;
            IsRelatedClient = isRelatedClient;

            NetworkedEntities.Add(EntityID, this);

            Fields = new List<NetworkField>();

            // unless we send a bool to cover whether there IS a related client, we'll always have to create one of these, won't we?
            if (isRelatedClient)
                RelatedClientFields = new List<NetworkField>();
            else
                OtherClientFields = new List<NetworkField>();
        }

        public override void Delete()
        {
            base.Delete();
            NetworkedEntities.Remove(EntityID);
        }

        internal string NetworkedType { get; private set; }
        public ushort EntityID { get; private set; }
        public bool IsRelatedClient { get; private set; }
        private List<NetworkField> Fields, RelatedClientFields, OtherClientFields;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FTW.Engine.Client
{
    public abstract partial class Entity
    {
        protected Entity()
        {
            IsDeleted = false;
        }

        protected internal virtual void Initialize()
        {
            AllEntities.Add(this);
        }
    }

    public abstract partial class NetworkedEntity : Entity
    {
        internal static SortedList<string, ConstructorInfo> Constructors;

        internal static void InitializeTypes()
        {
            Constructors = new SortedList<string, ConstructorInfo>();
            var Instances = new SortedList<string, NetworkedEntity>();

            foreach (Type t in Assembly.GetEntryAssembly().GetTypes())
                if (t.IsSubclassOf(typeof(NetworkedEntity)) && !t.IsAbstract)
                {
                    ConstructorInfo c = t.GetConstructor(Type.EmptyTypes);
                    if (c != null)
                    {
                        NetworkedEntity ent = c.Invoke(Type.EmptyTypes) as NetworkedEntity;
                        if (Constructors.ContainsKey(ent.NetworkedType))
                            Console.Error.WriteLine("Duplicate network type detected - {1} and {2} both use the same NetworkedType: {0}", ent.NetworkedType, c.DeclaringType.Name, Constructors[ent.NetworkedType].DeclaringType.Name);
                        else
                        {
                            Constructors.Add(ent.NetworkedType, c);
                            Instances.Add(ent.NetworkedType, ent);
                        }
                    }
                }

            NetworkTableHash = GetNetworkTableHash(Instances.Values);
        }

        private static byte[] NetworkTableHash;

        internal static bool CheckNetworkTableHash(byte[] serverHash)
        {
            if (NetworkTableHash.Length != serverHash.Length)
                return false;

            for (int i = 0; i < serverHash.Length; i++)
                if (NetworkTableHash[i] != serverHash[i])
                    return false;
#if DEBUG
            Console.WriteLine("Network tables match");
#endif
            return true;
        }

        internal static NetworkedEntity Create(string type, ushort entityID)
        {
            ConstructorInfo c;
            if (!Constructors.TryGetValue(type, out c))
                return null;

            NetworkedEntity ent = c.Invoke(Type.EmptyTypes) as NetworkedEntity;
            ent.EntityID = entityID;
            NetworkedEntities[entityID] = ent;
            return ent;
        }

        public NetworkedEntity(string networkedType, params NetworkField[] fields)
            : base()
        {
            NetworkedType = networkedType.Replace('¬', '-');
            DoFieldSetup();
        }

        protected internal override void Initialize()
        {
            base.Initialize();
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
                int offset = Fields.Length, max = list == null ? offset : list.Length + offset;
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
                for (int i = 0; i < Fields.Length; i++)
                    Fields[i].PerformRead(m);

                if (!UsesRelatedClient)
                    return;

                IsRelatedClient = m.ReadBool();
                
                var list = IsRelatedClient ? RelatedClientFields : OtherClientFields;
                for (int i = 0; i < list.Length; i++)
                    list[i].PerformRead(m);
            }
        }

        public bool IsRelatedClient { get; private set; }
    }

    public abstract class ClientOnlyEntity : Entity
    {
        protected ClientOnlyEntity()
        {
            Initialize();
        }

        public sealed override bool IsNetworked { get { return false; } }
    }
}

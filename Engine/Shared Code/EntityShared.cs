using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.Security.Cryptography;
using System.Collections.ObjectModel;

#if SERVER
namespace FTW.Engine.Server
#elif CLIENT
namespace FTW.Engine.Client
#endif
{
    public abstract partial class Entity
    {
        protected Entity()
        {
            IsDeleted = false;
            AllEntities.Add(this);
        }

        /// <summary>
        /// Indicates that an entity is scheduled for deletion at the end of the current frame.
        /// It should be treated as if it has already been deleted.
        /// </summary>
        public bool IsDeleted { get; protected set; }

        public abstract bool IsNetworked { get; }

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

    public abstract partial class NetworkedEntity
    {
        internal string NetworkedType { get; private set; }
        public ushort EntityID { get; private set; }
        internal bool UsesRelatedClient { get; private set; }

        public sealed override bool IsNetworked { get { return true; } }

        private NetworkField[] Fields, RelatedClientFields, OtherClientFields;

        protected abstract NetworkField[] SetupFields();
        protected virtual void SetupRelatedClientFields(out NetworkField[] relatedClientFields, out NetworkField[] otherClientFields)
        {
            relatedClientFields = null; otherClientFields = null;
        }

        private void DoFieldSetup()
        {
            Fields = SetupFields();

            NetworkField[] related, other;
            SetupRelatedClientFields(out related, out other);
            if (related != null)
            {
                UsesRelatedClient = true;
                RelatedClientFields = related;
                OtherClientFields = other;
            }
            else
                UsesRelatedClient = false;
        }

        private static byte[] GetNetworkTableHash(IList<NetworkedEntity> types)
        {
            bool first;
            const char separator = '¬';
            StringBuilder sb = new StringBuilder();
            foreach (var ent in types)
            {
                sb.Append(separator);
                sb.Append(separator);
                sb.Append(separator);

                sb.Append(ent.NetworkedType);

                sb.Append(separator);
                sb.Append(separator);

                first = true;
                foreach (NetworkField field in ent.Fields)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append(separator);

                    sb.Append(field.Describe());
                }

                if (ent.UsesRelatedClient)
                {
                    sb.Append(separator);
                    sb.Append(separator);

                    first = true;
                    foreach (NetworkField field in ent.RelatedClientFields)
                    {
                        if (first)
                            first = false;
                        else
                            sb.Append(separator);

                        sb.Append(field.Describe());
                    }

                    sb.Append(separator);
                    sb.Append(separator);

                    first = true;
                    foreach (NetworkField field in ent.OtherClientFields)
                    {
                        if (first)
                            first = false;
                        else
                            sb.Append(separator);

                        sb.Append(field.Describe());
                    }
                }
            }

            string fullTable = sb.Length > 0 ? sb.ToString().Substring(3) : string.Empty;
#if DEBUG
#if SERVER
            Console.WriteLine("Server network table:");
#elif CLIENT
            Console.WriteLine("Client network table:");
#endif
            Console.WriteLine(fullTable.Replace(separator, '\n'));
#endif
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Constants.NetworkTableStringEncoding.GetBytes(fullTable));
            md5.Clear();

            AllEntities.Clear();
            NetworkedEntities.Clear();
            return hash;
        }
    }
}

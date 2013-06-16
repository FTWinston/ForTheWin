using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if SERVER
using FTW.Engine.Server;
#endif

namespace FTW.Engine.Shared
{
    public abstract class NetworkField
    {
        public NetworkField()
        {
        }

#if CLIENT
        public abstract void ReadFrom(Message m);
#elif SERVER
        public abstract void WriteTo(Message m);

        internal void SetEntity(NetworkedEntity e, bool related)
        {
            entity = e;
            relatedClient = related;
        }
        protected NetworkedEntity entity; protected bool relatedClient;

        public uint LastChanged { get; protected set; }
#endif
    }

    public abstract class NetworkField<T> : NetworkField
    {
        public NetworkField()
            : base()
        {
            val = default(T);
        }

        protected T val;
        public T Value
        {
            get { return val; }
#if SERVER
            set
            {
                val = value;
                LastChanged = GameServer.Instance.FrameTime;
                if (entity.RelatedClient == null)
                    entity.LastChanged = LastChanged;
                else if (relatedClient)
                    entity.LastChangedRelated = LastChanged;
                else
                    entity.LastChangedOther = LastChanged;
            }
#endif
        }

        public static implicit operator T(NetworkField<T> f) { return f.Value; }
    }

    public class NetworkInt : NetworkField<int>
    {
#if CLIENT
        public override void ReadFrom(Message m)
        {
            val = m.ReadInt();
        }
#elif SERVER
        public override void WriteTo(Message m)
        {
            m.Write(val);
        }
#endif
    }

    public class NetworkFloat : NetworkField<float>
    {
#if CLIENT
        public override void ReadFrom(Message m)
        {
            val = m.ReadFloat();
        }
#elif SERVER
        public override void WriteTo(Message m)
        {
            m.Write(val);
        }
#endif
    }

    public class NetworkString : NetworkField<string>
    {
#if CLIENT
        public override void ReadFrom(Message m)
        {
            val = m.ReadString();
        }
#elif SERVER
        public override void WriteTo(Message m)
        {
            m.Write(val);
        }
#endif
    }
}

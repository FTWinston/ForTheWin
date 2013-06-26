using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if SERVER
using FTW.Engine.Server;
#elif CLIENT
using FTW.Engine.Client;
#endif

namespace FTW.Engine.Shared
{
    public abstract class NetworkField
    {
        public NetworkField()
        {
        }

        protected internal abstract string Describe();

#if SERVER
        public abstract void WriteTo(Message m);

        internal void SetEntity(NetworkedEntity e, bool related)
        {
            entity = e;
            relatedClient = related;
        }
        protected NetworkedEntity entity; protected bool relatedClient;

        public uint LastChanged { get; protected set; }
#elif CLIENT
        public abstract void PerformRead(Message m);
#endif
    }

    public abstract class NetworkField<T> : NetworkField
    {
#if SERVER
        public NetworkField(bool interpolate)
            : base()
        {
            this.interpolate = interpolate;
            val = default(T);
        }

        protected T val;
        public T Value
        {
            get { return val; }
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
        }
#elif CLIENT
        public NetworkField(bool interpolate)
            : base()
        {
            this.interpolate = interpolate;
            fromVal = default(T); toVal = default(T);
            fromTime = 0; toTime = 1;
        }

        protected T fromVal, toVal;
        protected uint fromTime, toTime;
        protected SortedList<uint, T> queuedValues = new SortedList<uint, T>();
        public T Value
        {
            get
            {
                if (interpolate && toTime < GameClient.Instance.FrameTime)
                    return Lerp(fromVal, toVal, (GameClient.Instance.FrameTime - fromTime) / (toTime - fromTime));
                else // no extrapolation, for the moment
                    return toVal;
            }
        }

        public override void PerformRead(Message m)
        {
            T val = ReadFrom(m);

            uint messageTime = m.Timestamp.Value;
            if (messageTime < GameClient.Instance.FrameTime)
            {
                // can interpolate from this instead of our previous value. Liable to "jump" ... do we want?
                if (messageTime > fromTime)
                {
                    fromVal = val;
                    fromTime = messageTime;
                }
            }
            else if (messageTime < toTime)
            {
                // usurp the current "to" value, and put it back into the queue. Liable to "jump" ... do we want?
                queuedValues[toTime] = toVal;
                toVal = val;
                toTime = messageTime;
            }
            else
            {
                // put this into the queue, for use later
                queuedValues[messageTime] = val;
            }
        }

        public abstract T ReadFrom(Message m);

        protected abstract T Lerp(T val1, T val2, float fraction);
#endif

        private bool interpolate;
        protected internal override string Describe()
        {
            return string.Format("{0},{1}", typeof(T).Name, interpolate ? "Y" : "N");
        } 

        public static implicit operator T(NetworkField<T> f) { return f.Value; }
    }

    public class NetworkInt : NetworkField<int>
    {
        public NetworkInt(bool interpolate)
            : base(interpolate)
        {
        }

#if CLIENT
        public override int ReadFrom(Message m)
        {
            return m.ReadInt();
        }

        protected override int Lerp(int val1, int val2, float fraction)
        {
            return (int)(val2 * fraction + val1 * (1f - fraction));
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
        public NetworkFloat(bool interpolate)
            : base(interpolate)
        {
        }

#if CLIENT
        public override float ReadFrom(Message m)
        {
            return m.ReadFloat();
        }

        protected override float Lerp(float val1, float val2, float fraction)
        {
            return val2 * fraction + val1 * (1f - fraction);
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
        public NetworkString()
            : base(false)
        {
        }

#if CLIENT
        public override string ReadFrom(Message m)
        {
            return m.ReadString();
        }

        protected override string Lerp(string val1, string val2, float fraction) { return val1; } // don't interpolate strings
#elif SERVER
        public override void WriteTo(Message m)
        {
            m.Write(val);
        }
#endif
    }
}

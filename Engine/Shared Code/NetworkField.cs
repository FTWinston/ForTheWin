/*
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
        public abstract void WriteTo(OutboundMessage m);

        internal void SetEntity(NetworkedEntity e, bool? related)
        {
            entity = e;
            relatedClient = related;
        }
        protected NetworkedEntity entity; protected bool? relatedClient;

        public DateTime LastChanged { get; protected set; }
#elif CLIENT
        public abstract void PerformRead(InboundMessage m, uint tick);
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
                if (relatedClient.HasValue)
                    if ( relatedClient.Value )
                        entity.LastChangedRelated = LastChanged;
                    else
                        entity.LastChangedOther = LastChanged;
                else
                    entity.LastChanged = LastChanged;
            }
        }
#elif CLIENT
        public NetworkField(bool interpolate)
            : base()
        {
            this.interpolate = interpolate;
            fromVal = default(T); toVal = default(T);
            fromTick = 0; toTick = 1;
        }

        protected T fromVal, toVal;
        protected uint fromTick, toTick;
        protected SortedList<uint, T> queuedValues = new SortedList<uint, T>();
        public T Value
        {
            get
            {
                if (toTick < GameClient.Instance.CurrentTick)
                {
                    fromVal = toVal;
                    fromTick = toTick;

                    // is there a queued value to get?
                    while ( queuedValues.Count > 0 )
                    {
                        toTick = queuedValues.Keys[0];
                        toVal = queuedValues[toTick];
                        queuedValues.RemoveAt(0);

                        // ok we got this one value, but it's already out-of-date
                        if (toTick < GameClient.Instance.CurrentTick)
                        {
                            fromVal = toVal;
                            fromTick = toTick;
                        }
                        else
                            break;
                    }
                }

                if (interpolate && fromTick < toTick)
                    return Lerp(fromVal, toVal, (float)(GameClient.Instance.CurrentTick - fromTick) / (toTick - fromTick));
                else
                    return toVal;
            }
        }

        public override void PerformRead(InboundMessage m, uint tick)
        {
            T val = ReadFrom(m);

            if (tick < GameClient.Instance.CurrentTick)
            {
                // can interpolate from this instead of our previous value. Liable to "jump" ... do we want?
                if (tick > fromTick)
                {
                    fromVal = val;
                    fromTick = tick;

                    if (fromTick > toTick)
                    {
                        toTick = fromTick;
                        toVal = fromVal;
                    }
                }
            }
            else if (tick < toTick)
            {
                // usurp the current "to" value, and put it back into the queue. Liable to "jump" ... do we want?
                if (toTick > tick)
                    queuedValues[toTick] = toVal;
                toVal = val;
                toTick = tick;
            }
            else
            {
                // put this into the queue, for use later
                queuedValues[tick] = val;
            }
        }

        public abstract T ReadFrom(InboundMessage m);

        protected abstract T Lerp(T val1, T val2, float fraction);
#endif

        private bool interpolate;
        protected internal override string Describe()
        {
            return string.Format("{0},{1}", typeof(T).Name, interpolate ? "Y" : "N");
        } 

        public static implicit operator T(NetworkField<T> f) { return f.Value; }
        public override string ToString() { return Value.ToString(); }
    }

    public class NetworkInt : NetworkField<int>
    {
        public NetworkInt(bool interpolate)
            : base(interpolate)
        {
        }

#if CLIENT
        public override int ReadFrom(InboundMessage m)
        {
            return m.ReadInt();
        }

        protected override int Lerp(int val1, int val2, float fraction)
        {
            return (int)(val1 + (val2 - val1) * fraction);
        }
#elif SERVER
        public override void WriteTo(OutboundMessage m)
        {
            m.Write(val);
        }
#endif
    }

    public class NetworkLong : NetworkField<long>
    {
        public NetworkLong(bool interpolate)
            : base(interpolate)
        {
        }

#if CLIENT
        public override long ReadFrom(InboundMessage m)
        {
            return m.ReadLong();
        }

        protected override long Lerp(long val1, long val2, float fraction)
        {
            return (long)(val1 + (val2 - val1) * fraction);
        }
#elif SERVER
        public override void WriteTo(OutboundMessage m)
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
        public override float ReadFrom(InboundMessage m)
        {
            return m.ReadFloat();
        }

        protected override float Lerp(float val1, float val2, float fraction)
        {
            return val1 + (val2-val1) * fraction;
        }
#elif SERVER
        public override void WriteTo(OutboundMessage m)
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
        public override string ReadFrom(InboundMessage m)
        {
            return m.ReadString();
        }

        protected override string Lerp(string val1, string val2, float fraction) { return val1; } // don't interpolate strings
#elif SERVER
        public override void WriteTo(OutboundMessage m)
        {
            m.Write(val);
        }
#endif
    }
}
*/

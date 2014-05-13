using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Artemis;
using Artemis.Interface;
using System.IO;

namespace FTW.Engine.Shared
{
    public interface INetworkedComponent
    {
        void Read(InboundMessage m);
        void Write(OutboundMessage m);
        uint LastChangedTick { get; }

        bool ShouldSendTo(Client client);
    }

    public abstract class NetworkedComponent : INetworkedComponent
    {
        public abstract void Read(InboundMessage m);
        public abstract void Write(OutboundMessage m);

        protected void Changed() { LastChanged = GameServer.Instance.CurrentTick; }
        public DateTime LastChanged { get; private set; }
    }

    public abstract class NetworkedComponentPoolable : ComponentPoolable, INetworkedComponent
    {
        public abstract void Read(InboundMessage m);
        public abstract void Write(OutboundMessage m);

        protected void Changed() { LastChanged = DateTime.Now; }
        public DateTime LastChanged { get; private set; }
    }

    /// <summary>
    /// The presence of this component indicates that the entity should be networked
    /// </summary>
    [Artemis.Attributes.ArtemisComponentPool(IsSupportMultiThread = true)]
    public class EntityIsNetworked : ComponentPoolable
    {
        SortedList<byte, DateTime> componentDeletions = new SortedList<byte, DateTime>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using System.IO;
using Artemis;
using Artemis.Interface;

namespace Game.Shared
{
    [NetworkedComponent(1)]
    [Artemis.Attributes.ArtemisComponentPool(IsSupportMultiThread = true)]
    public class Position : NetworkedComponentPoolable
    {
        private float x, y;
        public float X { get { return x; } set { x = value; Changed(); } }
        public float Y { get { return y; } set { y = value; Changed(); } }

        public override void Read(InboundMessage m)
        {
            x = m.ReadFloat();
            y = m.ReadFloat();
        }

        public override void Write(OutboundMessage m)
        {
            m.Write(y);
            m.Write(y);
        }
    }

    [NetworkedComponent(2)]
    [Artemis.Attributes.ArtemisComponentPool(IsSupportMultiThread = true)]
    public class Velocity : NetworkedComponentPoolable
    {
        private float dx, dy;
        public float DX { get { return dx; } set { dx = value; Changed(); } }
        public float DY { get { return dy; } set { dy = value; Changed(); } }

        public override void Read(InboundMessage m)
        {
            dx = m.ReadFloat();
            dy = m.ReadFloat();
        }

        public override void Write(OutboundMessage m)
        {
            m.Write(dx);
            m.Write(dy);
        }
    }

    [NetworkedComponent(3)]
    [Artemis.Attributes.ArtemisComponentPool(IsSupportMultiThread = true)]
    public class BouncingMovement : NetworkedComponentPoolable
    {
        public override void Read(InboundMessage m)
        {

        }

        public override void Write(OutboundMessage m)
        {

        }
    }

    [NetworkedComponent(4)]
    public class PlayerMovement : NetworkedComponent
    {
        private ushort clientID;
        public ushort ClientID { get { return clientID; } set { clientID = value; Changed(); } }

        public override void Read(InboundMessage m)
        {
            clientID = m.ReadUShort();
        }

        public override void Write(OutboundMessage m)
        {
            m.Write(clientID);
        }
    }
}

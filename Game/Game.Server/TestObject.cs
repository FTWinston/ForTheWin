using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Server;
using FTW.Engine.Shared;

namespace Game.Server
{
    class TestObject : NetworkedEntity
    {
        public TestObject()
            : base("moving")
        {
        }

        protected override NetworkField[] SetupFields()
        {
            return new NetworkField[] { positionX, positionY };
        }

        NetworkInt positionX = new NetworkInt(false);
        NetworkInt positionY = new NetworkInt(true);
    }
}

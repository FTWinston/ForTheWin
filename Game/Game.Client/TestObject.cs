using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Client;
using FTW.Engine.Shared;

namespace Game.Client
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

        NetworkFloat positionX = new NetworkFloat(false);
        NetworkFloat positionY = new NetworkFloat(true);
    }
}

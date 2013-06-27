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
            positionX.Value = 20; positionY.Value = 0;
        }

        protected override NetworkField[] SetupFields()
        {
            return new NetworkField[] { positionX, positionY };
        }

        NetworkFloat positionX = new NetworkFloat(false);
        NetworkFloat positionY = new NetworkFloat(true);

        double speed = 2.0;

        public override void Simulate(double dt)
        {
            positionX.Value += (float)(dt * speed);
            positionY.Value += (float)(dt * speed);

            Console.WriteLine(string.Format("Simulating... now at {0}, {1}", positionX, positionY));
        }
    }
}

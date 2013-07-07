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

        NetworkFloat positionX = new NetworkFloat(true);
        NetworkFloat positionY = new NetworkFloat(true);

        double sx = 60.0, sy = 60.0;
        
        const int minX = 50, maxX = 750, minY = 50, maxY = 550;
        public override void Simulate(double dt)
        {
            positionX.Value += (float)(dt * sx);
            if ( sx > 0 )
            {
                if ( positionX > maxX )
                    sx = -sx;
            }
            else if ( positionX < minX )
                sx = -sx;

            positionY.Value += (float)(dt * sy);
            if (sy > 0)
            {
                if (positionY > maxY)
                    sy = -sy;
            }
            else if (positionY < minY)
                sy = -sy;

            //Console.WriteLine("Simulating... now at {0}, {1}", positionX, positionY);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Client;
using FTW.Engine.Shared;
using SFML.Graphics;
using SFML.Window;

namespace Game.Client
{
    class TestObject : NetworkedEntity, Drawable
    {
        public TestObject()
            : base("moving")
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            Console.WriteLine("Initializing object...");

            shape = new RectangleShape();
            shape.Size = new Vector2f(16f, 16f);
            shape.FillColor = new Color(255, 0, 0, 220);

            GameClient.AddDrawable(this);
        }

        RectangleShape shape;
        public override void Delete()
        {
            base.Delete();
            GameClient.RemoveDrawable(this);
        }

        protected override NetworkField[] SetupFields()
        {
            return new NetworkField[] { positionX, positionY };
        }

        NetworkFloat positionX = new NetworkFloat(true);
        NetworkFloat positionY = new NetworkFloat(true);

        public void Draw(RenderTarget target, RenderStates states)
        {
            shape.Position = new Vector2f(positionX, positionY);
            target.Draw(shape, states);
        }
    }
}

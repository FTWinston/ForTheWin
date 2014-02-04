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
    public abstract class TestObject : NetworkedEntity, Drawable
    {
        public TestObject(string networkType)
            : base(networkType)
        {
        }
        
        public virtual Vector2f Size
        {
            get
            {
                return new Vector2f(16f, 16f);
            }
        }

        public virtual Color Color
        {
            get
            {
                return new Color(255, 0, 0, 220);
            }
        }
        
        protected override void Initialize()
        {
            base.Initialize();

            Console.WriteLine("Initializing object...");

            shape = new RectangleShape();
            shape.Size = Size;
            shape.FillColor = Color; 

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

    public class Obstacle : TestObject
    {
        public Obstacle()
            : base("obstacle")
        {

        }

    }

    public class Player : TestObject
    {
        public Player()
            : base("player")
        {

        }

        public override Vector2f Size
        {
            get
            {
                return new Vector2f(20, 20);
            }
        }

        public override Color Color
        {
            get
            {
                return new Color(0, 255, 255, 255);
            }
        }
    }
}

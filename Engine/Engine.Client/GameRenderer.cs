using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using FTW.Engine.Shared;
using SFML.Window;

namespace FTW.Engine.Client
{
    public class GameRenderer : InputListener, Drawable
    {
        public static GameRenderer Instance;
        public ServerConnection Connection { get; private set; }
        public bool FullyConnected { get; internal set; }

        public GameRenderer(RenderWindow window, ServerConnection connection, Config config)
            : base(window)
        {
            Instance = this;
            Connection = connection;
            FullyConnected = false;
            
            Connection.Connect();
        }

        public void Disconnect()
        {
            Connection.Disconnect();

            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);

            Instance = null;
        }

        public event EventHandler Disconnected;

        public override void Draw(RenderTarget target, RenderStates states)
        {
            Connection.RetrieveUpdates();

            if (!FullyConnected)
            {
                // draw a "connecting" thingamy
            }
            else
            {
                // draw an "in game" thingamy
            }
        }

        public event EventHandler ShowMenu;
        protected override void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                if (ShowMenu != null)
                    ShowMenu(this, EventArgs.Empty);
            }
        }

        protected override void OnKeyReleased(object sender, KeyEventArgs e)
        {
        }

        protected override void OnTextEntered(object sender, TextEventArgs e)
        {
        }

        protected override void OnMousePressed(object sender, MouseButtonEventArgs e)
        {
        }

        protected override void OnMouseReleased(object sender, MouseButtonEventArgs e)
        {
        }

        protected override void OnMouseMoved(object sender, MouseMoveEventArgs e)
        {
        }
    }
}

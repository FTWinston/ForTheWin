using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using FTW.Engine.Shared;
using SFML.Window;

namespace FTW.Engine.Client
{
    public class GameClient : InputListener, Drawable
    {
        public static GameClient Instance;
        public ServerConnection Connection { get; private set; }
        public bool FullyConnected { get; internal set; }

        public GameClient(RenderWindow window, ServerConnection connection, Config config)
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

        internal void HandleMessage(Message m)
        {
            if (!MessageReceived(m))
                Console.Error.WriteLine("Received an unrecognised message of Type " + m.Type);
        }

        protected internal virtual bool MessageReceived(Message m)
        {
            switch ((EngineMessage)m.Type)
            {
                case EngineMessage.ClientConnected:
                    {
                        string clientName = m.ReadString();

                        Console.WriteLine(clientName + " joined the game");
                        return true;
                    }
                case EngineMessage.PlayerList:
                    {
                        string clientName = m.ReadString();
                        Console.WriteLine("My name, corrected by server: " + clientName);

                        byte numOthers = m.ReadByte();

                        Console.WriteLine("There are " + numOthers + " other clients connected to this server:");

                        for (int i = 0; i < numOthers; i++)
                        {
                            string otherName = m.ReadString();
                            Console.WriteLine(" * " + otherName);
                        }

                        GameClient.Instance.FullyConnected = true;
                        return true;
                    }
                default:
                    return false;
            }
        }

        public void SendMessage(Message m)
        {
            Connection.Send(m);
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

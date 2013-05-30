using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using SFML.Graphics;
using SFML.Window;

namespace Game.Client
{
    class GameClient : FTW.Engine.Client.GameClient, Drawable, InputListener
    {   
        const string settingsFilename = "client.yml";

        GameWindow window;
        public GameClient(GameWindow window)
            : base(Config.ReadFile(settingsFilename) ?? CreateDefaultConfig())
        {
            this.window = window;
        }

        const string defaultClientName = "Some Client";
        public static Config CreateDefaultConfig()
        {
            Config config = new Config(null);
            config.Children = new List<Config>();

            Config value = new Config("name");
            value.Value = defaultClientName;
            config.Children.Add(value);

            config.SaveToFile(settingsFilename);
            return config;
        }


        public void Draw(RenderTarget target, RenderStates states)
        {
            Update();

            if (FullyConnected)
                ; // draw "connected" stuff
            else
                ; // draw "connecting" stuff
        }

        public void KeyPressed(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                window.CurrentMenu = window.InGameMenu;
                return;
            }

            // ...
        }

        public void KeyReleased(KeyEventArgs e) { }
        public void TextEntered(TextEventArgs e) { }
        public void MousePressed(MouseButtonEventArgs e) { }
        public void MouseReleased(MouseButtonEventArgs e) { }
        public void MouseMoved(MouseMoveEventArgs e) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using SFML.Graphics;
using SFML.Window;
using System.IO;
using System.Runtime.InteropServices;

namespace Game.Client
{
    class GameClient : FTW.Engine.Client.GameClient, Drawable, InputListener
    {   
        public const string settingsFilename = "client.yml";

        GameWindow window;
        public GameClient(GameWindow window, Config config)
            : base(config)
        {
            instance = this;
            this.window = window;
            SetupDisplay();
        }

        public const string defaultClientName = "Some Client";
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

        private static GameClient instance;
        public static void AddDrawable(Drawable d)
        {
            instance.gameElements.Add(d);
        }

        public static void RemoveDrawable(Drawable d)
        {
            instance.gameElements.Remove(d);
        }

        Text loading;
        List<Drawable> gameElements = new List<Drawable>();

        public View MainView { get; set; }
        ConsolePanel console;
        private bool showConsole;
        public bool ShowConsole
        {
            get { return showConsole; }
            set
            {
                showConsole = value;
                window.CurrentInput = ShowConsole ? (InputListener)console : (InputListener)this;
            }
        }

        public void SetupDisplay()
        {
            Font font = new Font("Resources/arial.ttf");

            loading = new Text("Connecting...", font, 64);
            loading.Color = Color.White;
            loading.Position = new Vector2f(window.Size.X / 2 - loading.GetLocalBounds().Width / 2, window.Size.Y / 2 - loading.GetLocalBounds().Height / 2);

            MainView = window.DefaultView;

            Text info = new Text("This is the game. Blah blah.", font, 48);
            info.Color = Color.Yellow;
            info.Position = new Vector2f(window.Size.X / 2 - info.GetLocalBounds().Width / 2, window.Size.Y / 2 - info.GetLocalBounds().Height / 2);

            gameElements.Add(info);

            console = new ConsolePanel(window, this);
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            Update();

            if (FullyConnected)
            {
                foreach ( Drawable d in gameElements )
                    target.Draw(d);

                if (ShowConsole)
                    target.Draw(console);
            }
            else
                target.Draw(loading);
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

        public void KeyReleased(KeyEventArgs e)
        {
            
        }

        public void TextEntered(TextEventArgs e)
        {
            // using TextEntered because there's no KeyCode for using backtick in KeyPressed
            if (e.Unicode == ConsolePanel.ShowKey1 || e.Unicode == ConsolePanel.ShowKey2)
            {
                ShowConsole = true;
                return;
            }
        }

        public void MousePressed(MouseButtonEventArgs e)
        {
            
        }

        public void MouseReleased(MouseButtonEventArgs e)
        {

        }

        public void MouseMoved(MouseMoveEventArgs e)
        {

        }

        protected override bool MessageReceived(Message m)
        {
            if (base.MessageReceived(m))
                return true;

            // ...
            return false;
        }

        protected override bool ConsoleCommand(string firstWord, string theRest)
        {
            if (base.ConsoleCommand(firstWord, theRest))
                return true;

            // ...
            return false;
        }
    }
}

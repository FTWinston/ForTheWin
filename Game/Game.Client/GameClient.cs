using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FTW.Engine.Shared;
using SFML.Graphics;
using SFML.Window;
using System.IO;
using System.Runtime.InteropServices;
using Game.Shared;
using FTW.Engine.Client;

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
            Disconnected += (e, o) => gameElements.Clear();
        }

        public const string defaultClientName = "Some Client";
        public const string defaultServerIP = "127.0.0.1";
        public const int defaultServerPort = 24680;

        public const string config_ClientName = "name", config_ServerIP = "server_ip", config_ServerPort = "server_port";

        public static Config CreateDefaultConfig()
        {
            Config config = new Config(null);
            config.Children = new List<Config>();

            Config value = new Config(config_ClientName);
            value.Value = defaultClientName;
            config.Children.Add(value);

            value = new Config(config_ServerIP);
            value.Value = defaultServerIP;
            config.Children.Add(value);

            value = new Config(config_ServerPort);
            value.Value = defaultServerPort.ToString();
            config.Children.Add(value);

            config.SaveToFile(settingsFilename);
            return config;
        }

        protected override void SetupVariableDefaults()
        {
            Variable.Get("name").SetDefaultValue("Player");
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

            txtInfo = new Text("Net Info", font, 16);
            txtInfo.Color = Color.Yellow;
            txtInfo.Position = new Vector2f(window.Size.X * 0.02f, window.Size.Y * 0.02f);

            gameElements.Add(txtInfo);

            console = new ConsolePanel(window, this);
        }

        Text txtInfo;
        private void WriteNetGraph()
        {
            if (!NetInfo.Enabled)
            {
                txtInfo.DisplayedString = string.Empty;
                return;
            }

            uint numIn = 0, numOut = 0;
            float sizeIn = 0, sizeOut = 0;
            foreach (var item in NetInfo.Data)
                if ( item.Outgoing )
                {
                    numOut++;
                    sizeOut += item.Size;
                }
                else
                {
                    numIn++;
                    sizeIn += item.Size;
                }

            txtInfo.DisplayedString = string.Format("In: {0}, {1} kb/sec\nOut: {2}, {3} kb/sec", numIn, sizeIn / NetInfo.DataDuration, numOut, sizeOut / NetInfo.DataDuration);
        }

        protected override void PreUpdate()
        {
            WriteNetGraph();
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

        Keys movement = Keys.None;

        public void KeyPressed(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                window.CurrentMenu = window.InGameMenu;
                return;
            }

            if (e.Code == Keyboard.Key.Up)
                movement |= Keys.Up;
            else if (e.Code == Keyboard.Key.Down)
                movement |= Keys.Down;
            if (e.Code == Keyboard.Key.Left)
                movement |= Keys.Left;
            else if (e.Code == Keyboard.Key.Right)
                movement |= Keys.Right;
        }

        public void KeyReleased(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Up)
                movement &= ~Keys.Up;
            else if (e.Code == Keyboard.Key.Down)
                movement &= ~Keys.Down;
            if (e.Code == Keyboard.Key.Left)
                movement &= ~Keys.Left;
            else if (e.Code == Keyboard.Key.Right)
                movement &= ~Keys.Right;
        }

        protected override void WriteUpdate(Message m)
        {
            m.Write((byte)movement);
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

            switch (firstWord)
            {   
                case "exit":
                case "quit":
                    window.CloseGameWindow();
                    return true;
                case "net":
                    NetInfo.Enabled = !NetInfo.Enabled;
                    return true;
            }
            return false;
        }
    }
}

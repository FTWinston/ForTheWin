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
        Font font;
        public GameClient(GameWindow window, Config config)
            : base(config)
        {
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

        Text loading, console, game;
        public bool ShowConsole;

        public void SetupDisplay()
        {
            font = new Font("Resources/arial.ttf");

            loading = new Text("Connecting...", font, 64);
            loading.Color = Color.White;
            loading.Position = new Vector2f(window.Size.X / 2 - loading.GetLocalBounds().Width / 2, window.Size.Y / 2 - loading.GetLocalBounds().Height / 2);

            console = new Text("", font, 18);
            console.Color = Color.White;
            console.Position = new Vector2f(8, 8);

            game = new Text("This is the game. Blah blah.", font, 48);
            game.Color = Color.Yellow;
            game.Position = new Vector2f(window.Size.X / 2 - game.GetLocalBounds().Width / 2, window.Size.Y / 2 - game.GetLocalBounds().Height / 2);

            TextStreamWriter tw = new TextStreamWriter(console);
            Console.SetOut(tw);
            Console.SetError(tw);
        }

        public class TextStreamWriter : TextWriter
        {
            Text output;
            public TextStreamWriter(Text output)
            {
                this.output = output;
            }

            public override void Write(char value)
            {
                output.DisplayedString += value;
                glFlush();
            }

            public override void WriteLine(string value)
            {
                output.DisplayedString += value + '\n';
                glFlush();
            }

            public override void Write(string value)
            {
                output.DisplayedString += value;
                glFlush();
            }

            public override Encoding Encoding
            {
                get { return Encoding.Unicode; }
            }
        }

        [DllImport("opengl32.dll")]
        private static extern void glFlush();

        public void Draw(RenderTarget target, RenderStates states)
        {
            Update();

            if (FullyConnected)
            {
                if (ShowConsole)
                    target.Draw(console);
                else
                    target.Draw(game);
            }
            else
                target.Draw(loading);
        }

        public void KeyPressed(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                if (ShowConsole)
                    ShowConsole = false;
                else
                    window.CurrentMenu = window.InGameMenu;
                return;
            }

            // ...
        }

        public void KeyReleased(KeyEventArgs e) { }
        public void TextEntered(TextEventArgs e)
        {
            // using TextEntered because there's no KeyCode for using backtick in KeyPressed
            if ( e.Unicode == "`" || e.Unicode == "~" )
                ShowConsole = !ShowConsole;
        }
        public void MousePressed(MouseButtonEventArgs e) { }
        public void MouseReleased(MouseButtonEventArgs e) { }
        public void MouseMoved(MouseMoveEventArgs e) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;
using FTW.Engine.Shared;

namespace Game.Client
{
    public class GameWindow : RenderWindow
    {
        static void Main()
        {
            new GameWindow();
        }

        public Menu MainMenu { get; private set; }
        public Menu InGameMenu { get; private set; }
        public Menu OptionsMenu { get; private set; }
        public Menu CurrentMenu
        {
            get { return currentMenu; }
            set
            {
                currentMenu = value;
                currentInput = currentMenu ?? (InputListener)renderer;
            }
        }

        Menu currentMenu;
        InputListener currentInput;
        GameClient renderer;

        public GameWindow() :
            base(new VideoMode(800, 600, 32), "FTW Example", Styles.Default, new ContextSettings(32, 0))
        {
            SetVerticalSyncEnabled(true);
            
            // Setup event handlers
            KeyPressed += OnKeyPressed;
            KeyReleased += OnKeyReleased;
            TextEntered += OnTextEntered;
            MouseButtonPressed += OnMousePressed;
            MouseButtonReleased += OnMouseReleased;
            MouseMoved += OnMouseMoved;
            Closed += OnClosed;

            CreateMenus();
            CurrentMenu = MainMenu;

            renderer = new GameClient(this);
            renderer.Disconnected += (object o, EventArgs e) => CurrentMenu = MainMenu;

            while (IsOpen())
            {
                DispatchEvents();

                Clear();

                if ( CurrentMenu != null )
                    Draw(CurrentMenu);

                if (renderer != null && renderer.Connected)
                    Draw(renderer);

                Display();
            }
        }

        private void CreateMenus()
        {
            MainMenu = new Menu(this)
            {
                ItemFont = new Font("Resources/arial.ttf"),
                ItemYPos = 96,
                ValueXOffset = 256,
                ItemColor = Color.Green,
                HoverItemColor = Color.Yellow,
                HoverItemStyle = Text.Styles.Underlined
            };

            MainMenu.EscapePressed = () => { CloseGameWindow(); };

            MainMenu.AddItem(new Menu.LinkItem("Host game", () => { renderer.ConnectLocal(); CurrentMenu = null; }));
            MainMenu.AddItem(new Menu.LinkItem("Join game", () => { renderer.ConnectRemote("127.0.0.1", 24680); CurrentMenu = null; }));
            MainMenu.AddItem(new Menu.LinkItem("Options", () => CurrentMenu = OptionsMenu));
            MainMenu.AddItem(new Menu.LinkItem("Quit", () => { CloseGameWindow(); }));



            InGameMenu = new Menu(this);
            InGameMenu.CopyStyling(MainMenu);

            InGameMenu.AddItem(new Menu.LinkItem("Resume", () => { CurrentMenu = null; }));
            InGameMenu.AddItem(new Menu.LinkItem("Disconnect", () => { EndGame(); CurrentMenu = MainMenu; }));

            InGameMenu.EscapePressed = () => { CurrentMenu = null; };

            OptionsMenu = new Menu(this);
            OptionsMenu.CopyStyling(MainMenu);

            OptionsMenu.AddItem(new Menu.ListItem("Choice:", new string[] { "Option 1", "Option 2", "Option 3" }, (string value) => Console.WriteLine(value + " selected")));
            OptionsMenu.AddItem(new Menu.TextEntryItem("Name:", "Player", 12, (string value) => Console.WriteLine("Name changed: " + value)));
            OptionsMenu.AddItem(new Menu.LinkItem("Back", () => CurrentMenu = MainMenu));

            OptionsMenu.EscapePressed = () => { CurrentMenu = MainMenu; };
        }

        private void OnKeyPressed(object sender, KeyEventArgs e) { currentInput.KeyPressed(e); }
        private void OnKeyReleased(object sender, KeyEventArgs e) { currentInput.KeyReleased(e); }
        private void OnTextEntered(object sender, TextEventArgs e) { currentInput.TextEntered(e); }
        private void OnMousePressed(object sender, MouseButtonEventArgs e) { currentInput.MousePressed(e); }
        private void OnMouseReleased(object sender, MouseButtonEventArgs e) { currentInput.MouseReleased(e); }
        private void OnMouseMoved(object sender, MouseMoveEventArgs e) { currentInput.MouseMoved(e); }
        private void OnClosed(object sender, EventArgs e) { CloseGameWindow(); }

        private void CloseGameWindow()
        {
            Close();
            EndGame();
        }

        private void EndGame()
        {
            if ( renderer.Connected )
                renderer.Disconnect();
        }
    }
}

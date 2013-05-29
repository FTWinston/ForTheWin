using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;
using FTW.Engine.Client;
using FTW.Engine.Shared;

namespace Game.Client
{
    public class GameWindow : RenderWindow
    {
        static void Main()
        {
            new GameWindow();
        }

        InputListener currentInput;
        Menu mainMenu, inGameMenu, optionsMenu;
        GameClient renderer;

        public GameWindow() :
            base(new VideoMode(800, 600, 32), "FTW Example", Styles.Default, new ContextSettings(32, 0))
        {
            SetVerticalSyncEnabled(true);
            
            // Setup event handlers
            Closed += OnClosed;

            CreateMenus();
            SetCurrentInput(mainMenu);

            while (IsOpen())
            {
                DispatchEvents();

                Clear();

                Draw(currentInput);

                if (renderer != null && renderer != currentInput) // draw the game behind the menu, if we're in a menu
                    Draw(renderer);

                Display();
            }
        }

        private void CreateMenus()
        {
            mainMenu = new Menu(this)
            {
                ItemFont = new Font("Resources/arial.ttf"),
                ItemYPos = 96,
                ValueXOffset = 256,
                ItemColor = Color.Green,
                HoverItemColor = Color.Yellow,
                HoverItemStyle = Text.Styles.Underlined
            };

            mainMenu.EscapePressed = () => { CloseGameWindow(); };

            mainMenu.AddItem(new Menu.LinkItem("Host game", () => CreateRenderer(new ListenServerConnection())));
            mainMenu.AddItem(new Menu.LinkItem("Join game", () => CreateRenderer(new RemoteClientConnection("127.0.0.1", 24680))));
            mainMenu.AddItem(new Menu.LinkItem("Options", () => SetCurrentInput(optionsMenu)));
            mainMenu.AddItem(new Menu.LinkItem("Quit", () => { CloseGameWindow(); }));



            inGameMenu = new Menu(this);
            inGameMenu.CopyStyling(mainMenu);

            inGameMenu.AddItem(new Menu.LinkItem("Resume", () => { SetCurrentInput(renderer); }));
            inGameMenu.AddItem(new Menu.LinkItem("Disconnect", () => { EndGame(); SetCurrentInput(mainMenu); }));

            inGameMenu.EscapePressed = () => { SetCurrentInput(renderer); };



            optionsMenu = new Menu(this);
            optionsMenu.CopyStyling(mainMenu);

            optionsMenu.AddItem(new Menu.ListItem("Choice:", new string[] { "Option 1", "Option 2", "Option 3" }, (string value) => Console.WriteLine(value + " selected")));
            optionsMenu.AddItem(new Menu.TextEntryItem("Name:", "Player", 12, (string value) => Console.WriteLine("Name changed: " + value)));
            optionsMenu.AddItem(new Menu.LinkItem("Back", () => SetCurrentInput(mainMenu)));

            optionsMenu.EscapePressed = () => { SetCurrentInput(mainMenu); };
        }

        private void CreateRenderer(ServerConnection connection)
        {
            renderer = new GameClient(this, connection, new Config());
            renderer.ShowMenu += (object o, EventArgs e) => SetCurrentInput(inGameMenu);
            renderer.Disconnected += (object o, EventArgs e) => { renderer = null; SetCurrentInput(mainMenu); };
            SetCurrentInput(renderer);
        }

        private void SetCurrentInput(InputListener item)
        {
            if (currentInput != null)
                currentInput.DisableInput();

            currentInput = item;

            if (currentInput != null)
                currentInput.EnableInput();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            CloseGameWindow();
        }

        private void CloseGameWindow()
        {
            Close();
            EndGame();
        }

        private void EndGame()
        {
            if (renderer != null)
            {
                renderer.Disconnect();
                renderer = null;
            }
        }
    }
}

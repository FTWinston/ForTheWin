using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Window;
using SFML.Graphics;
using FTW.Engine.Client;

namespace Game.Client
{
    static class Program
    {
        static void Main()
        {
            // Create the main window
            RenderWindow window = new RenderWindow(new VideoMode(800, 600, 32), "FTW Example", Styles.Default, new ContextSettings(32, 0));
            window.SetVerticalSyncEnabled(true);

            // Setup event handlers
            window.Closed += new EventHandler(OnClosed);
            window.KeyPressed += new EventHandler<KeyEventArgs>(OnKeyPressed);
            window.Resized += new EventHandler<SizeEventArgs>(OnResized);

            CreateMainMenu(window);
            CreateOptionsMenu(window);
            SetCurrentMenu(mainMenu);

            while (window.IsOpen())
            {
                window.DispatchEvents();

                window.Clear();

                window.Draw(currentMenu);

                window.Display();
            }
        }

        private static void SetCurrentMenu(Menu menu)
        {
            if (currentMenu != null)
                currentMenu.DisableInput();

            currentMenu = menu;
            currentMenu.EnableInput();
        }

        static Menu currentMenu, mainMenu, optionsMenu;

        private static void CreateMainMenu(RenderWindow window)
        {
            mainMenu = new Menu(window);
            mainMenu.ItemFont = new Font("Resources/arial.ttf");
            mainMenu.ItemYPos = 96;
            mainMenu.ValueXOffset = 256;
            mainMenu.ItemColor = Color.Green;
            mainMenu.HoverItemColor = Color.Yellow;
            mainMenu.HoverItemStyle = Text.Styles.Underlined;

            mainMenu.EscapePressed = () => { window.Close(); Console.WriteLine("escape pressed"); };

            mainMenu.AddItem(new Menu.LinkItem("Host game", () => Console.WriteLine("host clicked")));
            mainMenu.AddItem(new Menu.LinkItem("Join game", () => Console.WriteLine("join clicked")));
            mainMenu.AddItem(new Menu.LinkItem("Options", () => SetCurrentMenu(optionsMenu)));
            mainMenu.AddItem(new Menu.LinkItem("Quit", () => { window.Close(); Console.WriteLine("close clicked"); }));
        }

        private static void CreateOptionsMenu(RenderWindow window)
        {
            optionsMenu = new Menu(window);
            optionsMenu.CopyStyling(mainMenu);

            optionsMenu.AddItem(new Menu.ListItem("Choice:", new string[] { "Option 1", "Option 2", "Option 3" }, (string value) => Console.WriteLine(value + " selected")));
            optionsMenu.AddItem(new Menu.TextEntryItem("Name:", "Player", 12, (string value) => Console.WriteLine("Name changed: " + value)));
            optionsMenu.AddItem(new Menu.LinkItem("Back", () => SetCurrentMenu(mainMenu)));

            optionsMenu.EscapePressed = () => { SetCurrentMenu(mainMenu); };
        }

        /// <summary>
        /// Function called when the window is closed
        /// </summary>
        static void OnClosed(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Close();
        }

        /// <summary>
        /// Function called when a key is pressed
        /// </summary>
        static void OnKeyPressed(object sender, KeyEventArgs e)
        {
            /*Window window = (Window)sender;
            if (e.Code == Keyboard.Key.Escape)
                window.Close();*/
        }

        /// <summary>
        /// Function called when the window is resized
        /// </summary>
        static void OnResized(object sender, SizeEventArgs e)
        {
            //Gl.glViewport(0, 0, (int)e.Width, (int)e.Height);
        }
    }
}

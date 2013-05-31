using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using System.IO;
using System.Runtime.InteropServices;
using SFML.Window;

namespace Game.Client
{
    class ConsolePanel : TextWriter, Drawable
    {
        [DllImport("opengl32.dll")]
        private static extern void glFlush();

        Text text;
        RectangleShape background, scrollUp, scrollDown, scrollBar;
        CircleShape buttonUp, buttonDown;
        View view;

        private const float scrollButtonSize = 32, scrollButtonIconRadius = scrollButtonSize * 0.25f, scrollButtonIconOffset = scrollButtonSize / 2f - scrollButtonIconRadius;
        public ConsolePanel(RenderWindow window)
        {
            Font font = new Font("Resources/arial.ttf");
            text = new Text("", font, 18);
            text.Color = Color.White;
            text.Position = new Vector2f(8, 8);

            background = new RectangleShape();
            background.Position = new Vector2f(0, 0);
            background.Size = new Vector2f(window.Size.X, window.Size.Y);
            background.FillColor = new Color(72, 72, 72, 220);

            scrollUp = new RectangleShape();
            scrollUp.Position = new Vector2f(window.Size.X - scrollButtonSize, 0);
            scrollUp.Size = new Vector2f(scrollButtonSize, scrollButtonSize);
            scrollUp.FillColor = new Color(96, 96, 96);

            buttonUp = new CircleShape(scrollButtonIconRadius, 3);
            buttonUp.Position = new Vector2f(scrollUp.Position.X + scrollButtonIconOffset, scrollUp.Position.Y + scrollButtonIconOffset);
            buttonUp.FillColor = new Color(192, 192, 192);

            scrollDown = new RectangleShape();
            scrollDown.Position = new Vector2f(window.Size.X - scrollButtonSize, window.Size.Y / 2 - scrollButtonSize);
            scrollDown.Size = scrollUp.Size;
            scrollDown.FillColor = scrollUp.FillColor;

            buttonDown = new CircleShape(scrollButtonIconRadius, 3);
            buttonDown.Position = new Vector2f(scrollDown.Position.X + scrollButtonIconOffset * 3, scrollDown.Position.Y + scrollButtonIconOffset * 3);
            buttonDown.FillColor = buttonUp.FillColor;
            buttonDown.Rotation = 180;

            scrollBar = new RectangleShape();
            scrollBar.Position = new Vector2f(scrollUp.Position.X, scrollButtonSize);
            scrollBar.Size = new Vector2f(scrollButtonSize, window.Size.Y / 2 - scrollButtonSize - scrollButtonSize);
            scrollBar.FillColor = new Color(32, 32, 32, 128);

            view = new View(new FloatRect(0, 0, window.Size.X, window.Size.Y / 2));
            view.Viewport = new FloatRect(0, 0, 1, 0.5f);

            Console.SetOut(this);
            Console.SetError(this);
        }

        public override void Write(char value)
        {
            text.DisplayedString += value;
            glFlush();
        }

        public override void WriteLine(string value)
        {
            text.DisplayedString += value + '\n';
            glFlush();
        }

        public override void Write(string value)
        {
            text.DisplayedString += value;
            glFlush();
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.SetView(view);
            target.Draw(background);
            target.Draw(text);
            target.SetView(target.DefaultView);
            target.Draw(scrollBar);
            target.Draw(scrollUp);
            target.Draw(buttonUp);
            target.Draw(scrollDown);
            target.Draw(buttonDown);
        }
    }
}

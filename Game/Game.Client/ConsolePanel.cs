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

        Text text, input;
        RectangleShape background, scrollUp, scrollDown, scrollBar;
        CircleShape buttonUp, buttonDown;
        View view;
        FloatRect upButtonBounds, downButtonBounds;

        private float maxDisplayedHeight, maxScroll, scrollX;
        private const float scrollButtonSize = 32, scrollButtonIconRadius = scrollButtonSize * 0.25f, scrollButtonIconOffset = scrollButtonSize / 2f - scrollButtonIconRadius;
        public ConsolePanel(RenderWindow window)
        {
            Font font = new Font("Resources/arial.ttf");
            
            text = new Text("", font, 18);
            text.Color = new Color(208, 208, 208);
            text.Position = new Vector2f(8, 8);

            input = new Text("", font, text.CharacterSize);
            input.Color = Color.White;
            input.Position = new Vector2f(8, window.Size.Y / 2 - text.CharacterSize - 4);

            maxDisplayedHeight = input.Position.Y - 4;

            background = new RectangleShape();
            background.Position = new Vector2f(0, 0);
            background.Size = new Vector2f(window.Size.X, window.Size.Y/2);
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

            upButtonBounds = scrollUp.GetGlobalBounds();
            downButtonBounds = scrollDown.GetGlobalBounds();

            view = new View(new FloatRect(0, 0, window.Size.X, window.Size.Y / 2));
            view.Viewport = new FloatRect(0, 0, 1, 0.5f);

            scrollX = view.Center.X;
            maxScroll = view.Center.Y;

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
            Write(value + '\n');
        }

        public override void Write(string value)
        {
            text.DisplayedString += value;

            float outputHeight = text.GetLocalBounds().Height;

            if (outputHeight > maxDisplayedHeight)
                text.Position = new Vector2f(text.Position.X, maxDisplayedHeight - outputHeight);

            glFlush();
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            target.Draw(background);
            target.SetView(view);
            target.Draw(text);
            target.Draw(input);
            target.SetView(target.DefaultView);
            target.Draw(scrollBar);
            target.Draw(scrollUp);
            target.Draw(buttonUp);
            target.Draw(scrollDown);
            target.Draw(buttonDown);
        }

        internal void TextEntered(string text)
        {
            if (text != "\r" && text != "\b")
            {
                input.DisplayedString += text;
                ScrollToEnd();
            }
        }

        internal void KeyPressed(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Return)
            {
                string text = input.DisplayedString.Trim();
                if (text.Length > 0)
                {
                    // actually send it somewhere ... err ... this isn't really it, is it?
                    // no, we should use Console.SetIn to get this, somehow
                    Console.WriteLine(text);
                }

                input.DisplayedString = string.Empty;
            }
            else if (e.Code == Keyboard.Key.Back)
            {
                if (input.DisplayedString.Length > 0)
                    input.DisplayedString = input.DisplayedString.Substring(0, input.DisplayedString.Length - 1);
            }
            else
                return;

            ScrollToEnd();
        }

        internal void MousePressed(MouseButtonEventArgs e)
        {
            if (e.Button == Mouse.Button.Left)
                if (upButtonBounds.Contains(e.X, e.Y))
                    Scroll(true);
                else if (downButtonBounds.Contains(e.X, e.Y))
                    Scroll(false);
        }

        float scrollIncrement = 50;
        void Scroll(bool up)
        {
            if (up)
            {
                float minScroll = text.GetGlobalBounds().Top + maxScroll - 11; // can't work out where the 11's from... never mind.
                view.Center = new Vector2f(scrollX, Math.Max(minScroll, view.Center.Y - scrollIncrement));
            }
            else
            {
                view.Center = new Vector2f(scrollX, Math.Min(maxScroll, view.Center.Y + scrollIncrement));
            }
        }

        void ScrollToEnd()
        {
            float x = view.Center.X;
            view.Center = new Vector2f(x, maxScroll);
        }
    }
}

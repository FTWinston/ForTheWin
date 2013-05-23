using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;

namespace FTW.Engine.Client
{
    public abstract class InputListener : Drawable
    {
        protected InputListener(RenderWindow window)
        {
            this.window = window;

            keyPressed = new EventHandler<KeyEventArgs>(OnKeyPressed);
            keyReleased = new EventHandler<KeyEventArgs>(OnKeyReleased);
            textEntered = new EventHandler<TextEventArgs>(OnTextEntered);
            mousePressed = new EventHandler<MouseButtonEventArgs>(OnMousePressed);
            mouseReleased = new EventHandler<MouseButtonEventArgs>(OnMouseReleased);
            mouseMoved = new EventHandler<MouseMoveEventArgs>(OnMouseMoved);
        }

        protected RenderWindow window { get; private set; }
        
        public void EnableInput()
        {
            window.KeyPressed += keyPressed;
            window.KeyReleased += keyReleased;
            window.TextEntered += textEntered;
            window.MouseButtonPressed += mousePressed;
            window.MouseButtonReleased += mouseReleased;
            window.MouseMoved += mouseMoved;
        }

        public void DisableInput()
        {
            window.KeyPressed -= keyPressed;
            window.KeyReleased -= keyReleased;
            window.TextEntered -= textEntered;
            window.MouseButtonPressed -= mousePressed;
            window.MouseButtonReleased -= mouseReleased;
            window.MouseMoved -= mouseMoved;
        }

        private EventHandler<KeyEventArgs> keyPressed, keyReleased;
        private EventHandler<TextEventArgs> textEntered;
        private EventHandler<MouseButtonEventArgs> mousePressed, mouseReleased;
        private EventHandler<MouseMoveEventArgs> mouseMoved;

        protected abstract void OnKeyPressed(object sender, KeyEventArgs e);
        protected abstract void OnKeyReleased(object sender, KeyEventArgs e);
        protected abstract void OnTextEntered(object sender, TextEventArgs e);
        protected abstract void OnMousePressed(object sender, MouseButtonEventArgs e);
        protected abstract void OnMouseReleased(object sender, MouseButtonEventArgs e);
        protected abstract void OnMouseMoved(object sender, MouseMoveEventArgs e);

        public abstract void Draw(RenderTarget target, RenderStates states);
    }
}

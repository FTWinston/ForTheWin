using System;
using SFML.Window;

namespace Game.Client
{
    public interface InputListener
    {
        void KeyPressed(KeyEventArgs e);
        void KeyReleased(KeyEventArgs e);
        void TextEntered(TextEventArgs e);
        void MousePressed(MouseButtonEventArgs e);
        void MouseReleased(MouseButtonEventArgs e);
        void MouseMoved(MouseMoveEventArgs e);
    }
}

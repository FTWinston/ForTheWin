using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using SFML.Window;

namespace FTW.Engine.Client
{
    public class Menu : Drawable
    {
        public Menu(Window window)
        {
            this.window = window;
            keyPressed = new EventHandler<KeyEventArgs>(OnKeyPressed);
            keyReleased = new EventHandler<KeyEventArgs>(OnKeyReleased);
            textEntered = new EventHandler<TextEventArgs>(OnTextEntered);
            mousePressed = new EventHandler<MouseButtonEventArgs>(OnMousePressed);
            mouseReleased = new EventHandler<MouseButtonEventArgs>(OnMouseReleased);
            mouseMoved = new EventHandler<MouseMoveEventArgs>(OnMouseMoved);

            ItemXPos = ItemYPos = 128;

            ItemXSpacing = 0;
            ItemYSpacing = 96;
            ItemTextSize = 64;
            ValueXOffset = 192;

            ItemColor = HoverItemColor = PressedItemColor = Color.White;

            CurrentIndex = -1;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            foreach (Drawable item in Items)
                target.Draw(item, states);
        }

        public int ItemXPos { get; set; }
        public int ItemYPos { get; set; }
        public float ItemXSpacing { get; set; }
        public float ItemYSpacing { get; set; }
        public uint ItemTextSize { get; set; }
        public Color ItemColor { get; set; }
        public Color HoverItemColor { get; set; }
        public Color PressedItemColor { get; set; }
        public Text.Styles ItemStyle { get; set; }
        public Text.Styles HoverItemStyle { get; set; }
        public Text.Styles PressedItemStyle { get; set; }
        public Font ItemFont { get; set; }
        public int ValueXOffset { get; set; }

        public void CopyStyling(Menu other)
        {
            ItemXPos = other.ItemXPos;
            ItemYPos = other.ItemYPos;
            ItemXSpacing = other.ItemXSpacing;
            ItemYSpacing = other.ItemYSpacing;
            ItemTextSize = other.ItemTextSize;
            ItemColor = other.ItemColor;
            HoverItemColor = other.HoverItemColor;
            PressedItemColor = other.PressedItemColor;
            ItemStyle = other.ItemStyle;
            HoverItemStyle = other.HoverItemStyle;
            HoverItemStyle = other.HoverItemStyle;
            PressedItemStyle = other.PressedItemStyle;
            ItemFont = other.ItemFont;
            ValueXOffset = other.ValueXOffset;
        }

        public ItemActivatedFunction EscapePressed { get; set; }

        public int CurrentIndex { get; protected set; }
        public Item CurrentItem { get; protected set; }
        public bool CurrentPressed { get; protected set; }

        private Window window;
        private List<Item> Items = new List<Item>();

        public delegate void ItemActivatedFunction();
        public delegate void ValueChangedFunction(string value);

        public void AddItem(Item item)
        {
            item.AddTo(this);
            Items.Add(item);
        }

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

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Escape)
            {
                if (EscapePressed != null)
                    EscapePressed();
            }
            else if (e.Code == Keyboard.Key.Up)
            {
                int index = CurrentIndex - 1;
                if (index < 0)
                    index = Items.Count - 1;
                ChangeCurrentIndex(index);
            }
            else if (e.Code == Keyboard.Key.Down)
            {
                int index = CurrentIndex + 1;
                if (index >= Items.Count)
                    index = 0;
                ChangeCurrentIndex(index);
            }
            else if (e.Code == Keyboard.Key.Left)
            {
                if (CurrentItem == null)
                    return;
                    
                if (CurrentItem is ListItem)
                    (CurrentItem as ListItem).Cycle(false);
            }
            else if (e.Code == Keyboard.Key.Right)
            {
                if (CurrentItem is ListItem)
                    (CurrentItem as ListItem).Cycle(true);
            }
            else if (e.Code == Keyboard.Key.Return)
            {
                ItemPressed(true);

                if (CurrentItem is ListItem)
                    (CurrentItem as ListItem).DoButtonPress(true, true);
            }
            else if (e.Code == Keyboard.Key.Back)
            {
                if (CurrentItem == null)
                    return;

                if (CurrentItem is TextEntryItem)
                    (CurrentItem as TextEntryItem).Delete();
            }
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.Return)
            {
                if (CurrentPressed)
                {
                    ItemPressed(false);

                    if (CurrentItem is ListItem)
                        (CurrentItem as ListItem).DoButtonPress(true, false);
                }
            }
        }

        private void ItemPressed(bool keyDown)
        {
            if (CurrentItem == null)
                return;

            CurrentPressed = keyDown;

            if (keyDown)
            {
                if (CurrentItem is LinkItem)
                {
                    CurrentItem.Color = PressedItemColor;
                    CurrentItem.Style = PressedItemStyle;
                }
            }
            else
            {
                CurrentItem.Color = HoverItemColor;
                CurrentItem.Style = HoverItemStyle;

                if (CurrentItem is LinkItem)
                    (CurrentItem as LinkItem).Activated();
            }
        }

        private void OnTextEntered(object sender, TextEventArgs e)
        {
            if (CurrentItem == null)
                return;

            if (CurrentItem is TextEntryItem)
                (CurrentItem as TextEntryItem).Type(e.Unicode);
        }

        private void OnMousePressed(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != Mouse.Button.Left)
                return;

            if (CurrentItem == null)
                return;

            ItemPressed(true);

            if (CurrentItem is ListItem)
                (CurrentItem as ListItem).CheckButtonPress(e.X, e.Y, true);
        }

        private void OnMouseReleased(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != Mouse.Button.Left)
                return;

            if ( CurrentItem == null )
                return;

            if (CurrentPressed)
            {
                ItemPressed(false);

                if (CurrentItem is ListItem)
                    (CurrentItem as ListItem).CheckButtonPress(e.X, e.Y, false);
            }
        }

        private void OnMouseMoved(object sender, MouseMoveEventArgs e)
        {
            int index = GetHoveredItemIndex(e.X, e.Y);
            if (index != CurrentIndex)
                ChangeCurrentIndex(index);
        }

        private void ChangeCurrentIndex(int newIndex)
        {
            int oldIndex = CurrentIndex;
            CurrentIndex = newIndex;
            CurrentPressed = false; // if we change mid-way through a press, forget about that press

            if ( oldIndex < Items.Count && oldIndex >= 0 )
            {
                Items[oldIndex].Color = ItemColor;
                Items[oldIndex].Style = ItemStyle;
            }
            if (newIndex < Items.Count && newIndex >= 0)
            {
                CurrentItem = Items[CurrentIndex];
                CurrentItem.Color = HoverItemColor;
                CurrentItem.Style = HoverItemStyle;
            }
            else
                CurrentItem = null;
        }

        private int GetHoveredItemIndex(int x, int y)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Contains(x, y))
                    return i;
            return -1;
        }

        public abstract class Item : Drawable
        {
            protected internal abstract void AddTo(Menu menu);
            public abstract Color Color { get; set; }
            public abstract Text.Styles Style { get; set; }
            public abstract void Draw(RenderTarget target, RenderStates states);
            public abstract bool Contains(int x, int y);
        }

        public class LinkItem : Item
        {
            private Text Text;
            public ItemActivatedFunction Activated { get; protected set; }

            public LinkItem(string label, ItemActivatedFunction itemClicked)
            {
                Text = new Text(label, null);
                Activated = itemClicked;
            }

            protected internal override void AddTo(Menu menu)
            {
                Text.Font = menu.ItemFont;
                Text.CharacterSize = menu.ItemTextSize;
                Text.Color = menu.ItemColor;
                Text.Position = new Vector2f(menu.ItemXPos + menu.Items.Count * menu.ItemXSpacing, menu.ItemYPos + menu.Items.Count * menu.ItemYSpacing);
            }

            public override Color Color
            {
                get { return Text.Color; }
                set { Text.Color = value; }
            }

            public override Text.Styles Style
            {
                get { return Text.Style; }
                set { Text.Style = value; }
            }

            public override void Draw(RenderTarget target, RenderStates states)
            {
                Text.Draw(target, states);
            }

            public override bool Contains(int x, int y)
            {
                return Text.GetGlobalBounds().Contains(x, y);
            }
        }

        public class ListItem : Item
        {
            private Text Label, Value, Left, Right;
            private int selectedIndex = 0;
            private string[] Values;
            private Menu menu;
            public ValueChangedFunction ValueChanged { get; protected set; }

            public ListItem(string label, IList<string> values, ValueChangedFunction valueChanged)
            {
                Values = values.ToArray();
                Label = new Text(label, null);
                Value = new Text();
                ValueChanged = valueChanged;
            }

            protected internal override void AddTo(Menu menu)
            {
                this.menu = menu;

                Label.Font = menu.ItemFont;
                Label.CharacterSize = menu.ItemTextSize;
                Label.Color = menu.ItemColor;
                Label.Position = new Vector2f(menu.ItemXPos + menu.Items.Count * menu.ItemXSpacing, menu.ItemYPos + menu.Items.Count * menu.ItemYSpacing);

                Left = new Text("<", menu.ItemFont);
                Left.Position = new Vector2f(menu.ItemXPos + menu.ValueXOffset + menu.Items.Count * menu.ItemXSpacing, Label.Position.Y);
                Left.CharacterSize = menu.ItemTextSize;
                Left.Color = menu.ItemColor;

                FloatRect bounds = Left.GetGlobalBounds();
                float extraWidth = bounds.Width * 0.25f;

                Value.Font = menu.ItemFont;
                Value.CharacterSize = menu.ItemTextSize;
                Value.Color = menu.ItemColor;
                Value.Position = new Vector2f(bounds.Left + bounds.Width + extraWidth, Label.Position.Y);

                float biggestWidth = 0;

                foreach (string value in Values)
                {
                    Value.DisplayedString = value;
                    bounds = Value.GetGlobalBounds();
                    if (bounds.Width > biggestWidth)
                        biggestWidth = bounds.Width;
                }

                Value.DisplayedString = Values[0];

                Right = new Text(">", menu.ItemFont);
                Right.Position = new Vector2f(bounds.Left + biggestWidth + extraWidth, Label.Position.Y);
                Right.CharacterSize = menu.ItemTextSize;
                Right.Color = menu.ItemColor;
            }

            public override Color Color
            {
                get { return Label.Color; }
                set { Label.Color = Value.Color = Left.Color = Right.Color = value; }
            }

            public override Text.Styles Style
            {
                get { return Label.Style; }
                set { Label.Style = Value.Style = value; }
            }

            public override void Draw(RenderTarget target, RenderStates states)
            {
                Label.Draw(target, states);
                Value.Draw(target, states);
                Left.Draw(target, states);
                Right.Draw(target, states);
            }

            public override bool Contains(int x, int y)
            {
                FloatRect bounds = Label.GetGlobalBounds();
                if (y < bounds.Top || y >= bounds.Top + bounds.Height)
                    return false;

                if (x < bounds.Left)
                    return false;

                bounds = Right.GetGlobalBounds();
                if (x >= bounds.Left + bounds.Width)
                    return false;

                return true;
            }

            public void Cycle(bool forward)
            {
                if (forward)
                {
                    selectedIndex++;
                    if (selectedIndex >= Values.Length)
                        selectedIndex = 0;
                }
                else
                {
                    selectedIndex--;
                    if (selectedIndex < 0)
                        selectedIndex = Values.Length - 1;
                }

                Value.DisplayedString = Values[selectedIndex];

                if ( ValueChanged != null )
                    ValueChanged(Value.DisplayedString);
            }

            internal void CheckButtonPress(int x, int y, bool keyDown)
            {
                if (Left.GetGlobalBounds().Contains(x, y))
                    DoButtonPress(false, keyDown);
                else if (Right.GetGlobalBounds().Contains(x, y))
                    DoButtonPress(true, keyDown);
            }

            internal void DoButtonPress(bool right, bool keyDown)
            {
                Text button = right ? Right : Left;
                if (keyDown)
                    button.Color = menu.PressedItemColor;
                else
                {
                    button.Color = menu.HoverItemColor;
                    Cycle(right);
                }
            }
        }

        public class TextEntryItem : Item
        {
            private Text Label, Value, Cursor;
            private int MaxLength;
            private Menu menu;
            public ValueChangedFunction ValueChanged { get; protected set; }

            public TextEntryItem(string label, string value, int maxLength, ValueChangedFunction valueChanged)
            {
                Label = new Text(label, null);
                Value = new Text(value, null);
                Cursor = new Text();
                MaxLength = maxLength;
                ValueChanged = valueChanged;
            }

            protected internal override void AddTo(Menu menu)
            {
                this.menu = menu;

                Label.Font = menu.ItemFont;
                Label.CharacterSize = menu.ItemTextSize;
                Label.Color = menu.ItemColor;
                Label.Position = new Vector2f(menu.ItemXPos + menu.Items.Count * menu.ItemXSpacing, menu.ItemYPos + menu.Items.Count * menu.ItemYSpacing);
                
                Value.Font = menu.ItemFont;
                Value.CharacterSize = menu.ItemTextSize;
                Value.Color = menu.ItemColor;
                Value.Position = new Vector2f(menu.ItemXPos + menu.ValueXOffset + menu.Items.Count * menu.ItemXSpacing, Label.Position.Y);

                Cursor.Font = menu.ItemFont;
                Cursor.CharacterSize = menu.ItemTextSize;
                Cursor.Color = menu.PressedItemColor;
                UpdateCursor();
            }

            public override Color Color
            {
                get { return Label.Color; }
                set { Label.Color = Value.Color = value; }
            }

            public override Text.Styles Style
            {
                get { return Label.Style; }
                set { Label.Style = Value.Style = value; }
            }

            public override void Draw(RenderTarget target, RenderStates states)
            {
                Label.Draw(target, states);
                Value.Draw(target, states);
                if (menu.CurrentItem == this)
                    Cursor.Draw(target, states);
            }

            public override bool Contains(int x, int y)
            {
                FloatRect bounds = Label.GetGlobalBounds();
                if (y < bounds.Top || y >= bounds.Top + bounds.Height)
                    return false;

                if (x < bounds.Left)
                    return false;

                bounds = Value.GetGlobalBounds();
                if (x >= bounds.Left + bounds.Width)
                    return false;

                return true;
            }

            public void Delete()
            {
                if (Value.DisplayedString.Length > 0)
                {
                    Value.DisplayedString = Value.DisplayedString.Substring(0, Value.DisplayedString.Length - 1);
                    UpdateCursor();
                }
            }

            public void Type(string input)
            {
                string val = Value.DisplayedString;

                foreach (char c in input)
                {
                    if (val.Length >= MaxLength)
                        break;

                    if (char.IsLetterOrDigit(c)
                        || ( c == ' ' && val.Length > 1 && val[val.Length-1] != ' ')
                        )
                        val += c;
                }

                Value.DisplayedString = val;
                UpdateCursor();
            }

            private void UpdateCursor()
            {
                FloatRect bounds = Value.GetGlobalBounds();
                Cursor.Position = new Vector2f(bounds.Left + bounds.Width, Label.Position.Y);
                Cursor.DisplayedString = Value.DisplayedString.Length == MaxLength ? "|" : "_";

                if ( ValueChanged != null )
                    ValueChanged(Value.DisplayedString);
            }
        }
    }
}

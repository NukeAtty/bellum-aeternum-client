using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A tab control that can be laid out either horizontally or vertically.
    /// Reimplements the tab layout so it doesn't require changes to the
    /// Rampastring.XNAUI submodule. Set <see cref="Vertical"/> to lay the tabs
    /// out vertically (top-to-bottom); otherwise they are laid out horizontally.
    /// </summary>
    public class ClientTabControl : XNAControl
    {
        public ClientTabControl(WindowManager windowManager) : base(windowManager)
        {
        }

        public event EventHandler SelectedIndexChanged;

        private int selectedTab;
        public int SelectedTab
        {
            get => selectedTab;
            set
            {
                if (selectedTab == value)
                    return;

                selectedTab = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// If set, tabs are laid out vertically (top-to-bottom) instead of
        /// horizontally (left-to-right).
        /// </summary>
        public bool Vertical { get; set; }

        public int FontIndex { get; set; }

        public EnhancedSoundEffect ClickSound { get; set; }

        private Color? textColor;
        public Color TextColor
        {
            get => textColor ?? UISettings.ActiveSettings.AltColor;
            set => textColor = value;
        }

        private Color? textColorDisabled;
        public Color TextColorDisabled
        {
            get => textColorDisabled ?? UISettings.ActiveSettings.DisabledItemColor;
            set => textColorDisabled = value;
        }

        private readonly List<ClientTab> tabs = new List<ClientTab>();

        public void AddTab(string text, int width)
        {
            AddTab(text, width, width + "pxtab", width + "pxtab_c");
        }

        public void AddTab(string text, int width, string textureName, string pressedTextureName)
        {
            if (!AssetLoader.AssetExists(textureName + ".png"))
                textureName = width + "pxtab";

            if (!AssetLoader.AssetExists(textureName + ".png"))
                textureName = width + "pxbtn";

            if (!AssetLoader.AssetExists(pressedTextureName + ".png"))
                pressedTextureName = textureName + "_c";

            AddTab(text, AssetLoader.LoadTexture(textureName + ".png"),
                AssetLoader.LoadTexture(pressedTextureName + ".png"));
        }

        public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture)
        {
            AddTab(text, defaultTexture, pressedTexture, true);
        }

        public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
        {
            var tab = new ClientTab(text, defaultTexture, pressedTexture, selectable);
            tabs.Add(tab);

            Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
            tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
            tab.TextYPosition = Renderer.GetTextYPadding(text, FontIndex, defaultTexture.Height);

            if (Vertical)
            {
                Width = Math.Max(Width, defaultTexture.Width);
                Height += defaultTexture.Height;
            }
            else
            {
                Width += defaultTexture.Width;
                Height = defaultTexture.Height;
            }
        }

        public void MakeSelectable(int index) => tabs[index].Selectable = true;

        public void MakeUnselectable(int index) => tabs[index].Selectable = false;

        public void RemoveTab(int index) => tabs.RemoveAt(index);

        public void RemoveTab(string text)
        {
            int index = tabs.FindIndex(t => t.Text == text);
            if (index >= 0)
                tabs.RemoveAt(index);
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Vertical":
                    Vertical = Conversions.BooleanFromString(value, false);
                    return;
                case "RemapColor":
                case "TextColor":
                    TextColor = AssetLoader.GetColorFromString(value);
                    return;
                case "TextColorDisabled":
                    TextColorDisabled = AssetLoader.GetColorFromString(value);
                    return;
            }

            if (key.StartsWith("RemoveTabIndex", StringComparison.InvariantCulture))
            {
                int index = int.Parse(key.Substring(14), CultureInfo.InvariantCulture);
                if (Conversions.BooleanFromString(value, false))
                    RemoveTab(index);
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            base.OnLeftClick(inputEventArgs);
            inputEventArgs.Handled = true;

            Point p = GetCursorPoint();

            int offset = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                offset += Vertical ? tabs[i].DefaultTexture.Height : tabs[i].DefaultTexture.Width;

                if ((Vertical ? p.Y : p.X) < offset)
                {
                    if (tabs[i].Selectable)
                    {
                        ClickSound?.Play();
                        SelectedTab = i;
                    }

                    return;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            int x = 0;
            int y = 0;

            for (int i = 0; i < tabs.Count; i++)
            {
                ClientTab tab = tabs[i];
                Texture2D texture = i == SelectedTab ? tab.PressedTexture : tab.DefaultTexture;

                DrawTexture(texture, new Point(x, y), RemapColor);
                DrawStringWithShadow(tab.Text, FontIndex,
                    new Vector2(x + tab.TextXPosition, y + tab.TextYPosition),
                    tab.Selectable && Enabled ? TextColor : TextColorDisabled);

                if (Vertical)
                    y += tab.DefaultTexture.Height;
                else
                    x += tab.DefaultTexture.Width;
            }
        }

        private class ClientTab
        {
            public ClientTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
            {
                Text = text;
                DefaultTexture = defaultTexture;
                PressedTexture = pressedTexture;
                Selectable = selectable;
            }

            public string Text { get; }
            public Texture2D DefaultTexture { get; }
            public Texture2D PressedTexture { get; }
            public bool Selectable { get; set; }
            public int TextXPosition { get; set; }
            public int TextYPosition { get; set; }
        }
    }
}

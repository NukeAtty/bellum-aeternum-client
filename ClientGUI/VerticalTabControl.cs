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
    /// A tab control whose tabs are laid out vertically (top-to-bottom).
    /// Reimplements the vertical layout that <see cref="XNATabControl"/> lacks,
    /// so it does not require changes to the Rampastring.XNAUI submodule.
    /// </summary>
    public class VerticalTabControl : XNAControl
    {
        public VerticalTabControl(WindowManager windowManager) : base(windowManager)
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

        private readonly List<VerticalTab> tabs = new List<VerticalTab>();

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
            var tab = new VerticalTab(text, defaultTexture, pressedTexture, selectable);
            tabs.Add(tab);

            Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
            tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
            tab.TextYPosition = Renderer.GetTextYPadding(text, FontIndex, defaultTexture.Height);

            Width = Math.Max(Width, defaultTexture.Width);
            Height += defaultTexture.Height;
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

            int y = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                y += tabs[i].DefaultTexture.Height;

                if (p.Y < y)
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
            int y = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                VerticalTab tab = tabs[i];
                Texture2D texture = i == SelectedTab ? tab.PressedTexture : tab.DefaultTexture;

                DrawTexture(texture, new Point(0, y), RemapColor);
                DrawStringWithShadow(tab.Text, FontIndex,
                    new Vector2(tab.TextXPosition, y + tab.TextYPosition),
                    tab.Selectable && Enabled ? TextColor : TextColorDisabled);

                y += tab.DefaultTexture.Height;
            }
        }

        private class VerticalTab
        {
            public VerticalTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
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

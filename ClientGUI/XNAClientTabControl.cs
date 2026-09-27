using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientTabControl : XNATabControl
    {
        public XNAClientTabControl(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            if (ClickSound == null)
            {
                ClickSound = new EnhancedSoundEffect("Audio/SE/button.wav");
            }
            
            base.Initialize();
        }

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
    }
}

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A modder-defined control that draws a stack of semi-transparent PNG layers,
    /// each rotating around a configurable axis. Layers are configured through the
    /// INI system using <c>LayerN</c> keys.
    /// </summary>
    /// <remarks>
    /// <para>Layer value format (comma separated):</para>
    /// <code>Layer0=texture.png,x,y,speed,axisX,axisY</code>
    /// <list type="bullet">
    /// <item><c>texture.png</c> - texture name to load.</item>
    /// <item><c>x</c>, <c>y</c> - position of the texture's center (relative to the control), at angle 0.</item>
    /// <item><c>speed</c> - rotation speed in degrees per second (positive rotates clockwise).</item>
    /// <item><c>axisX</c>, <c>axisY</c> - rotation pivot offset from the texture's center, in pixels. (0,0) rotates around the center.</item>
    /// </list>
    /// <para>The drawing order relative to sibling controls is controlled by the standard
    /// <c>DrawOrder</c> attribute (the "z-index").</para>
    /// </remarks>
    public class XNARotatingBackground : XNAControl
    {
        public XNARotatingBackground(WindowManager windowManager) : base(windowManager)
        {
            InputEnabled = false;
        }

        private sealed class RotatingLayer
        {
            public Texture2D Texture;
            public float PositionX;
            public float PositionY;
            public float SpeedDegreesPerSecond;
            public float AxisX;
            public float AxisY;
            public float Angle;
        }

        private readonly List<RotatingLayer> layers = new List<RotatingLayer>();

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key.StartsWith("Layer", StringComparison.Ordinal))
            {
                string[] parts = value.Split(',');
                if (parts.Length >= 5)
                {
                    Texture2D texture = AssetLoader.LoadTexture(parts[0].Trim());
                    layers.Add(new RotatingLayer()
                    {
                        Texture = texture,
                        PositionX = Conversions.FloatFromString(parts[1], 0f),
                        PositionY = Conversions.FloatFromString(parts[2], 0f),
                        SpeedDegreesPerSecond = Conversions.FloatFromString(parts[3], 0f),
                        AxisX = Conversions.FloatFromString(parts[4], 0f),
                        AxisY = parts.Length > 5 ? Conversions.FloatFromString(parts[5], 0f) : 0f,
                    });
                }

                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (RotatingLayer layer in layers)
            {
                layer.Angle += layer.SpeedDegreesPerSecond * elapsedSeconds;
                layer.Angle %= 360f;
                if (layer.Angle < 0f)
                    layer.Angle += 360f;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                RotatingLayer layer = layers[i];

                float originX = layer.Texture.Width / 2f + layer.AxisX;
                float originY = layer.Texture.Height / 2f + layer.AxisY;

                var origin = new Vector2(originX, originY);
                var position = new Vector2(layer.PositionX + layer.AxisX, layer.PositionY + layer.AxisY);

                DrawTexture(layer.Texture, position, MathHelper.ToRadians(layer.Angle),
                    origin, Vector2.One, Color.White, i * 0.001f);
            }

            base.Draw(gameTime);
        }
    }
}

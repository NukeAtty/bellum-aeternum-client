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
    /// A modder-defined control that draws a stack of semi-transparent PNG layers,
    /// each of which can independently move, rotate around a configurable axis and
    /// be given an explicit z-order. Layers are configured through the INI system.
    /// </summary>
    /// <remarks>
    /// <para>Layers are declared with <c>LayerN</c> keys where <c>N</c> is any
    /// non-negative integer. Each layer supports two value formats:</para>
    /// <list type="bullet">
    /// <item>Positional shorthand (backwards compatible):</item>
    /// <code>Layer0=texture.png,x,y,speed,axisX,axisY[,zIndex]</code>
    /// <item>Named properties (recommended):</item>
    /// <code>
    /// Layer0=texture.png
    /// Layer0.Position=640,360
    /// Layer0.Velocity=10,0
    /// Layer0.RotationSpeed=15
    /// Layer0.Axis=0,0
    /// Layer0.ZIndex=1.5
    /// Layer0.Alpha=0.8
    /// Layer0.Scale=1,1
    /// Layer0.Loop=true
    /// </code>
    /// </list>
    /// <para>Named property reference:</para>
    /// <list type="bullet">
    /// <item><c>Position=x,y</c> - position of the texture's center (relative to the control).</item>
    /// <item><c>Velocity=vx,vy</c> - movement speed in pixels per second.</item>
    /// <item><c>RotationSpeed=speed</c> - rotation speed in degrees per second (positive = clockwise).</item>
    /// <item><c>Axis=axisX,axisY</c> - rotation pivot offset from the texture's center, in pixels. (0,0) rotates around the center.</item>
    /// <item><c>ZIndex=z</c> - explicit draw order; higher values are drawn on top. Defaults to the layer index.</item>
    /// <item><c>Alpha=a</c> - opacity multiplier from 0 to 1.</item>
    /// <item><c>Scale=sx,sy</c> (or <c>Scale=s</c>) - scale multiplier, defaults to 1.</item>
    /// <item><c>Loop=true|false</c> - if enabled, a moving layer wraps around the control's bounds for seamless parallax loops. Defaults to false.</item>
    /// </list>
    /// <para>Layers are sorted by their <c>ZIndex</c> before drawing, so z-ordering is
    /// independent of the order in which the <c>LayerN</c> keys appear in the INI file.
    /// The whole control's z-order relative to sibling controls is controlled by the
    /// standard <c>DrawOrder</c> attribute.</para>
    /// </remarks>
    public class XNAAnimatedBackground : XNAControl
    {
        public XNAAnimatedBackground(WindowManager windowManager) : base(windowManager)
        {
            InputEnabled = false;
        }

        private sealed class BackgroundLayer
        {
            public Texture2D Texture;
            public float PositionX;
            public float PositionY;
            public float VelocityX;
            public float VelocityY;
            public float RotationSpeedDegreesPerSecond;
            public float AxisX;
            public float AxisY;
            public float Angle;
            public float ZIndex;
            public float Alpha = 1f;
            public Vector2 Scale = Vector2.One;
            public bool Loop;
        }

        private readonly Dictionary<int, BackgroundLayer> layersByIndex = new Dictionary<int, BackgroundLayer>();
        private readonly List<BackgroundLayer> sortedLayers = new List<BackgroundLayer>();
        private bool layersNeedSorting;

        private BackgroundLayer GetOrCreateLayer(int index)
        {
            if (!layersByIndex.TryGetValue(index, out BackgroundLayer layer))
            {
                layer = new BackgroundLayer { ZIndex = index };
                layersByIndex.Add(index, layer);
                layersNeedSorting = true;
            }

            return layer;
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key.StartsWith("Layer", StringComparison.OrdinalIgnoreCase))
            {
                string remainder = key.Substring("Layer".Length);
                int dotIndex = remainder.IndexOf('.');

                string indexString = dotIndex >= 0 ? remainder.Substring(0, dotIndex) : remainder;
                string property = dotIndex >= 0 ? remainder.Substring(dotIndex + 1) : null;

                if (!int.TryParse(indexString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                {
                    base.ParseControlINIAttribute(iniFile, key, value);
                    return;
                }

                BackgroundLayer layer = GetOrCreateLayer(index);

                if (property == null)
                    ParseLayerDefinition(layer, value);
                else
                    ParseLayerProperty(layer, property, value);

                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        private void ParseLayerDefinition(BackgroundLayer layer, string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length >= 5)
            {
                layer.Texture = AssetLoader.LoadTexture(parts[0].Trim());
                layer.PositionX = Conversions.FloatFromString(parts[1], 0f);
                layer.PositionY = Conversions.FloatFromString(parts[2], 0f);
                layer.RotationSpeedDegreesPerSecond = Conversions.FloatFromString(parts[3], 0f);
                layer.AxisX = Conversions.FloatFromString(parts[4], 0f);
                layer.AxisY = parts.Length > 5 ? Conversions.FloatFromString(parts[5], 0f) : 0f;

                if (parts.Length > 6)
                    layer.ZIndex = Conversions.FloatFromString(parts[6], layer.ZIndex);
            }
            else
            {
                layer.Texture = AssetLoader.LoadTexture(value.Trim());
            }
        }

        private void ParseLayerProperty(BackgroundLayer layer, string property, string value)
        {
            switch (property)
            {
                case "Position":
                    string[] position = value.Split(',');
                    if (position.Length >= 2)
                    {
                        layer.PositionX = Conversions.FloatFromString(position[0], layer.PositionX);
                        layer.PositionY = Conversions.FloatFromString(position[1], layer.PositionY);
                    }

                    break;

                case "Velocity":
                    string[] velocity = value.Split(',');
                    if (velocity.Length >= 2)
                    {
                        layer.VelocityX = Conversions.FloatFromString(velocity[0], layer.VelocityX);
                        layer.VelocityY = Conversions.FloatFromString(velocity[1], layer.VelocityY);
                    }

                    break;

                case "RotationSpeed":
                    layer.RotationSpeedDegreesPerSecond = Conversions.FloatFromString(value, layer.RotationSpeedDegreesPerSecond);
                    break;

                case "Axis":
                    string[] axis = value.Split(',');
                    if (axis.Length >= 2)
                    {
                        layer.AxisX = Conversions.FloatFromString(axis[0], layer.AxisX);
                        layer.AxisY = Conversions.FloatFromString(axis[1], layer.AxisY);
                    }

                    break;

                case "ZIndex":
                    layer.ZIndex = Conversions.FloatFromString(value, layer.ZIndex);
                    layersNeedSorting = true;
                    break;

                case "Alpha":
                    layer.Alpha = MathHelper.Clamp(Conversions.FloatFromString(value, layer.Alpha), 0f, 1f);
                    break;

                case "Scale":
                    string[] scale = value.Split(',');
                    if (scale.Length >= 2)
                    {
                        layer.Scale = new Vector2(
                            Conversions.FloatFromString(scale[0], layer.Scale.X),
                            Conversions.FloatFromString(scale[1], layer.Scale.Y));
                    }
                    else
                    {
                        float uniformScale = Conversions.FloatFromString(value, layer.Scale.X);
                        layer.Scale = new Vector2(uniformScale, uniformScale);
                    }

                    break;

                case "Loop":
                    layer.Loop = Conversions.BooleanFromString(value, layer.Loop);
                    break;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (BackgroundLayer layer in layersByIndex.Values)
            {
                layer.Angle += layer.RotationSpeedDegreesPerSecond * elapsedSeconds;
                layer.Angle %= 360f;
                if (layer.Angle < 0f)
                    layer.Angle += 360f;

                if (layer.VelocityX != 0f || layer.VelocityY != 0f)
                {
                    layer.PositionX += layer.VelocityX * elapsedSeconds;
                    layer.PositionY += layer.VelocityY * elapsedSeconds;

                    if (layer.Loop)
                    {
                        layer.PositionX = Wrap(layer.PositionX, 0f, Width);
                        layer.PositionY = Wrap(layer.PositionY, 0f, Height);
                    }
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (layersNeedSorting)
            {
                sortedLayers.Clear();
                sortedLayers.AddRange(layersByIndex.Values);
                sortedLayers.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));
                layersNeedSorting = false;
            }

            for (int i = 0; i < sortedLayers.Count; i++)
            {
                BackgroundLayer layer = sortedLayers[i];

                if (layer.Texture == null)
                    continue;

                float originX = layer.Texture.Width / 2f + layer.AxisX;
                float originY = layer.Texture.Height / 2f + layer.AxisY;

                var origin = new Vector2(originX, originY);
                var position = new Vector2(layer.PositionX + layer.AxisX, layer.PositionY + layer.AxisY);

                // layerDepth is only used to avoid clipping (must stay within [0,1]);
                // the actual z-order is determined by the sorted loop order above.
                DrawTexture(layer.Texture, position, MathHelper.ToRadians(layer.Angle),
                    origin, layer.Scale, Color.White * layer.Alpha, i * 0.001f);
            }

            base.Draw(gameTime);
        }

        private static float Wrap(float value, float min, float max)
        {
            if (max <= min)
                return value;

            float range = max - min;
            value = (value - min) % range;
            if (value < 0f)
                value += range;

            return value + min;
        }
    }
}

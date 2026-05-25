using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.Text
{
    public class FontTextureDimensions
    {
        public readonly Vector2i sdfBounds;
        public readonly Vector2i colBounds;
        public readonly float sdfRadius;

        public FontTextureDimensions(Vector2i sdfBounds, Vector2i colBounds, float sdfRadius)
        {
            this.sdfBounds = sdfBounds;
            this.colBounds = colBounds;
            this.sdfRadius = sdfRadius;
        }
    }
}

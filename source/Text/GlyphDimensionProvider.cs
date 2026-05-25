using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.Text
{
    public interface GlyphDimensionProvider
    {
        GlyphDimensions GetGlyph(UnicodeChars c);
        FontTextureDimensions textureDimensions { get; }
    }
}

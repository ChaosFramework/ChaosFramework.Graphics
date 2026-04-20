using ChaosUtil.Primitives;
using ChaosFramework.Collections.Immutable;

namespace ChaosFramework.Graphics.Text
{
    public sealed class TextGeometryDescription
    {
        public static readonly ImmutableArray<UnicodeChars> POSSIBLE_LINE_BREAKS = new UnicodeChars[] { (UnicodeChars)' ' };
        public static readonly ImmutableArray<UnicodeChars> LINE_INDENTATION_CHARS
            = new UnicodeChars[] { (UnicodeChars)' ', (UnicodeChars)'\t' };

        public readonly string text;
        public readonly LayoutInfo layout;
        public readonly FontTextureDimensions dimensions;
        public readonly GlyphDimensionProvider font;

        public TextGeometryDescription(GlyphDimensionProvider font, string text, LayoutInfo layout)
        {
            this.font = font;
            this.dimensions = font.textureDimensions;
            this.text = text ?? string.Empty;
            this.layout = layout;
        }

        public override int GetHashCode()
            => HashCode.Combine(text, layout);

        public override bool Equals(object obj)
            => Equals(obj as TextGeometryDescription);

        public bool Equals(TextGeometryDescription args)
            => args != null
            && args.text == text
            && layout.Equals(args.layout);

        public static bool operator ==(TextGeometryDescription a, TextGeometryDescription b)
            => a?.Equals(b) ?? ReferenceEquals(b, null);

        public static bool operator !=(TextGeometryDescription a, TextGeometryDescription b)
            => !(a == b);
    }
}

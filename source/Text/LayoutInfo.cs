using ChaosFramework.Collections.Immutable;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;
using System.Linq;

namespace ChaosFramework.Graphics.Text
{
    public class LayoutInfo
    {
        public static readonly LayoutInfo TOP_LEFT = new LayoutInfo(Align.TopLeft);
        public static readonly LayoutInfo TOP= new LayoutInfo(Align.Top);
        public static readonly LayoutInfo TOP_RIGHT = new LayoutInfo(Align.TopRight);
        public static readonly LayoutInfo RIGHT = new LayoutInfo(Align.Right);
        public static readonly LayoutInfo BOTTOM_RIGHT = new LayoutInfo(Align.BottomRight);
        public static readonly LayoutInfo BOTTOM = new LayoutInfo(Align.Bottom);
        public static readonly LayoutInfo BOTTOM_LEFT = new LayoutInfo(Align.BottomLeft);
        public static readonly LayoutInfo LEFT = new LayoutInfo(Align.Left);
        public static readonly LayoutInfo CENTER = new LayoutInfo(Align.Center);

        public readonly Align align = Align.TopLeft;
        public readonly bool monospace = false;
        public readonly bool enableEscapeSequences = false;
        public readonly bool ignoreControlCharacters = false;
        public readonly Vector2f letterDistance = 1.0f;
        public readonly ImmutableArray<float> tabStops = new[] { 4.0f };
        public readonly float maxLineWidth = float.PositiveInfinity;

        public LayoutInfo(
            Align align = Align.TopLeft,
            bool monospace = false,
            bool enableEscapeSequences = false,
            bool ignoreControlCharacters = false,
            ImmutableArray<float> tabStops = null,
            float maxLineWidth = float.PositiveInfinity
            )
            : this(align, monospace,enableEscapeSequences, ignoreControlCharacters, 1.0f, tabStops, maxLineWidth)
        { }

        public LayoutInfo(
            Align align,
            bool monospace,
            bool enableEscapeSequences,
            bool ignoreControlCharacters,
            Vector2f letterDistance,
            ImmutableArray<float> tabStops = null,
            float maxLineWidth = float.PositiveInfinity
            )
        {
            this.align = align;
            this.monospace = monospace;
            this.enableEscapeSequences = enableEscapeSequences;
            this.ignoreControlCharacters = ignoreControlCharacters;
            this.letterDistance = letterDistance;
            this.tabStops = tabStops ?? new[] { 4.0f };
            this.maxLineWidth = maxLineWidth;
        }

        public override bool Equals(object obj)
            => Equals(obj as LayoutInfo);

        public bool Equals(LayoutInfo info)
            => ReferenceEquals(this, info)
            || (!ReferenceEquals(info, null)
             && info.align == align
             && info.monospace == monospace
             && info.enableEscapeSequences == enableEscapeSequences
             && info.ignoreControlCharacters == ignoreControlCharacters
             && info.letterDistance == letterDistance
             && info.maxLineWidth == maxLineWidth
             && info.tabStops.SequenceEqual(tabStops));

        public static bool operator ==(LayoutInfo a, LayoutInfo b)
            => a?.Equals(b) ?? ReferenceEquals(b, null);

        public static bool operator !=(LayoutInfo a, LayoutInfo b)
            => !(a == b);

        public override int GetHashCode()
            => HashCode.Combine(
                align,
                monospace,
                enableEscapeSequences,
                ignoreControlCharacters,
                letterDistance,
                maxLineWidth,
                HashCode.Combine(tabStops)
                );
    }
}

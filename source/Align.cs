namespace ChaosFramework.Graphics
{
    public enum Align : byte
    {
        Center = 0,
        Left = 1,
        Right = 2,
        LeftRight = Left | Right,
        Top = 4,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        TopLeftRight = Top | Left | Right,
        Bottom = 8,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right,
        BottomLeftRight = Bottom | Left | Right,
        BottomTop = Bottom | Top,
        BottomTopLeft = Bottom | Top | Left,
        BottomTopRight = Bottom | Top | Right,
        BottomTopLeftRight = Bottom | Top | Left | Right
    }

    public static class AlignExt
    {
        public static bool IsVerticallyCentered(this Align align)
            => (align & Align.BottomTop) == 0;

        public static bool IsHorizontallyCentered(this Align align)
            => (align & Align.LeftRight) == 0;

        public static Align Horizontal(this Align align)
            => align & Align.LeftRight;

        public static Align Vertical(this Align align)
            => align & Align.BottomTop;
    }
}

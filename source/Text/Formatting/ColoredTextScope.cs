using ChaosFramework.Graphics.Colors;
using ChaosFramework.Math.Vectors;
using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.Text.Formatting
{
    public class ColoredTextScope : Scope
    {
        internal const char ESCAPE_CODE_COL = 'c';
        public static readonly string COLOR_CODE = ESCAPE_CODE.ToString() + ESCAPE_CODE_COL;
        public static readonly string RESET_COLOR_CODE = COLOR_CODE + ESCAPE_CODE;

        public static string GetColorCode(Rgba color)
            => COLOR_CODE
            + ((byte)(color.r * 255)).ToString("X2")
            + ((byte)(color.g * 255)).ToString("X2")
            + ((byte)(color.b * 255)).ToString("X2")
            + ((byte)(color.a * 255)).ToString("X2")
            + Scope.ESCAPE_CODE;

        public static string GetColorCode(string color)
        {
            if (color.Length != 8 || color.Contains(ESCAPE_CODE.ToString()))
                throw new System.ArgumentException("Invalid color string!", nameof(color));

            return COLOR_CODE + color + ESCAPE_CODE;
        }

        internal static bool TryParseColor(UnicodeChars[] colChars, int offset, out Rgba color)
        {
            color = Rgba.TRANSPARENT_BLACK;
            Vector4f tmp = 0;
            int channel = 0;
            for (int i = offset; i < offset + 8; i += 2)
            {
                UnicodeChars hi = colChars[i];
                UnicodeChars lo = colChars[i + 1];
                if (!(hi.Is16BitChar() && lo.Is16BitChar()))
                    return false;
                byte loB, hiB;
                if (!(hi.TryParseHexChar(out hiB) && lo.TryParseHexChar(out loB)))
                    return false;
                tmp[channel++] = (loB | (hiB << 4)) / 255.0f;
            }
            color = new Rgba(tmp);
            return true;
        }

        public ColoredTextScope(System.Text.StringBuilder bldr, string color)
            : base(bldr.Append(GetColorCode(color)))
        { }

        public ColoredTextScope(System.Text.StringBuilder bldr, Rgba color)
            : base(bldr.Append(GetColorCode(color)))
        { }

        protected override void Reset()
            => bldr.Append(RESET_COLOR_CODE);
    }
}

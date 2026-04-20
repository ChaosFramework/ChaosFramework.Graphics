using ChaosFramework.IO;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using Globalization = System.Globalization;
using ChaosUtil.Primitives;
using Color = System.Drawing.Color;

namespace ChaosFramework.Graphics.Colors
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Rgba
    {
        public static readonly Rgba TRANSPARENT_BLACK = new Rgba(Rgb.BLACK, 0);
        public static readonly Rgba OPAQUE_BLACK = new Rgba(Rgb.BLACK, 1);
        public static readonly Rgba OPAQUE_WHITE = new Rgba(Rgb.WHITE, 1);
        public static readonly Rgba NAN = new Rgba(float.NaN);

        const float INV_255 = 1.0f / 0xFF;
        const Globalization.NumberStyles HEX = Globalization.NumberStyles.AllowHexSpecifier;
        static readonly Globalization.CultureInfo INVARIANT_CULTURE = Globalization.CultureInfo.InvariantCulture;

        public static Rgba FromColor(Color color)
            => new Rgba(color.R * INV_255,
                        color.G * INV_255,
                        color.B * INV_255,
                        color.A * INV_255
                        );

        public static Rgba FromArgb(int argb) => FromColor(Color.FromArgb(argb));

        public static Rgba FromHsva(Hsva hsva) => hsva.ToRgba();

        [ChaosIO.RegisterType]
        static void _() => IO.ChaosIO.AddType(Read, Write);
        static Rgba Read(System.IO.BinaryReader rd) => new Rgba(rd.Read<Rgb>(), rd.Read<float>());
        static void Write(System.IO.BinaryWriter wr, Rgba c) { wr.WriteAs(c.rgb); wr.WriteAs(c.a); }

        public static bool TryParse(string str, out Rgba rgba)
        {
            Vector4f vec4;
            if (Vector4f.TryParse(str, out vec4))
            {
                rgba = new Rgba(vec4);
                return true;
            }

            if (TryParseHex(str, out rgba))
                return true;

            return false;
        }

        public static Rgba ParseHex(string hex)
        {
            byte r, g, b, a;
            a = 255;
            if (hex.Length != 6 && hex.Length != 8)
                throw new System.ArgumentException("Hex string must be either 6 or 8 characters long.");

            if (!(byte.TryParse(hex.Substring(0, 2), HEX, INVARIANT_CULTURE, out r)
                && byte.TryParse(hex.Substring(2, 2), HEX, INVARIANT_CULTURE, out g)
                && byte.TryParse(hex.Substring(4, 2), HEX, INVARIANT_CULTURE, out b)
                && (hex.Length == 6 || byte.TryParse(hex.Substring(6, 2), HEX, INVARIANT_CULTURE, out a))))
                throw new System.ArgumentException($"Invalid color code {hex}.");

            return new Rgba(r * INV_255, g * INV_255, b * INV_255, a * INV_255);
        }

        public static bool TryParseHex(string hex, out Rgba result)
        {
            result = TRANSPARENT_BLACK;

            byte r, g, b, a;
            a = 255;
            if (hex.Length != 6 && hex.Length != 8)
                return false;

            if (!(byte.TryParse(hex.Substring(0, 2), HEX, INVARIANT_CULTURE, out r)
               && byte.TryParse(hex.Substring(2, 2), HEX, INVARIANT_CULTURE, out g)
               && byte.TryParse(hex.Substring(4, 2), HEX, INVARIANT_CULTURE, out b)
               && (hex.Length == 6 || byte.TryParse(hex.Substring(6, 2), HEX, INVARIANT_CULTURE, out a))))
                return false;

            result = new Rgba(new Vector4f(r, g, b, a) * INV_255);
            return true;
        }

        public Rgb rgb;
        public float a;

        public float r { get { return rgb.r; } set { rgb.r = value; } }
        public float g { get { return rgb.g; } set { rgb.g = value; } }
        public float b { get { return rgb.b; } set { rgb.b = value; } }

        public Rgba(float r, float g, float b, float a = 1) : this(new Vector4f(r, g, b, a)) { }
        public Rgba(Vector3f rgb, float a = 1) : this(new Rgb(rgb), a) { }
        public Rgba(Rgb rgb, float a) { this.rgb = rgb; this.a = a; }
        public Rgba(Vector4f rgba) { rgb = new Rgb(rgba.xyz); a = rgba.w; }

        public bool IsNaN() => rgb.IsNaN() || float.IsNaN(a);

        public Color ToColor()
            => Color.FromArgb((byte)(Clamp(0, 255, a * 255)),
                              (byte)(Clamp(0, 255, r * 255)),
                              (byte)(Clamp(0, 255, g * 255)),
                              (byte)(Clamp(0, 255, b * 255))
                              );

        public int ToArgb() => ToColor().ToArgb();
        public Hsva ToHsva() => Hsva.FromRgba(this);
        public Vector4f ToVec() => new Vector4f(rgb.value, a);

        public static Rgba operator +(Rgba c1, Rgba c2) => new Rgba(c1.rgb + c2.rgb, c1.a + c2.a);
        public static Rgba operator -(Rgba c1, Rgba c2) => new Rgba(c1.rgb - c2.rgb, c1.a - c2.a);
        public static Rgba operator *(Rgba c, float f) => new Rgba(c.rgb * f, c.a * f);
        public static Rgba operator *(float f, Rgba c) => new Rgba(c.rgb * f, c.a * f);
        public static Rgba operator /(Rgba c, float f) => new Rgba(c.rgb / f, c.a / f);

        public static bool operator ==(Rgba c1, Rgba c2) => c1.rgb == c2.rgb && c1.a == c2.a;
        public static bool operator !=(Rgba c1, Rgba c2) => c1.rgb != c2.rgb || c1.a != c2.a;

        public override bool Equals(object obj)
            => obj is Rgba ? Equals((Rgba)obj) : false;

        public bool Equals(Rgba rgba)
            => this == rgba;

        public override int GetHashCode()
            => HashCode.Combine(rgb, a);
    }
}

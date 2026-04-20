using ChaosFramework.IO;
using ChaosFramework.Math.Vectors;
using Globalization = System.Globalization;
using ChaosUtil.Primitives;

namespace ChaosFramework.Graphics.Colors
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Rgb
    {
        public static readonly Rgb BLACK = new Rgb(0);
        public static readonly Rgb WHITE = new Rgb(1);
        public static readonly Rgb NAN = new Rgb(float.NaN);

        const float INV_255 = 1.0f / 0xFF;
        const Globalization.NumberStyles HEX = Globalization.NumberStyles.AllowHexSpecifier;
        static readonly Globalization.CultureInfo INVARIANT_CULTURE = Globalization.CultureInfo.InvariantCulture;

        [ChaosIO.RegisterType]
        static void _() => ChaosIO.AddType(Read, Write);
        static Rgb Read(System.IO.BinaryReader rd) => new Rgb(rd.Read<Vector3f>());
        static void Write(System.IO.BinaryWriter wr, Rgb c) => wr.WriteAs(c.value);

        public static bool TryParse(string str, out Rgb rgba)
        {
            Vector3f vec3;
            if (Vector3f.TryParse(str, out vec3))
            {
                rgba = new Rgb(vec3);
                return true;
            }

            if (TryParseHex(str, out rgba))
                return true;

            return false;
        }


        public static Rgb ParseHex(string hex)
        {
            byte r, g, b;
            if (hex.Length != 6)
                throw new System.ArgumentException("Hex string must be 6 characters long.");

            if (!(byte.TryParse(hex.Substring(0, 2), HEX, INVARIANT_CULTURE, out r)
                && byte.TryParse(hex.Substring(2, 2), HEX, INVARIANT_CULTURE, out g)
                && byte.TryParse(hex.Substring(4, 2), HEX, INVARIANT_CULTURE, out b)))
                throw new System.ArgumentException($"Invalid color code {hex}.");

            return new Rgb(r * INV_255, g * INV_255, b * INV_255);
        }

        public static bool TryParseHex(string hex, out Rgb result)
        {
            result = BLACK;

            byte r, g, b;
            if (hex.Length != 6)
                return false;

            if (!(byte.TryParse(hex.Substring(0, 2), HEX, INVARIANT_CULTURE, out r)
               && byte.TryParse(hex.Substring(2, 2), HEX, INVARIANT_CULTURE, out g)
               && byte.TryParse(hex.Substring(4, 2), HEX, INVARIANT_CULTURE, out b)
               ))
                return false;

            result = new Rgb(new Vector3f(r, g, b) * INV_255);
            return true;
        }

        public Vector3f value;

        public float r { get { return value.x; } set { this.value.x = value; } }
        public float g { get { return value.y; } set { this.value.y = value; } }
        public float b { get { return value.z; } set { this.value.z = value; } }

        public Rgb(float r, float g, float b) : this(new Vector3f(r, g, b)) { }
        public Rgb(Vector3f rgb) { value = rgb; }

        public bool IsNaN() => value.IsNaN();

        public Vector3f ToVec() => new Vector3f(r, g, b);

        public static Rgb operator +(Rgb c1, Rgb c2) => new Rgb(c1.value + c2.value);
        public static Rgb operator -(Rgb c1, Rgb c2) => new Rgb(c1.value - c2.value);
        public static Rgb operator *(Rgb c, float f) => new Rgb(c.value * f);
        public static Rgb operator *(float f, Rgb c) => new Rgb(c.value * f);
        public static Rgb operator /(Rgb c, float f) => new Rgb(c.value / f);

        public static bool operator ==(Rgb c1, Rgb c2) => c1.value == c2.value;
        public static bool operator !=(Rgb c1, Rgb c2) => c1.value != c2.value;

        public override bool Equals(object obj)
            => obj is Rgb ? Equals((Rgb)obj) : false;

        public bool Equals(Rgb rgb)
            => this == rgb;

        public override int GetHashCode()
            => HashCode.Combine(r, g, b);
    }
}

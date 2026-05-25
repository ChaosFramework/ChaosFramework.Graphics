using ChaosUtil.Primitives;
using ChaosFramework.Math.Vectors;
using static ChaosFramework.Math.Clamping;
using static ChaosFramework.Math.Modulus;
using Globalization = System.Globalization;

namespace ChaosFramework.Graphics.Colors
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct Hsva
    {
        public static Hsva FromArgb(int argb) => FromRgba(Rgba.FromArgb(argb));

        public static Hsva FromRgba(Rgba rgba) => FromRgba(rgba.r, rgba.g, rgba.b, rgba.a);

        public static Hsva FromRgba(float r, float g, float b, float a = 1)
        {
            r = Clamp(0, 1, r);
            g = Clamp(0, 1, g);
            b = Clamp(0, 1, b);
            float max = Max(r, g, b);
            float min = Min(r, g, b);
            float range = max - min;

            float s = 0;
            if (max != 0)
                s = range / max;

            float h = 0;
            const float ONE_SIXTH = 1.0f / 6;
            if (max != min)
            {
                if (max == r)
                    h = ONE_SIXTH * (0 + (g - b) / range);
                else if (max == g)
                    h = ONE_SIXTH * (2 + (b - r) / range);
                else if (max == b)
                    h = ONE_SIXTH * (4 + (r - g) / range);
            }

            while (h < 0)
                h += 1;

            return new Hsva(h, s, max, a);
        }

        public Vector4f hsva;

        public float h { get { return hsva.x; } set { hsva.x = value; } }
        public float s { get { return hsva.y; } set { hsva.y = value; } }
        public float v { get { return hsva.z; } set { hsva.z = value; } }
        public float a { get { return hsva.w; } set { hsva.w = value; } }

        public Hsva(float h, float s, float v, float a = 1) : this(new Vector4f(h, s, v, a)) { }
        public Hsva(Vector3f hsv, float a = 1) : this(new Vector4f(hsv, a)) { }
        public Hsva(Vector4f hsva) { this.hsva = hsva; }

        public Rgba ToRgba()
        {
            h = Mod(h, 1);
            int hi = (int)(h * 6);
            float f = h * 6 - hi;
            float p = v * (1 - s);
            float q = v * (1 - s * f);
            float t = v * (1 - s * (1 - f));
            switch (hi)
            {
                case 0: return new Rgba(v, t, p, a);
                case 1: return new Rgba(q, v, p, a);
                case 2: return new Rgba(p, v, t, a);
                case 3: return new Rgba(p, q, v, a);
                case 4: return new Rgba(t, p, v, a);
                default: return new Rgba(v, p, q, a);
            }
        }

        public int ToArgb() => ToRgba().ToArgb();

        public static bool operator ==(Hsva a, Hsva b) => a.hsva == b.hsva;
        public static bool operator !=(Hsva a, Hsva b) => a.hsva != b.hsva;

        public override bool Equals(object obj)
            => obj is Hsva ? Equals((Hsva)obj) : false;

        public bool Equals(Hsva hsva)
            => this == hsva;

        public override int GetHashCode()
            => HashCode.Combine(h, s, v, a);
    }
}

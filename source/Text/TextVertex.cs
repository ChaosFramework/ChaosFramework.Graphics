using ChaosFramework.Math.Vectors;
using System.Runtime.InteropServices;

namespace ChaosFramework.Graphics.Text
{
    using Colors;

    [StructLayout(LayoutKind.Sequential)]
    public struct TextVertex
    {
        public static readonly int SIZE_IN_BYTES = Marshal.SizeOf(typeof(TextVertex));

        public Vector4f position;
        public Vector4f texCoord;
        public Rgba color;
    }
}

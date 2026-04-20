using System.Drawing;

namespace ChaosFramework.Graphics.Colors
{
    public static class Bgr
    {
        public static int Color2BGRInt(Color color) => (color.B << 0x10) | (color.G << 0x8) | color.R;

        public static int[] Color2BGRInt(params Color[] colors)
        {
            int[] arr = new int[colors.Length];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = Color2BGRInt(colors[i]);
            return arr;
        }
    }
}

using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using System;

namespace ChaosFramework.Graphics.Imaging
{
    public static class ImageExt
    {
        public delegate bool ValidatePixel<Color>(Color pixel);

        public static Bounds2i TrimBitmap<Color>(
            this Image<Color> src,
            ValidatePixel<Color> isColorValid,
            bool left = true,
            bool right = true,
            bool bottom = true,
            bool top = true
            )
            where Color : struct
        {
            // TODO: create and use Bounds2ui
            Bounds2i rect = new Bounds2i(0, 0, (int)src.width - 1, (int)src.height - 1);

            if (left)
                for (; rect.left < rect.right; rect.low.x++)
                    for (uint y = (uint)rect.bottom; y < rect.top; y++)
                        if (isColorValid(src[(uint)rect.left, y]))
                            goto found_left;
                        found_left:

            if (right)
                for (; rect.left < rect.right; rect.high.x--)
                    for (uint y = (uint)rect.bottom; y < rect.top; y++)
                        if (isColorValid(src[(uint)rect.right, y]))
                            goto found_right;
                        found_right:

            if (bottom)
                for (; rect.bottom < rect.top; rect.low.y++)
                    for (uint x = (uint)rect.left; x < rect.right; x++)
                        if (isColorValid(src[x, (uint)rect.bottom]))
                            goto found_bottom;
                        found_bottom:

            if (top)
                for (; rect.bottom < rect.top; rect.high.y--)
                    for (uint x = (uint)rect.left; x < rect.right; x++)
                        if (isColorValid(src[x, (uint)rect.top]))
                            goto found_top;
                        found_top:

            return rect;
        }

        public static TImage CutBitmap<TImage, Color>(this TImage src, Bounds2i rect)
            where TImage : Image<Color>
            where Color : struct
        {
            if (src.width < rect.high.x || src.height < rect.high.y)
                throw new ArgumentException("The provided rectangle exceeds the image bounds.", nameof(rect));

            // TODO: create and use Bounds2ui
            TImage dest = (TImage)TImage.CreateEmpty((uint)rect.width, (uint)rect.height);
            for (uint y = 0; y < rect.height; y++)
                for (uint x = 0; x < rect.width; x++)
                    dest[x, y] = src[x + (uint)rect.low.x, y + (uint)rect.low.y];

            return dest;
        }

        public static void CopyPixels<Color>(this Image<Color> src, Image<Color> dest, Bounds2i srcRect, Vector2i position)
            where Color : struct
        {
            // TODO: create and use Bounds2ui
            if (position.x + srcRect.width - 1 >= dest.width
             || position.y + srcRect.height - 1 >= dest.height)
                throw new ArgumentException("dest exceeds source boundaries");

            for (uint y = 0; y < srcRect.height; y++)
                for (uint x = 0; x < srcRect.width; x++)
                    dest[x, y] = src[x + (uint)position.x, y + (uint)position.y];
        }
    }
}

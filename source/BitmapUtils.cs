using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using ChaosFramework.IO.Streams;
using ChaosUtil.Primitives;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using static ChaosFramework.Math.Clamping;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics
{
    public static class BitmapUtils
    {
        class LockedBits : IDisposable {
            public readonly BitmapData data;
            readonly Bitmap bmp;

            public IntPtr Scan0 => data.Scan0;
            public int Stride => data.Stride;

            public LockedBits(Bitmap bmp, Rectangle rect, ImageLockMode lockMode, PixelFormat pixelFormat)
            {
                this.bmp = bmp;
                data = bmp.LockBits(rect, lockMode, pixelFormat);
            }

            void IDisposable.Dispose()
                => bmp.UnlockBits(data);
        }

        public enum ImageFormats
        {
            Png
        }

        public delegate bool ValidatePixel(byte[] pixel);

        static readonly SysCol.Dictionary<ImageFormats, ImageFormat> formatMap = new SysCol.Dictionary<ImageFormats, ImageFormat>();
        static readonly SysCol.Dictionary<PixelFormat, int> drawingPixelFormatSizes = new SysCol.Dictionary<PixelFormat, int>();

        static BitmapUtils()
        {
            drawingPixelFormatSizes[PixelFormat.Format1bppIndexed] = 1;
            drawingPixelFormatSizes[PixelFormat.Format4bppIndexed] = 4;
            drawingPixelFormatSizes[PixelFormat.Format8bppIndexed] = 8;

            drawingPixelFormatSizes[PixelFormat.Format16bppArgb1555] =
                 drawingPixelFormatSizes[PixelFormat.Format16bppGrayScale] =
                 drawingPixelFormatSizes[PixelFormat.Format16bppRgb555] =
                 drawingPixelFormatSizes[PixelFormat.Format16bppRgb565] = 16;

            drawingPixelFormatSizes[PixelFormat.Format24bppRgb] = 24;

            drawingPixelFormatSizes[PixelFormat.Canonical] =
                drawingPixelFormatSizes[PixelFormat.Format32bppArgb] =
                drawingPixelFormatSizes[PixelFormat.Format32bppPArgb] =
                drawingPixelFormatSizes[PixelFormat.Format32bppRgb] = 32;

            drawingPixelFormatSizes[PixelFormat.Format48bppRgb] = 48;

            drawingPixelFormatSizes[PixelFormat.Format64bppArgb] =
                drawingPixelFormatSizes[PixelFormat.Format64bppPArgb] = 64;

            formatMap[ImageFormats.Png] = ImageFormat.Png;
        }


        public static byte[] GetPixelData(Stream stream, bool flipY = true)
        {
            using (Bitmap bmp = ReadBitmapFromStream(stream))
                return GetPixelData(bmp, flipY);
        }

        public static byte[] GetPixelData(Stream stream, out int textureWidth, out int textureHeight, bool flipY = true)
        {
            using (Bitmap bmp = ReadBitmapFromStream(stream))
            {
                textureWidth = bmp.Width;
                textureHeight = bmp.Height;
                return GetPixelData(bmp, flipY);
            }
        }

        public static byte[] GetPixelData(Bitmap source, bool flipY = true)
        {
            Bitmap tmp = source;
            bool useTemporary = source.PixelFormat != PixelFormat.Format32bppArgb;
            try {
                if (useTemporary)
                    tmp = ConvertBitmap(tmp, PixelFormat.Format32bppArgb, source.Width, source.Height);

                BitmapData dat = tmp.LockBits(
                    new Rectangle(0, 0, tmp.Width, tmp.Height), ImageLockMode.ReadOnly,
                    tmp.PixelFormat
                    );

                byte[] bitmapData = new byte[tmp.Width * tmp.Height * 4];
                for (int i = 0; i < tmp.Height; i++)
                    Marshal.Copy(
                        System.IntPtr.Add(dat.Scan0, dat.Stride * i),
                        bitmapData,
                        tmp.Width * 4 * (flipY ? (tmp.Height - i - 1) : i),
                        tmp.Width * 4
                        );
                tmp.UnlockBits(dat);

                for (int i = 0; i < bitmapData.Length; i += 4)
                {
                    byte tmp1 = bitmapData[i], tmp2 = bitmapData[i + 1];
                    bitmapData[i] = bitmapData[i + 2];
                    bitmapData[i + 1] = tmp2;
                    bitmapData[i + 2] = tmp1;
                }

                return bitmapData;
            }
            finally {
                if (useTemporary)
                    tmp.Dispose();
            }
        }

        /// <summary> Creates a buffer filled with raw Rgba8 pixel data for the provided color. </summary>
        public static byte[] CreateSingleColorImageData(Colors.Rgba col, int width, int height, int stride)
        {
            if (stride % 4 != 0)
                throw new NotSupportedException();

            byte[] buffer = new byte[stride * height];
            buffer[0] = (byte)(col.r * 0xFF);
            buffer[1] = (byte)(col.g * 0xFF);
            buffer[2] = (byte)(col.b * 0xFF);
            buffer[3] = (byte)(col.a * 0xFF);

            buffer.RepeatPattern(4);

            return buffer;
        }

        public static int GetPixelFormatBitCount(PixelFormat fmt)
        {
            int output;
            if (drawingPixelFormatSizes.TryGetValue(fmt, out output) && output > 0)
                return output;
            else
                throw new NotImplementedException($"Unknown Drawing pixel format: {fmt}");
        }

        public static byte[] GetBitmapBytes(Bitmap source)
        {
            byte[] output = new byte[source.Width * source.Height * drawingPixelFormatSizes[source.PixelFormat] / 8];

            using (LockedBits dat = new LockedBits(source, new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat))
            {
                int outputStride = source.Width * drawingPixelFormatSizes[source.PixelFormat] / 8;
                if (dat.Stride != outputStride)
                {
                    byte[] input = new byte[dat.Stride * source.Height];
                    for (int i = 0; i < source.Height; i++)
                        Marshal.Copy(dat.Scan0 + dat.Stride * i, output, outputStride * i, outputStride);
                }
                else
                    Marshal.Copy(dat.Scan0, output, 0, output.Length);
            }

            return output;
        }

        public static void SetBitmapBytes(Bitmap target, byte[] bytes)
        {
            using (LockedBits dat = new LockedBits(target, new Rectangle(0, 0, target.Width, target.Height), ImageLockMode.WriteOnly, target.PixelFormat))
            {
                if (dat.Stride != target.Width * drawingPixelFormatSizes[target.PixelFormat] / 8)
                    throw new NotImplementedException("Stupid programmers detected.");

                Marshal.Copy(bytes, 0, dat.Scan0, bytes.Length);
            }
        }

        public static Bitmap ConvertBitmap(Bitmap orig, PixelFormat newFormat, int newWidth, int newHeight)
        {
            Bitmap clone = new Bitmap(Max(1, newWidth), Max(1, newHeight), PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(clone))
            {
                g.Clear(Color.FromArgb(0, 0, 0, 0));
                if (newWidth != orig.Width || newHeight != orig.Height)
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                }
                g.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            }
            return clone;
        }

        public static Bitmap CutBitmap(Bitmap src, Rectangle rect)
        {
            if (src.Width < rect.Right || src.Height < rect.Bottom)
                throw new ArgumentException("The provided rectangle exceeds the image bounds.", nameof(rect));

            Bitmap dest = new Bitmap(rect.Width, rect.Height, src.PixelFormat);
            try
            {
                using (LockedBits oldData = new LockedBits(src, rect, ImageLockMode.ReadOnly, src.PixelFormat))
                using (LockedBits newData = new LockedBits(dest, new Rectangle(0, 0, dest.Width, dest.Height), ImageLockMode.WriteOnly, dest.PixelFormat))
                {
                    for (int y = 0; y < rect.Height; y++)
                        ChaosUtil.Platform.Windows.MicrosoftVisualCppRuntime.memory.memcpy.Invoke(
                            IntPtr.Add(newData.Scan0, y * newData.Stride),
                            IntPtr.Add(oldData.Scan0, y * oldData.Stride),
                            (uint)rect.Width * (uint)drawingPixelFormatSizes[src.PixelFormat] / 8u
                            );

                    return dest;
                }
            }
            catch
            {
                dest.Dispose();
                throw;
            }
        }

        /// <summary>
        ///     Reads a bitmap from the provided <paramref name="stream"/> at its current position.
        ///     <para>
        ///         This is different from <see cref="Image.FromFile(Stream)"/> or <see cref="Bitmap(Stream)"/>,
        ///         which both always read from the start of the stream.
        ///     </para>
        /// </summary>
        /// <param name="stream"> The stream to read from. </param>
        /// <returns> The bitmap encoded at the provided <paramref name="stream"/>'s current position. </returns>
        public static Bitmap ReadBitmapFromStream(Stream stream)
        {
            using (Stream pngStream = GetPngStream(new BinaryReader(stream)))
                return new Bitmap(pngStream);
        }

        public static void WriteBitmapToStream(BinaryWriter wr, Bitmap img)
        {
            using (Stream str = new MemoryStream())
            {
                img.Save(str, ImageFormat.Png);
                str.Flush();
                str.Position = 0;
                using (BinaryReader rd = new BinaryReader(str))
                    wr.Write(rd.ReadBytes((int)str.Length));
            }
        }

        public static Bounds2i TrimBitmap(
            Bitmap src,
            ValidatePixel isColorValid,
            bool left = true,
            bool right = true,
            bool bottom = true,
            bool top = true
            )
        {
            PixelFormat pixelFormat = src.PixelFormat;
            if (drawingPixelFormatSizes[pixelFormat] % 8 != 0)
                throw new NotSupportedException("Pixel formats that do not have full bytes per channel do not make sense to us.");

            int fmtSz = drawingPixelFormatSizes[pixelFormat] / 8;
            byte[] data = GetBitmapBytes(src);
            Bounds2i rect = new Bounds2i(0, 0, src.Width - 1, src.Height - 1);

            if (left)
                for (; rect.left < rect.right; rect.low.x++)
                    for (int y = rect.bottom; y < rect.top; y++)
                        if (isColorValid(data.SubArray(fmtSz * (rect.left + y * src.Width), fmtSz)))
                            goto found_left;
                        found_left:

            if (right)
                for (; rect.left < rect.right; rect.high.x--)
                    for (int y = rect.bottom; y < rect.top; y++)
                        if (isColorValid(data.SubArray(fmtSz * (rect.right + y * src.Width), fmtSz)))
                            goto found_right;
                        found_right:

            if (bottom)
                for (; rect.bottom < rect.top; rect.low.y++)
                    for (int x = rect.left; x < rect.right; x++)
                        if (isColorValid(data.SubArray(fmtSz * (x + rect.bottom * src.Width), fmtSz)))
                            goto found_bottom;
                        found_bottom:

            if (top)
                for (; rect.bottom < rect.top; rect.high.y--)
                    for (int x = rect.left; x < rect.right; x++)
                        if (isColorValid(data.SubArray(fmtSz * (x + rect.top * src.Width), fmtSz)))
                            goto found_top;
                        found_top:

            return rect;
        }

        public static void CopyPixels(Bitmap src, Bitmap dest, Bounds2i srcRect, Vector2i position)
        {
            if (src.PixelFormat != dest.PixelFormat)
                throw new ArgumentException("Source and destination pixel formats do not match.");

            if (position.x + srcRect.width - 1 >= dest.Width
             || position.y + srcRect.height - 1 >= dest.Height)
                throw new ArgumentException("dest exceeds source boundaries");

            Rectangle destRect = new Rectangle(position.x, position.y, srcRect.width, srcRect.height);

            using (LockedBits srcData = new LockedBits(src, srcRect, ImageLockMode.ReadOnly, src.PixelFormat))
            using (LockedBits destData = new LockedBits(dest, destRect, ImageLockMode.WriteOnly, dest.PixelFormat))
            {
                for (int y = 0; y < srcRect.height; y++)
                    ChaosUtil.Platform.Windows.MicrosoftVisualCppRuntime.memory.memcpy.Invoke(
                        IntPtr.Add(destData.Scan0, y * destData.Stride),
                        IntPtr.Add(srcData.Scan0, y * srcData.Stride),
                        (uint)srcRect.width * 4u
                        );
            }
        }

        public static Bitmap Convert8bppTo32bppRgba(byte[] data, Vector2i bounds)
        {
            Bitmap bm = new Bitmap(bounds.x, bounds.y);
            try
            {
                byte[] converted = new byte[data.Length * 4];
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < 3; j++)
                        converted[i * 4 + j] = data[i];
                    converted[i * 4 + 3] = 0xFF;
                }
                SetBitmapBytes(bm, converted);
                return bm;
            }
            catch
            {
                bm.Dispose();
                throw;
            }
        }

        public static Bitmap MakeBitmapSquare(Bitmap source)
        {
            int sz = Max(source.Width, source.Height);
            Bitmap bm = new Bitmap(sz, sz);
            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bm))
                    graphics.DrawImage(source, bm.Width / 2 - source.Width / 2, bm.Height / 2 - source.Height / 2, source.Width, source.Height);
                return bm;
            }
            catch
            {
                bm.Dispose();
                throw;
            }
        }

        public static Bitmap ScaleBitmap(Image img, int width, int height)
        {
            Bitmap newImg = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            try
            {
                newImg.SetResolution(img.HorizontalResolution, img.VerticalResolution);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newImg))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        attr.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(img, new Rectangle(0, 0, width, height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attr);
                    }
                }
                return newImg;
            }
            catch
            {
                newImg.Dispose();
                throw;
            }
        }

        public static StreamView GetPngStream(BinaryReader rd)
        {
            const int PNG_END = 'I' << 24 | 'E' << 16 | 'N' << 8 | 'D';
            long startPosition = rd.BaseStream.Position;
            long sig = IO.Primitives.Integer.ReadInt64_BigEndian(rd);
            if (sig != unchecked((long)0x89504e470d0a1a0a)) // ?-P-N-G-CR-LF-EOF-LF
                throw new InvalidDataException("Stream does not contain png data at the current position.");

            while (true)
            {
                if (rd.BaseStream.Position == rd.BaseStream.Length)
                    throw new EndOfStreamException();

                int chunkLength = IO.Primitives.Integer.ReadInt32_BigEndian(rd);
                int chunkType = IO.Primitives.Integer.ReadInt32_BigEndian(rd);

                rd.ReadBytes(chunkLength);                     // byte[] data
                IO.Primitives.Integer.ReadInt32_BigEndian(rd); // int crc

                if (chunkType == PNG_END)
                    break;
            }

            long pngLength = rd.BaseStream.Position - startPosition;
            if (pngLength >= int.MaxValue)
                throw new NotSupportedException($"Images larger than {int.MaxValue - 1} bytes are not supported.");

            rd.BaseStream.Position = startPosition;
            return new StreamView(rd.BaseStream, startPosition, pngLength);
        }
    }
}

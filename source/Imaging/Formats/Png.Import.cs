using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static ChaosFramework.Math.Signs;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Graphics.Imaging.Formats
{
    using ChaosFramework.IO.Primitives;

    public static partial class Png
    {
        public static Rgba8Image FromStream(Stream str, bool flipY = true)
        {
            BinaryReader rd = new BinaryReader(str);

            if (IO.Primitives.Integer.ReadInt64_BigEndian(rd) != SIG)
                throw new InvalidDataException("Not a PNG");

            uint width = 0, height = 0;
            ColorType colorType = (ColorType)0xFF;

            byte[] palette = null;
            SysCol.List<byte> idat = new SysCol.List<byte>();

            bool hasIHDR = false;
            for (int chunkIndex = 0; ; ++chunkIndex)
            {
                uint len = Integer.ReadUInt32_BigEndian(rd);
                Chunks type = (Chunks)IO.Primitives.Integer.ReadInt32_BigEndian(rd);
                byte[] data = rd.ReadBytes((int)len);
                Integer.ReadUInt32_BigEndian(rd); // TODO: validate CRC

                switch (type)
                {
                    case Chunks.IHDR:
                        if (len != 13)
                            throw new InvalidDataException("Invalid IHDR");

                        if (hasIHDR)
                            throw new InvalidDataException("Multiple IHDR chunks!");
                        if (chunkIndex != 0)
                            throw new InvalidDataException("IHDR must be the first chunk!");
                        else
                            hasIHDR = true;

                        using (MemoryStream chunkStr = new MemoryStream(data))
                        using (BinaryReader chunkRd = new BinaryReader(chunkStr))
                        {
                            width = Integer.ReadUInt32_BigEndian(chunkRd);
                            height = Integer.ReadUInt32_BigEndian(chunkRd);

                            if (chunkRd.ReadByte() != 8)
                                throw new NotSupportedException("Only 8-bit PNG supported");

                            colorType = (ColorType)chunkRd.ReadByte();
                            // ignore compression/filter/interlace bytes
                        }

                        if (!Enum.GetValues<ColorType>().Contains(colorType))
                            throw new NotSupportedException("Unsupported color type");

                        break;

                    case Chunks.PLTE:
                        if (palette == null)
                            palette = data;
                        else
                            throw new InvalidDataException("Multiple palettes");
                        break;

                    case Chunks.IDAT:
                        idat.AddRange(data);
                        break;

                    case Chunks.IEND:
                        goto end;

                    default:
                        // ignore unknown chunks
                        break;
                }
            }
        end:;

            if (!hasIHDR)
                throw new InvalidDataException("Missing IHDR");

            if (!colorType.HasColor() && palette != null)
                throw new InvalidDataException("Grayscale images must not have palettes");

            byte[] decompressed;
            using (MemoryStream ms = new MemoryStream(idat.ToArray()))
            {
                using (ZLibStream ds = new ZLibStream(ms, CompressionMode.Decompress))
                using (MemoryStream outMs = new MemoryStream())
                {
                    ds.CopyTo(outMs);
                    decompressed = outMs.ToArray();
                }
            }

            uint bytesPerPixel = colorType switch
            {
                ColorType.Grayscale      => 1,
                ColorType.GrayscaleAlpha => 2,
                ColorType.Rgb            => 3,
                ColorType.Rgba           => 4,
                ColorType.Indexed        => 1,
                _                        => throw new NotSupportedException("Unsupported color type")
            };

            Rgba8Image img = new Rgba8Image(width, height);

            uint srcPos = 0;
            uint rowBytes = width * bytesPerPixel;
            byte[] prev = new byte[rowBytes];
            byte[] cur = new byte[rowBytes];
            for (uint line = 0; line < height; ++line)
            {
                uint y = flipY ? height - line - 1 : line;

                if (srcPos >= decompressed.Length)
                    throw new Exception("Truncated IDAT");

                FilterType filterType = (FilterType)decompressed[srcPos++];
                if (srcPos + rowBytes > decompressed.Length)
                    throw new Exception("Truncated scanline");

                Array.Copy(decompressed, srcPos, cur, 0, rowBytes);
                srcPos += rowBytes;

                // reverse filter
                switch (filterType)
                {
                    case FilterType.None:
                        break;

                    case FilterType.Sub:
                        for (int i = 0; i < rowBytes; i++)
                        {
                            int left = (i - bytesPerPixel) >= 0 ? cur[i - bytesPerPixel] : 0;
                            cur[i] = (byte)((cur[i] + left) & 0xFF);
                        }
                        break;

                    case FilterType.Up:
                        for (int i = 0; i < rowBytes; i++)
                        {
                            int up = prev[i];
                            cur[i] = (byte)((cur[i] + up) & 0xFF);
                        }
                        break;

                    case FilterType.Average:
                        for (int i = 0; i < rowBytes; i++)
                        {
                            int left = (i - bytesPerPixel) >= 0 ? cur[i - bytesPerPixel] : 0;
                            int up = prev[i];
                            cur[i] = (byte)((cur[i] + ((left + up) >> 1)) & 0xFF);
                        }
                        break;

                    case FilterType.Paeth:
                        for (int i = 0; i < rowBytes; i++)
                        {
                            int a = (i - bytesPerPixel) >= 0 ? cur[i - bytesPerPixel] : 0;
                            int b = prev[i];
                            int c = (i - bytesPerPixel) >= 0 ? prev[i - bytesPerPixel] : 0;
                            int p = a + b - c;
                            int pa = Abs(p - a), pb = Abs(p - b), pc = Abs(p - c);
                            int pr = (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                            cur[i] = (byte)((cur[i] + pr) & 0xFF);
                        }
                        break;

                    default:
                        throw new InvalidDataException("Unknown filter");
                }

                // convert cur -> RGBA output
                switch(colorType)
                {
                    case ColorType.Rgba:
                        for (int x = 0; x < width; x++)
                        {
                            int si = x * 4;
                            img[(uint)x, y] = new Rgba8(
                                cur[si + 0],
                                cur[si + 1],
                                cur[si + 2],
                                cur[si + 3]
                                );
                        }
                        break;

                    case ColorType.Rgb:
                        for (int x = 0; x < width; x++)
                        {
                            int si = x * 3;
                            img[(uint)x, y] = new Rgba8(
                                cur[si + 0],
                                cur[si + 1],
                                cur[si + 2],
                                255
                                );
                        }
                        break;

                    case ColorType.Grayscale:
                        for (int x = 0; x < width; x++)
                        {
                            byte v = cur[x];
                            img[(uint)x, y] = new Rgba8(v, v, v, 255);
                        }
                        break;

                    case ColorType.Indexed:
                        if (palette == null)
                            throw new InvalidDataException("Missing PLTE for indexed PNG");

                        for (int x = 0; x < width; x++)
                        {
                            int idx = cur[x];
                            int pi = idx * 3;
                            if (pi + 2 >= palette.Length)
                                throw new InvalidDataException("Palette index out of range");

                            img[(uint)x, y] = new Rgba8(
                                palette[pi + 0],
                                palette[pi + 1],
                                palette[pi + 2],
                                255
                                );
                        }
                        break;

                    case ColorType.GrayscaleAlpha:
                        for (int x = 0; x < width; x++)
                        {
                            int si = x * 2;
                            byte g = cur[si];
                            byte a = cur[si + 1];
                            img[(uint)x, y] = new Rgba8(g, g, g, a);
                        }
                        break;
                }

                (cur, prev) = (prev, cur);
                Array.Clear(cur, 0, cur.Length);
            }

            return img;
        }
    }
}

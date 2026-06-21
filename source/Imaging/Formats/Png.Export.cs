using System;
using System.IO;
using System.IO.Compression;
using static ChaosFramework.Math.Signs;

namespace ChaosFramework.Graphics.Imaging.Formats
{
    using ChaosFramework.IO.Primitives;

    public static partial class Png
    {
        public static void Save(Rgba8Image img, Stream target, bool flipY = true)
        {
            bool opaque = true;
            for (uint y = 0; y < img.height; ++y)
                for (uint x = 0; x < img.width; ++x)
                    if (img[x, y].a < 255)
                    {
                        opaque = false;
                        goto determinedOpaqueness;
                    }

        determinedOpaqueness:
            Save(img, target, !opaque, flipY);
        }

        public static void Save(Rgba8Image img, Stream target, bool alpha, bool flipY = true)
        {
            if (img == null)
                throw new ArgumentNullException(nameof(img));

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if ((int)img.width <= 0 || (int)img.height <= 0)
                throw new ArgumentException("Image too large");

            byte[] sig = BitConverter.GetBytes(SIG);
            Array.Reverse(sig);
            target.Write(sig);

            using (MemoryStream ihdr = new MemoryStream())
            {
                Integer.WriteBigEndian(ihdr, img.width);
                Integer.WriteBigEndian(ihdr, img.height);
                ihdr.WriteByte(8); // bit depth = 8
                ihdr.WriteByte((byte)(alpha ? ColorType.Rgba : ColorType.Rgb));
                ihdr.WriteByte(0); // compression method
                ihdr.WriteByte(0); // filter method
                ihdr.WriteByte(0); // interlace method
                WriteChunk(target, Chunks.IHDR, ihdr.ToArray());
            }

            uint bytesPerPixel = alpha ? 4u : 3u;
            using (MemoryStream uncompressed = new MemoryStream())
            {
                uint rowLen = img.width * bytesPerPixel;
                byte[] prevLine = new byte[rowLen];
                byte[] curLine = new byte[rowLen];
                byte[] filtLine = new byte[1 + curLine.Length];

                for (uint line = 0; line < img.height; ++line)
                {
                    uint y = flipY ? img.height - line - 1 : line;
                    int p = 0;
                    for (uint x = 0; x < img.width; ++x)
                    {
                        Rgba8 px = img[x, y];
                        curLine[p++] = px.r;
                        curLine[p++] = px.g;
                        curLine[p++] = px.b;
                        if (alpha)
                            curLine[p++] = px.a;
                    }

                    filtLine[0] = (byte)FilterType.Paeth;
                    for (int i = 0; i < curLine.Length; ++i)
                    {
                        int a = (i - bytesPerPixel) >= 0 ? curLine[i - bytesPerPixel] : 0;
                        int b = prevLine[i];
                        int c = (i - bytesPerPixel) >= 0 ? prevLine[i - bytesPerPixel] : 0;
                        filtLine[1 + i] = (byte)((curLine[i] - PaethPredictor(a, b, c)) & 0xFF);
                    }

                    uncompressed.Write(filtLine, 0, filtLine.Length);

                    // swap prevLine <- curLine
                    Buffer.BlockCopy(curLine, 0, prevLine, 0, curLine.Length);
                }

                uncompressed.Position = 0;
                using (MemoryStream compressed = new MemoryStream())
                {
                    compressed.WriteByte(0x78);
                    compressed.WriteByte(0xDA);

                    using (DeflateStream ds = new DeflateStream(
                        compressed,
                        CompressionLevel.
    #if NET7_0_OR_GREATER
                            SmallestSize
    #else
                            Optimal
    #endif
                        , leaveOpen: true
                        ))
                    {
                        uncompressed.CopyTo(ds);
                    }

                    uint adler = Adler32(uncompressed.ToArray(), 0, (int)uncompressed.Length);
                    Integer.WriteBigEndian(compressed, adler);

                    compressed.Position = 0;
                    WriteChunk(target, Chunks.IDAT, compressed.ToArray());
                }
            }

            WriteChunk(target, Chunks.IEND, Array.Empty<byte>());
        }

        static int PaethPredictor(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Abs(p - a);
            int pb = Abs(p - b);
            int pc = Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        static void WriteChunk(Stream s, Chunks chunk, byte[] data)
        {
            Integer.WriteBigEndian(s, (uint)data.Length);

            byte[] chunkBytes = BitConverter.GetBytes((int)chunk);
            Array.Reverse(chunkBytes);
            s.Write(chunkBytes);
            if (data.Length > 0)
                s.Write(data, 0, data.Length);

            uint crc = Crc32(chunkBytes, data);
            Integer.WriteBigEndian(s, crc);
        }

        static uint Adler32(byte[] buf, int offset, int length)
        {
            const uint MOD = 65521;
            uint a = 1, b = 0;
            int end = offset + length;
            for (int i = offset; i < end; ++i)
            {
                a = (a + buf[i]) % MOD;
                b = (b + a) % MOD;
            }
            return (b << 16) | a;
        }
    }
}

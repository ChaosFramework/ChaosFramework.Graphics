using System;
using System.IO;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.Imaging.Formats
{
    public static partial class Png
    {
        const float INV = 1.0f / 255.0f;

        const long SIG = unchecked((long)0x89504E470D0A1A0A); // ?-P-N-G-CR-LF-EOF-LF

        enum ColorType : byte
        {
            Grayscale = 0,
            Rgb = 2,
            Indexed = 3,
            GrayscaleAlpha = 4,
            Rgba = 6,
        }

        enum FilterType : byte
        {
            None = 0,
            Sub = 1,
            Up = 2,
            Average = 3,
            Paeth = 4,
        }

        enum Chunks : int
        {
            IHDR = 'I' << 24 | 'H' << 16 | 'D' << 8 | 'R',
            PLTE = 'P' << 24 | 'L' << 16 | 'T' << 8 | 'E',
            IDAT = 'I' << 24 | 'D' << 16 | 'A' << 8 | 'T',
            IEND = 'I' << 24 | 'E' << 16 | 'N' << 8 | 'D',
        }

        public static StreamView GetPngStream(BinaryReader rd)
        {
            long startPosition = rd.BaseStream.Position;
            if (IO.Primitives.Integer.ReadInt64_BigEndian(rd) != SIG)
                throw new InvalidDataException("Stream does not contain png data at the current position.");

            while (true)
            {
                if (rd.BaseStream.Position == rd.BaseStream.Length)
                    throw new EndOfStreamException();

                int chunkLength = IO.Primitives.Integer.ReadInt32_BigEndian(rd);
                Chunks chunkType = (Chunks)IO.Primitives.Integer.ReadInt32_BigEndian(rd);

                rd.ReadBytes(chunkLength);                     // byte[] data
                IO.Primitives.Integer.ReadInt32_BigEndian(rd); // int crc

                if (chunkType == Chunks.IEND)
                    break;
            }

            long pngLength = rd.BaseStream.Position - startPosition;
            if (pngLength >= int.MaxValue)
                throw new NotSupportedException($"Images larger than {int.MaxValue - 1} bytes are not supported.");

            rd.BaseStream.Position = startPosition;
            return new StreamView(rd.BaseStream, startPosition, pngLength);
        }

        static bool HasPalette(this ColorType c)
            => ((byte)c & 0x01) != 0;

        static bool HasColor(this ColorType c)
            => ((byte)c & 0x02) != 0;

        static bool HasAlpha(this ColorType c)
            => ((byte)c & 0x04) != 0;
    }
}

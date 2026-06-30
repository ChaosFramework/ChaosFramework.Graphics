using System.Collections.Generic;
using System.IO;
using ChaosFramework.Math.Vectors;

namespace ChaosFramework.Graphics.Imaging
{
    using Formats;

    public class Icon(Dictionary<Vector2i, Rgba8Image> imgs)
    {
        readonly record struct IconDir(
            ushort reserved,
            ushort type,
            ushort count,
            IconDirEntry[] entries
            )
        {
            public static IconDir Read(BinaryReader rd)
            {
                ushort reserved = rd.ReadUInt16();
                ushort type = rd.ReadUInt16();
                ushort count = rd.ReadUInt16();
                return new IconDir(reserved, type, count, IconDirEntry.Read(rd, count));
            }

            public readonly ushort reserved = reserved == 0 ? reserved : throw new InvalidDataException("Reserved must be 0!");
            public readonly ushort type = type;
            public readonly ushort count = count;
            public readonly IconDirEntry[] entries = entries;
        }

        readonly struct IconDirEntry(
            ushort width,
            ushort height,
            byte colorCount,
            byte reserved,
            ushort planes,
            ushort bitCount,
            uint bytesInRes,
            uint imageOffset
            )
        {
            public static IconDirEntry[] Read(BinaryReader br, ushort count)
            {
                IconDirEntry[] entries = new IconDirEntry[count];
                for (int i = 0; i < count; i++)
                {
                    byte w = br.ReadByte();
                    byte h = br.ReadByte();
                    entries[i] = new IconDirEntry(
                        width:       (ushort)(w == 0 ? 256 : w),
                        height:      (ushort)(h == 0 ? 256 : h),
                        colorCount:  br.ReadByte(),
                        reserved:    br.ReadByte(),
                        planes:      br.ReadUInt16(),
                        bitCount:    br.ReadUInt16(),
                        bytesInRes:  br.ReadUInt32(),
                        imageOffset: br.ReadUInt32()
                        );
                }

                return entries;
            }

            public readonly ushort width = width;
            public readonly ushort height = height;
            public readonly byte colorCount = colorCount;
            public readonly byte reserved = reserved == 0 ? reserved : throw new InvalidDataException("Reserved must be 0!");
            public readonly ushort planes = planes;
            public readonly ushort bitCount = bitCount;
            public readonly uint bytesInRes = bytesInRes;
            public readonly uint imageOffset = imageOffset;
        }

#if NET8_0_OR_GREATER
        public static Icon FromStream(Stream icoStream)
        {
            using (BinaryReader br = new BinaryReader(icoStream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                IconDir dir = IconDir.Read(br);

                Dictionary<Vector2i, Rgba8Image> imgs = [];
                foreach (IconDirEntry entry in dir.entries)
                    if (entry.bytesInRes > 0u)
                    {
                        icoStream.Position = entry.imageOffset;

                        byte[] rawImage = br.ReadBytes(checked((int)entry.bytesInRes));
                        if (rawImage.Length != entry.bytesInRes)
                            throw new EndOfStreamException($"Unexpected end of ICO while reading image at offset {entry.imageOffset}.");

                        using (MemoryStream ms = new(rawImage, writable: false))
                        {
                            Rgba8Image img = Png.FromStream(ms);
                            imgs[new Vector2i(entry.width, entry.height)] = img;
                        }
                    }

                return new(imgs);
            }
        }
#endif

        public readonly IReadOnlyDictionary<Vector2i, Rgba8Image> imgs = imgs;
    }
}

using ChaosFramework.Collections.Immutable;

namespace ChaosFramework.Graphics.Imaging.Formats
{
    public static partial class Png
    {
        // TODO: Move this somewhere else

        static readonly ImmutableArray<uint> CRC32_LOOKUP = CreateCrc32Lookup();

        static uint[] CreateCrc32Lookup()
        {
            uint[] table = new uint[256];

            const uint CRC32_POLYNOMIAL = 0xEDB88320u;
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                    c = ((c & 1) != 0) ? (CRC32_POLYNOMIAL ^ (c >> 1)) : (c >> 1);

                table[i] = c;
            }

            return table;
        }

        static uint Crc32(params byte[][] data)
        {
            uint c = 0xFFFFFFFFu;

            foreach(byte[] dat in data)
                if (dat != null)
                    foreach (byte b in dat)
                        c = CRC32_LOOKUP[(int)((c ^ b) & 0xFF)] ^ (c >> 8);

            return c ^ 0xFFFFFFFFu;
        }
    }
}

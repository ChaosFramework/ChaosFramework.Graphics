using ChaosFramework.IO;
using ChaosFramework.Math;
using ChaosFramework.Math.Vectors;
using System.IO;

namespace ChaosFramework.Graphics.Text
{
    public class GlyphDimensions
    {
        [ChaosIO.RegisterType]
        static void _() => ChaosIO.AddType(Read, Write);

        static GlyphDimensions Read(BinaryReader reader)
            => new GlyphDimensions(
                reader.Read<Bounds2f>(),
                reader.Read<Bounds2i>(),
                reader.Read<Bounds2i>(),
                reader.Read<Vector2f>()
                );

        static void Write(BinaryWriter wr, GlyphDimensions descr)
        {
            wr.WriteAs(descr.charBounds);
            wr.WriteAs(descr.sdfCoords);
            wr.WriteAs(descr.colCoords);
            wr.WriteAs(descr.advanceCursor);
        }

        public readonly Bounds2f charBounds;
        public readonly Bounds2i sdfCoords;
        public readonly Bounds2i colCoords;
        public readonly Vector2f advanceCursor;

        public GlyphDimensions(Bounds2f charBounds, Bounds2i sdfCoords, Bounds2i colCoords, Vector2f advanceCursor)
        {
            this.charBounds = charBounds;
            this.sdfCoords = sdfCoords;
            this.colCoords = colCoords;
            this.advanceCursor = advanceCursor;
        }
    }
}

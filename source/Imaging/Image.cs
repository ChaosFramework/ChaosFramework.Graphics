using ChaosFramework.Graphics.Colors;
using System;

namespace ChaosFramework.Graphics.Imaging
{
    public interface Image
    {
        uint width {get;}
        uint height {get;}
    }

    public interface Image<Color> : Image
        where Color : struct
    {
        static abstract Image<Color> CreateEmpty(uint w, uint h);

        Color this[uint x, uint y] {get; set;}
    }

    public class RgbaImage : Image<Rgba>
    {
        static Image<Rgba> Image<Rgba>.CreateEmpty(uint w, uint h)
            => new RgbaImage(w, h);

        static Rgba[] CreateColorPixels(uint w, uint h, Rgba rgba)
        {
            Rgba[] pixels = new Rgba[w * h];

            // TODO: performance
            uint len = w * h;
            for (uint i = 0; i < len; ++i)
                pixels[i] = rgba;

                return pixels;
        }

        public readonly uint w, h;
        readonly Rgba[] pixels;

        public uint width => w;
        public uint height => h;

        public Rgba this[uint x, uint y]
        {
            get { return pixels[x + w * y]; }
            set { pixels[x + w * y] = value; }
        }

        public RgbaImage(uint w, uint h)
            : this(w, h, new Rgba[w * h])
        { }

        public RgbaImage(uint w, uint h, Rgba rgba)
            : this(w, h, CreateColorPixels(w, h, rgba))
        { }

        public RgbaImage(uint w, uint h, Rgba[] pixels)
        {
            if (w * h != pixels.Length)
                throw new ArgumentException($"""
                    Image bounds do not match provided pixel array
                        w={w}
                        h={h}
                        expected length: {w * h}
                        provided length: {pixels.Length}
                    """
                    );

            this.w = w;
            this.h = h;
            this.pixels = pixels;
        }
    }
}

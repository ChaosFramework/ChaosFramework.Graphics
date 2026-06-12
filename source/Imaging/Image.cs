using System;
using System.Runtime.InteropServices;

namespace ChaosFramework.Graphics.Imaging
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct Rgba8(byte r, byte g, byte b, byte a);

    public class RawDataHandle
        : IDisposable
    {
        public readonly IntPtr firstElementAddress;
        readonly GCHandle gcHandle;

        public static RawDataHandle Create<T>(T[] arr)
            where T: struct
            => new RawDataHandle(GCHandle.Alloc(arr, GCHandleType.Pinned), Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0));

        private RawDataHandle(GCHandle gcHandle, IntPtr firstElementAddress)
        {
            this.gcHandle = gcHandle;
            this.firstElementAddress = firstElementAddress;
        }

        void IDisposable.Dispose()
            => gcHandle.Free();
    }

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

    public class Rgba8Image : Image<Rgba8>
    {
        static Image<Rgba8> Image<Rgba8>.CreateEmpty(uint w, uint h)
            => new Rgba8Image(w, h);

        public static Rgba8Image CreateEmpty(uint w, uint h)
            => new Rgba8Image(w, h);

        static Rgba8[] CreateColorPixels(uint w, uint h, Rgba8 rgba)
        {
            Rgba8[] pixels = new Rgba8[w * h];

            // TODO: performance
            uint len = w * h;
            for (uint i = 0; i < len; ++i)
                pixels[i] = rgba;

            return pixels;
        }

        public readonly uint w, h;
        readonly Rgba8[] pixels;

        public uint width => w;
        public uint height => h;

        public Rgba8 this[uint x, uint y]
        {
            get { return pixels[x + w * y]; }
            set { pixels[x + w * y] = value; }
        }

        public Rgba8Image(uint w, uint h)
            : this(w, h, new Rgba8[w * h])
        { }

        public Rgba8Image(uint w, uint h, Rgba8 rgba)
            : this(w, h, CreateColorPixels(w, h, rgba))
        { }

        public Rgba8Image(uint w, uint h, Rgba8[] pixels)
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

        public RawDataHandle GetRawData()
            => RawDataHandle.Create(pixels);
    }
}

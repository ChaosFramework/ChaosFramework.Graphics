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
#if NET8_0_OR_GREATER
        static abstract Image<Color> CreateEmpty(uint w, uint h);
#endif

        Color this[uint x, uint y] {get; set;}
    }
}

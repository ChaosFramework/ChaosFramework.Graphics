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
}

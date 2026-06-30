using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.AssetContainers
{
    using Imaging;
    using Imaging.Formats;

    public class Rgba8ImageContainer
        : AssetContainer<Rgba8Image>
    {
        public Rgba8ImageContainer(StreamSource streamSource, bool monitoring = true)
            : base(streamSource, monitoring)
        { }

        protected override void DisposeItem(Rgba8Image obj) { }

        protected override Rgba8Image LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
            =>
#if NET8_0_OR_GREATER
            Png.FromStream(resource);
#else
            throw new System.NotSupportedException("Loading images from stream requires .NET 8.0 or greater.");
#endif
    }
}

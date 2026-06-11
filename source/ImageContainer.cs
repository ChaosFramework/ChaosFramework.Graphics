using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.AssetContainers
{
    using Imaging;
    using Imaging.Formats;

    public class RgbaImageContainer
        : AssetContainer<RgbaImage>
    {
        public RgbaImageContainer(StreamSource streamSource, bool monitoring = true)
            : base(streamSource, monitoring)
        { }

        protected override void DisposeItem(RgbaImage obj) { }

        protected override RgbaImage LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
            => Png.FromStream(resource);
    }
}

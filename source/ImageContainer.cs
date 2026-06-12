using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;

namespace ChaosFramework.Graphics.AssetContainers
{
    using Imaging;
    using Imaging.Formats;

    public class RgbaImageContainer
        : AssetContainer<Rgba8Image>
    {
        public RgbaImageContainer(StreamSource streamSource, bool monitoring = true)
            : base(streamSource, monitoring)
        { }

        protected override void DisposeItem(Rgba8Image obj) { }

        protected override Rgba8Image LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
            => Png.FromStream(resource);
    }
}

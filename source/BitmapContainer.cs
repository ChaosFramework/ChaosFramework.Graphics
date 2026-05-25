using ChaosFramework.IO.Containers;
using ChaosFramework.IO.Streams;
using System.Drawing;

namespace ChaosFramework.Graphics.AssetContainers
{
    public class BitmapContainer
        : AssetContainer<Bitmap>
    {
        public BitmapContainer(StreamSource streamSource, bool monitoring = true)
            : base(streamSource, monitoring)
        { }

        protected override void DisposeItem(Bitmap obj)
            => obj?.Dispose();

        protected override Bitmap LoadFromStream(Key key, System.IO.Stream resource, CancellationToken cancel)
            => new Bitmap(resource);
    }
}

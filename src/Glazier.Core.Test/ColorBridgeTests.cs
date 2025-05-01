using CascadePass.Glazier.Core;
using SixLabors.ImageSharp.PixelFormats;
using System.Windows.Media;

namespace CascadePass.Glazier.Core.Test
{
    [TestClass]
    public class ColorBridgeTests
    {
        [TestMethod]
        public void GetRgba32FromColor_ShouldConvertCorrectly()
        {
            var mediaColor = Color.FromArgb(255, 128, 64, 32);

            var rgba32 = ColorBridge.GetRgba32FromColor(mediaColor);

            Assert.AreEqual(mediaColor.R, rgba32.R);
            Assert.AreEqual(mediaColor.G, rgba32.G);
            Assert.AreEqual(mediaColor.B, rgba32.B);
            Assert.AreEqual(mediaColor.A, rgba32.A);
        }

        [TestMethod]
        public void GetColorFromRgba32_ShouldConvertCorrectly()
        {
            var rgba32 = new Rgba32(128, 64, 32, 255);

            var mediaColor = ColorBridge.GetColorFromRgba32(rgba32);

            Assert.AreEqual(rgba32.R, mediaColor.R);
            Assert.AreEqual(rgba32.G, mediaColor.G);
            Assert.AreEqual(rgba32.B, mediaColor.B);
            Assert.AreEqual(rgba32.A, mediaColor.A);
        }

        [TestMethod]
        public void Conversion_ShouldBeSymmetric()
        {
            var originalColor = Color.FromArgb(255, 128, 64, 32);

            var rgba32 = ColorBridge.GetRgba32FromColor(originalColor);
            var convertedBackColor = ColorBridge.GetColorFromRgba32(rgba32);

            Assert.AreEqual(originalColor.R, convertedBackColor.R);
            Assert.AreEqual(originalColor.G, convertedBackColor.G);
            Assert.AreEqual(originalColor.B, convertedBackColor.B);
            Assert.AreEqual(originalColor.A, convertedBackColor.A);
        }
    }
}
